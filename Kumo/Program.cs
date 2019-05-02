using Kumo.Structs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading;

namespace Kumo
{
    internal class Program
    {
        private const string ConfigFileName = "config.json";
        private const string DataFileName = "data.json";
        private static readonly Dictionary<string, AbuseStruct> AbuseDictionary = new Dictionary<string, AbuseStruct>();

        private static bool _dataChanged;
        private static bool _underAttack;
        private static int _underAttackExpirationTicks;

        private static void Main(string[] args)
        {
            if (!File.Exists(ConfigFileName))
            {
                GlobalVars.Config = ConfigManager.GetDefaultConfig();
                ConfigManager.SaveConfig(ConfigFileName, GlobalVars.Config);

                Console.WriteLine("Config not found, generated new one");
                Environment.Exit(1);
            }

            GlobalVars.Config = ConfigManager.ReadConfig(ConfigFileName);
            GlobalVars.Data = File.Exists(DataFileName) ? DataManager.ReadData(DataFileName) : DataManager.GetDefaultData();

            if (string.IsNullOrEmpty(GlobalVars.Config.CloudflareEmail))
            {
                Console.WriteLine($"Application is not configured, please edit {ConfigFileName}");
                Environment.Exit(2);
            }

            if (!File.Exists(GlobalVars.Config.NginxBlockSnippetFile))
                File.Create(GlobalVars.Config.NginxBlockSnippetFile);

            GlobalVars.Http = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                SslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12,
            });

            GlobalVars.Http.DefaultRequestHeaders.TryAddWithoutValidation("X-Auth-Email", GlobalVars.Config.CloudflareEmail);
            GlobalVars.Http.DefaultRequestHeaders.TryAddWithoutValidation("X-Auth-Key", GlobalVars.Config.CloudflareApiKey);

            while (true)
            {
                try
                {
                    Console.WriteLine("Watching...");
                    Watcher();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Watcher crashed: {ex.Message}{ex.StackTrace}");
                    Thread.Sleep(1_000);
                }
            }
        }

        private static void Watcher()
        {
            byte[] parseBuffer = null;

            while (true)
            {
                GC.Collect();
                Thread.Sleep(GlobalVars.Config.WatcherCheckSleep);

                var fs = new FileStream(GlobalVars.Config.WatcherTargetFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fs.Length > GlobalVars.Data.WatcherStreamPosition)
                {
                    parseBuffer = new byte[fs.Length - GlobalVars.Data.WatcherStreamPosition];
                    Console.WriteLine($"Analyzing {parseBuffer.Length} bytes...");

                    fs.Position = GlobalVars.Data.WatcherStreamPosition;
                    fs.Read(parseBuffer, 0, parseBuffer.Length);
                }

                GlobalVars.Data.WatcherStreamPosition = fs.Length;

                fs.Dispose();

                if (parseBuffer != null)
                {
                    Parse(parseBuffer);
                    parseBuffer = null;
                }

                ProcessAbuseLog();
                ProcessBlock();

                if (_dataChanged)
                {
                    Utilities.SaveNginxSnippet();

                    DataManager.SaveData(DataFileName, GlobalVars.Data);
                    _dataChanged = false;
                }
            }
        }

        private static void Parse(byte[] buffer)
        {
            var text = Encoding.UTF8.GetString(buffer);
            var lines = text.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine($"Parsing {lines.Length} lines...");

            foreach (var line in lines)
            {
                var match = RegexPatterns.AbuseRegex.Match(line);
                if (match.Success)
                {
                    var year = short.Parse(match.Groups["year"].Value);
                    var month = byte.Parse(match.Groups["month"].Value);
                    var day = byte.Parse(match.Groups["day"].Value);
                    var hour = byte.Parse(match.Groups["hour"].Value);
                    var minute = byte.Parse(match.Groups["minute"].Value);
                    var second = byte.Parse(match.Groups["second"].Value);
                    var ip = match.Groups["ip"].Value;

                    var timestamp = Utilities.GetTimestamp(year, month, day, hour, minute, second);

                    if (!AbuseDictionary.ContainsKey(ip))
                        AbuseDictionary[ip] = new AbuseStruct(ip);

                    AbuseDictionary[ip].Timestamps.Enqueue(timestamp);

                    // save data file so we don't rescan same error log after restart
                    // -> WatcherStreamPosition
                    _dataChanged = true;
                }
            }
        }

        private static void ProcessAbuseLog()
        {
            var currentTimestamp = Utilities.GetCurrentTimestamp();
            var expireTimestamp = currentTimestamp - GlobalVars.Config.AbuseExpirationTime;
            var blockCounter = 0;
            var itemsToRemove = new List<string>();

            foreach (var (key, value) in AbuseDictionary)
            {
                if (GlobalVars.Data.BlockHashSet.Contains(value.IpAddress))
                {
                    itemsToRemove.Add(key);
                    continue;
                }

                while (true)
                {
                    if (value.Timestamps.TryPeek(out var timestamp))
                    {
                        if (timestamp < expireTimestamp)
                        {
                            value.Timestamps.Dequeue();
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                
                // remove expired timestamps
                if (value.Timestamps.Count == 0)
                {
                    itemsToRemove.Add(key);
                    continue;
                }

                // count & block abusing ips
                var abusesToBlock = _underAttack
                    ? GlobalVars.Config.AbusesToBlockUnderAttack
                    : GlobalVars.Config.AbusesToBlock;

                if (value.Timestamps.Count >= abusesToBlock)
                {
                    var blockStruct = new BlockStruct(value.IpAddress, currentTimestamp + GlobalVars.Config.BlockExpirationTime)
                    {
                        BlockId = CloudflareUtilities.Block(value.IpAddress)
                    };

                    Console.WriteLine($"Blocked abusing IP: {blockStruct.IpAddress} (Id = {blockStruct.BlockId})");

                    GlobalVars.Data.BlockQueue.Enqueue(blockStruct);
                    GlobalVars.Data.BlockHashSet.Add(value.IpAddress);
                    blockCounter++;

                    itemsToRemove.Add(key);
                    _dataChanged = true;
                }
            }

            foreach (var key in itemsToRemove)
                AbuseDictionary.Remove(key);

            if (!_underAttack && blockCounter >= GlobalVars.Config.BlocksToUnderAttack)
            {
                _underAttack = true;
                Console.WriteLine($"UAM is now enabled (blocked {blockCounter} IPs in one tick)");
            }

            if (_underAttack)
            {
                if (blockCounter > 0)
                    _underAttackExpirationTicks = GlobalVars.Config.UnderAttackExpirationTicks;
                else
                {
                    _underAttackExpirationTicks--;

                    if (_underAttackExpirationTicks <= 0)
                    {
                        _underAttack = false;
                        Console.WriteLine("UAM is now disabled, no more abuses detected");
                    }
                }
            }
        }

        private static void ProcessBlock()
        {
            var currentTimestamp = Utilities.GetCurrentTimestamp();

            while (true)
            {
                if (!GlobalVars.Data.BlockQueue.TryPeek(out var blockStruct))
                {
                    break;
                }

                if (blockStruct.ExpirationTime < currentTimestamp)
                {
                    CloudflareUtilities.Unblock(blockStruct.BlockId);
                    Console.WriteLine($"Unblocked IP: {blockStruct.IpAddress} (Id = {blockStruct.BlockId})");

                    GlobalVars.Data.BlockQueue.Dequeue();
                    GlobalVars.Data.BlockHashSet.Remove(blockStruct.IpAddress);
                    
                    _dataChanged = true;
                }
                else
                {
                    break;
                }
            }
        }
    }
}
