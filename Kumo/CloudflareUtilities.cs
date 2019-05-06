using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Kumo
{
	class CloudflareUtilities
    {
        private const string BlockMode = "block";

		

        public static string Block(string ipAddress)
		{
            while (true)
            {
                try
                {
	                if (GlobalVars.Config.BlockRange > 0 && RegexPatterns.IpV4Regex.Match(ipAddress).Success)
	                {
		                ipAddress += "/" + GlobalVars.Config.BlockRange;
	                }

                    var request = new Dictionary<string, object>
                    {
                        ["mode"] = BlockMode,
                        ["configuration"] = new Dictionary<string, object> {{"target", "ip"}, {"value", ipAddress}},
                        ["notes"] = GlobalVars.Config.BlockNote,
                    };

                    var requestJson = JsonConvert.SerializeObject(request);
                    var response = GlobalVars.Http.SendAsync(
                        new HttpRequestMessage(HttpMethod.Post, "https://api.cloudflare.com/client/v4/user/firewall/access_rules/rules")
                        {
                            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
                        }).GetAwaiter().GetResult();

                    var responseHtml = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var responseObj = JsonConvert.DeserializeObject<dynamic>(responseHtml);

                    var success = (bool) responseObj.success;
                    if (success)
                    {
                        return (string) responseObj.result.id;
                    }

                    Debug.WriteLine($"Block() non-success response: {responseHtml}");
                    return null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Block() crashed: {ex.Message}");
                    Thread.Sleep(1_000);
                }
            }
        }

        public static void Unblock(string id)
        {
            while (true)
            {
                try
                {
                    var response = GlobalVars.Http.SendAsync(
                        new HttpRequestMessage(HttpMethod.Delete, "https://api.cloudflare.com/client/v4/user/firewall/access_rules/rules/" + id)
                        {
                            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
                        }).GetAwaiter().GetResult();

                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unblock() crashed: {ex.Message}");
                    Thread.Sleep(1_000);
                }
            }
        }
    }
}
