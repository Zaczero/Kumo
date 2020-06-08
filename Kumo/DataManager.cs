using Kumo.Structs;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Kumo
{
	public static class DataManager
	{
		public static DataStruct ReadData(string path)
		{
			var json = File.ReadAllText(path);
			return JsonConvert.DeserializeObject<DataStruct>(json);
		}

		public static void SaveData(string path, DataStruct value)
		{
			var json = JsonConvert.SerializeObject(value, Formatting.None);
			File.WriteAllText(path, json);
		}

		public static DataStruct GetDefaultData()
		{
			return new DataStruct
			{
				BlockQueue = new Queue<BlockStruct>(),
				BlockHashSet = new HashSet<string>(),
			};
		}
	}
}
