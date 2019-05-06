using System.Collections.Generic;

namespace Kumo.Structs
{
	struct ConfigStruct
	{
		public string CloudflareEmail;
		public string CloudflareApiKey;
		public string BlockNote;
		public int BlockRange;

		public string WatcherTargetFile;
		public int WatcherCheckSleep;

		public int AbuseExpirationTime;
		public int BlockExpirationTime;
		
		public int BlocksToUnderAttack;
		public int UnderAttackExpirationTicks;

		public int AbusesToBlock;
		public int AbusesToBlockUnderAttack;

		public string NginxBlockSnippetFile;
	}
}
