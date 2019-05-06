using System.Collections.Generic;

namespace Kumo.Structs
{
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
}
