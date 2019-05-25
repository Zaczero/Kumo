using System.Collections.Generic;
using Kumo.Structs;
using Newtonsoft.Json;
using System.IO;

namespace Kumo
{
	class ConfigManager
	{
		public static ConfigStruct ReadConfig(string path)
		{
			var json = File.ReadAllText(path);
			return JsonConvert.DeserializeObject<ConfigStruct>(json);
		}

		public static void SaveConfig(string path, ConfigStruct config)
		{
			var json = JsonConvert.SerializeObject(config, Formatting.Indented);
			File.WriteAllText(path, json);
		}

		public static ConfigStruct GetDefaultConfig()
		{
			return new ConfigStruct
			{
				CloudflareEmail = string.Empty,
				CloudflareApiKey = string.Empty,

				CloudflareUnderAttackMode = false,
				CloudflareModeDefault = "high",
				CloudflareManageZones = new List<string>(),

				BlockNote = "Created by Kumo",

				WatcherTargetFile = "/var/log/nginx/error.log",
				WatcherCheckSleep = 2_000,
				
				AbuseExpirationTime = 300,
				BlockExpirationTime = 10800,

				BlocksToUnderAttack = 3,
				UnderAttackExpirationTicks = 30,

				AbusesToBlock = 8,
				AbusesToBlockUnderAttack = 2,

				NginxBlockSnippetFile = "/etc/nginx/snippets/kumo.conf",
			};
		}
	}
}
