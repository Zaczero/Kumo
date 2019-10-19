using System.Collections.Generic;

namespace Kumo.Structs
{
	struct ConfigStruct
	{
		public string CloudflareEmail;
		public string CloudflareApiKey;

		public bool CloudflareUnderAttackMode;
		public string CloudflareModeDefault;
		public List<string> CloudflareManageZones;

		public string BlockNote;

		public string WatcherTargetFile;
		public int WatcherCheckSleep;

		public int AbuseExpirationTime;
		public int BlockExpirationTime;
		
		public int BlocksToUnderAttack;
		public int UnderAttackExpirationTicks;

		public int AbusesToBlock;
		public int AbusesToBlockUnderAttack;

		public string NginxBlockSnippetFile;
		public string NginxReloadBashCommand;
	}
}
