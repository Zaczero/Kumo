using System.Collections.Generic;

namespace Kumo.Structs
{
	struct DataStruct
	{
		public long WatcherStreamPosition;

		public Queue<BlockStruct> BlockQueue;
		public HashSet<string> BlockHashSet;
	}
}
