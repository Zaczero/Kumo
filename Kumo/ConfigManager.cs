using Kumo.Structs;
using Newtonsoft.Json;
using System.IO;

namespace Kumo
{
	public static class ConfigManager
	{
		public static ConfigStruct ReadConfig(string path)
		{
			var json = File.ReadAllText(path);
			return MakeBackwardsCompatible(JsonConvert.DeserializeObject<ConfigStruct>(json));
		}

		public static void SaveConfig(string path, ConfigStruct config)
		{
			var json = JsonConvert.SerializeObject(config, Formatting.Indented);
			File.WriteAllText(path, json);
		}

		private static ConfigStruct MakeBackwardsCompatible(ConfigStruct config)
		{
			var defaultConfig = GetDefaultConfig();

			if (string.IsNullOrEmpty(config.NginxReloadBashCommand))
				config.NginxReloadBashCommand = defaultConfig.NginxReloadBashCommand;

			return config;
		}

		public static ConfigStruct GetDefaultConfig()
		{
			return new ConfigStruct
			{
				CloudflareEmail = string.Empty,
				CloudflareApiKey = string.Empty,

				CloudflareUnderAttackMode = false,
				CloudflareModeDefault = "high",
				CloudflareManageZones = new string[0],

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
				NginxReloadBashCommand = "nginx -s reload",
			};
		}
	}
}
