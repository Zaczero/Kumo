using System.Collections.Generic;

namespace Kumo.Structs
{
	public readonly struct AbuseStruct
	{
		public readonly string IpAddress;
		public readonly Queue<int> Timestamps;

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
