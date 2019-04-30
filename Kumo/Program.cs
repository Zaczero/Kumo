using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Kumo
{
    struct ConfigStruct
    {
        public string CloudflareEmail;
        public string CloudflareApiKey;
        public string BlockNote;

        public string WatcherTargetFile;
        public int WatcherCheckSleep;
        public long WatcherStreamPosition;

        public int AbuseExpirationTime;
        public int BlockExpirationTime;
        public int AbusesToBlock;

        public string NginxBlockSnippetFile;

        public Dictionary<string, AbuseStruct> AbuseLogDictionary;
        public Queue<BlockStruct> BlockQueue;
        public HashSet<string> BlockIpAddressHashSet;
    }

    struct AbuseStruct
    {
        public string IpAddress;
        public Queue<int> Timestamps;

        public AbuseStruct(string ipAddress)
        {
            IpAddress = ipAddress;
            Timestamps = new Queue<int>();
        }

        public override string ToString()
        {
            return $"{IpAddress} (Count = {Timestamps.Count})";
        }
    }

    struct BlockStruct
    {
        public string IpAddress;
        public int ExpirationTime;
        public string BlockId;

        public BlockStruct(string ipAddress, int expirationTime)
        {
            IpAddress = ipAddress;
            ExpirationTime = expirationTime;
            BlockId = null;
        }
    }

    class Program
    {
        private static readonly Regex AbuseRegex = new Regex(@"^(?<year>\d{4})\/(?<month>\d{2})\/(?<day>\d{2}) (?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2}) \[error\] \d+#\d+: \*\d+ limiting requests, excess: \d+\.\d+ by zone "".*?"", client: (?<ip>[\da-f\.:]+)", RegexOptions.Compiled);
        private static readonly HttpClient Http = new HttpClient();

        private const string ConfigFileName = "config.json";
        private static ConfigStruct _config;
        private static bool _saveConfig;
        private static bool _saveSnippet;

        static void Main(string[] args)
        {
            LoadConfig();

            if (string.IsNullOrEmpty(_config.CloudflareEmail))
            {
                Console.WriteLine(Process.GetCurrentProcess().MainModule.FileName);
                Console.WriteLine("Kumo is not configured");
                Environment.Exit(2);
            }
            
            Http.DefaultRequestHeaders.TryAddWithoutValidation("X-Auth-Email", _config.CloudflareEmail);
            Http.DefaultRequestHeaders.TryAddWithoutValidation("X-Auth-Key", _config.CloudflareApiKey);

            while (true)
            {
                try
                {
                    Console.WriteLine("Watching...");
                    Watcher();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Watcher crashed: {ex.Message}");
                    Thread.Sleep(1_000);
                }
            }
        }

        static void LoadConfig()
        {
            if (!File.Exists(ConfigFileName))
            {
                SaveConfig();

                Console.WriteLine("Generated new config file");
                Environment.Exit(1);
            }

            var json = File.ReadAllText(ConfigFileName);
            _config = JsonConvert.DeserializeObject<ConfigStruct>(json);

            if (!File.Exists(_config.NginxBlockSnippetFile))
            {
                File.Create(_config.NginxBlockSnippetFile);
            }
        }

        static void SaveConfig()
        {
            if (_config.Equals(default(ConfigStruct)))
            {
                _config = new ConfigStruct
                {
                    CloudflareEmail = string.Empty,
                    CloudflareApiKey = string.Empty,
                    BlockNote = "Created by Kumo",

                    WatcherTargetFile = "/var/log/nginx/error.log",
                    WatcherCheckSleep = 2_000,
                
                    AbuseExpirationTime = 300,
                    BlockExpirationTime = 10800,
                    AbusesToBlock = 10,

                    NginxBlockSnippetFile = "/etc/nginx/snippets/kumo.conf",

                    AbuseLogDictionary = new Dictionary<string, AbuseStruct>(),
                    BlockQueue = new Queue<BlockStruct>(),
                    BlockIpAddressHashSet = new HashSet<string>(),
                };
            }

            var json = JsonConvert.SerializeObject(_config);
            File.WriteAllText(ConfigFileName, json);
        }

        static void SaveSnippet()
        {
            var sb = new StringBuilder();

            sb.AppendLine("# " + _config.BlockNote);

            foreach (var blockStruct in _config.BlockQueue)
            {
                sb.AppendLine($"deny {blockStruct.IpAddress};");
            }

            File.WriteAllText(_config.NginxBlockSnippetFile, sb.ToString());

            "nginx -s reload".Bash();
        }

        static string GetDictionaryKey(string humanIpAddress)
        {
            var ipAddress = IPAddress.Parse(humanIpAddress);
            var buffer = ipAddress.GetAddressBytes();

            // prioritize least significant bytes
            Array.Reverse(buffer);

            return Encoding.ASCII.GetString(buffer);
        }

        static int GetCurrentTimestamp()
        {
            var date = DateTime.UtcNow;

            return GetTimestamp(
                (short) date.Year,
                (byte) date.Month,
                (byte) date.Day,
                (byte) date.Hour,
                (byte) date.Minute,
                (byte) date.Second);
        }

        static int GetTimestamp(short year, byte month, byte day, byte hour, byte minute, byte second)
        {
            // values are rounded for maximum performance
            // we don't care if month has 28 or 31 days
            const int perMinute = 60;
            const int perHour = perMinute * 60;
            const int perDay = perHour * 24;
            const int perMonth = perDay * 31;
            const int perYear = perMonth * 12;

            return (year - 2018) * perYear +
                   month * perMonth +
                   day * perDay +
                   hour * perHour +
                   minute * perMinute +
                   second;
        }

        static void BlockIp(ref BlockStruct blockStruct)
        {
            while (true)
            {
                try
                {
                    var request = new Dictionary<string, object>
                    {
                        ["mode"] = "block",
                        ["configuration"] = new Dictionary<string, object> {{"target", "ip"}, {"value", blockStruct.IpAddress}},
                        ["notes"] = _config.BlockNote,
                    };

                    var requestJson = JsonConvert.SerializeObject(request);
                    var response = Http.SendAsync(
                        new HttpRequestMessage(HttpMethod.Post, "https://api.cloudflare.com/client/v4/user/firewall/access_rules/rules")
                        {
                            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
                        }).GetAwaiter().GetResult();

                    var responseHtml = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var responseObj = JsonConvert.DeserializeObject<dynamic>(responseHtml);

                    var success = (bool) responseObj.success;
                    if (success)
                    {
                        var id = (string) responseObj.result.id;
                        blockStruct.BlockId = id;
                        Console.WriteLine($"Blocked IP: {blockStruct.IpAddress} (Id = {id})");
                    }
                    else
                    {
                        Console.WriteLine(responseHtml);
                    }

                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"IP blocker crashed: {ex.Message}");
                    Thread.Sleep(1_000);
                }
            }
        }

        static void UnblockIp(ref BlockStruct blockStruct)
        {
            while (true)
            {
                try
                {
                    var id = blockStruct.BlockId;

                    var response = Http.SendAsync(
                        new HttpRequestMessage(HttpMethod.Delete, "https://api.cloudflare.com/client/v4/user/firewall/access_rules/rules/" + id)
                        {
                            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
                        }).GetAwaiter().GetResult();

                    Console.WriteLine($"Un-blocked IP: {blockStruct.IpAddress} (Id = {id})");

                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"IP un-blocker crashed: {ex.Message}");
                    Thread.Sleep(1_000);
                }
            }
        }

        static void Watcher()
        {
            byte[] analyzeBuffer = null;

            while (true)
            {
                Thread.Sleep(_config.WatcherCheckSleep);

                var fs = new FileStream(_config.WatcherTargetFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fs.Length > _config.WatcherStreamPosition)
                {
                    // analyze
                    Console.WriteLine("Analyzing...");

                    analyzeBuffer = new byte[fs.Length - _config.WatcherStreamPosition];

                    fs.Position = _config.WatcherStreamPosition;
                    fs.Read(analyzeBuffer, 0, analyzeBuffer.Length);

                    _saveConfig = true;
                }
                else if (fs.Length < _config.WatcherStreamPosition)
                {
                    // reset
                    Console.WriteLine("Reset >.<");

                    _saveConfig = true;
                }

                _config.WatcherStreamPosition = fs.Length;

                fs.Dispose();

                if (analyzeBuffer != null)
                {
                    Analyze(analyzeBuffer);
                    analyzeBuffer = null;
                }

                ProcessAbuseLog();
                ProcessBlock();

                if (_saveSnippet)
                {
                    SaveSnippet();
                    _saveSnippet = false;
                }

                if (_saveConfig)
                {
                    SaveConfig();
                    _saveConfig = false;
                }
            }
        }

        static void Analyze(byte[] buffer)
        {
            var text = Encoding.UTF8.GetString(buffer);
            var lines = text.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine($"Parsing {lines.Length} lines... ({buffer.Length} bytes)");

            foreach (var line in lines)
            {
                var match = AbuseRegex.Match(line);
                if (match.Success)
                {
                    var year = short.Parse(match.Groups["year"].Value);
                    var month = byte.Parse(match.Groups["month"].Value);
                    var day = byte.Parse(match.Groups["day"].Value);
                    var hour = byte.Parse(match.Groups["hour"].Value);
                    var minute = byte.Parse(match.Groups["minute"].Value);
                    var second = byte.Parse(match.Groups["second"].Value);
                    var ip = match.Groups["ip"].Value;

                    var key = GetDictionaryKey(ip);
                    var timestamp = GetTimestamp(year, month, day, hour, minute, second);

                    if (!_config.AbuseLogDictionary.ContainsKey(key))
                    {
                        _config.AbuseLogDictionary[key] = new AbuseStruct(ip);
                    }

                    _config.AbuseLogDictionary[key].Timestamps.Enqueue(timestamp);
                }
            }
        }

        static void ProcessAbuseLog()
        {
            var currentTimestamp = GetCurrentTimestamp();
            var expireTimestamp = currentTimestamp - _config.AbuseExpirationTime;
            var itemsToRemove = new List<string>();

            foreach (var (key, value) in _config.AbuseLogDictionary)
            {
                if (_config.BlockIpAddressHashSet.Contains(value.IpAddress))
                {
                    itemsToRemove.Add(key);
                    continue;
                }

                // remove expired timestamps
                if (value.Timestamps.Count == 0)
                {
                    itemsToRemove.Add(key);
                    continue;
                }

                while (true)
                {
                    var timestamp = value.Timestamps.Peek();
                    if (timestamp < expireTimestamp)
                    {
                        value.Timestamps.Dequeue();
                    }
                    else
                    {
                        break;
                    }
                }

                // count & block abusing ips
                if (value.Timestamps.Count >= _config.AbusesToBlock)
                {
                    var blockStruct = new BlockStruct(value.IpAddress, currentTimestamp + _config.BlockExpirationTime);
                    BlockIp(ref blockStruct);

                    _config.BlockQueue.Enqueue(blockStruct);
                    _config.BlockIpAddressHashSet.Add(value.IpAddress);

                    itemsToRemove.Add(key);
                    _saveConfig = true;
                    _saveSnippet = true;
                }
            }

            foreach (var key in itemsToRemove)
            {
                _config.AbuseLogDictionary.Remove(key);
            }
        }

        static void ProcessBlock()
        {
            var currentTimestamp = GetCurrentTimestamp();

            while (true)
            {
                if (!_config.BlockQueue.TryPeek(out var blockStruct))
                {
                    break;
                }

                if (blockStruct.ExpirationTime < currentTimestamp)
                {
                    UnblockIp(ref blockStruct);

                    _config.BlockQueue.Dequeue();
                    _config.BlockIpAddressHashSet.Remove(blockStruct.IpAddress);

                    _saveConfig = true;
                    _saveSnippet = true;
                }
                else
                {
                    break;
                }
            }
        }
    }
}
