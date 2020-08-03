using System;
using System.IO;
using System.Text;

namespace Kumo
{
	public static class Utilities
	{
		public static int GetCurrentTimestamp()
		{
			var date = DateTime.Now;

			return GetTimestamp(
				(short) date.Year,
				(byte) date.Month,
				(byte) date.Day,
				(byte) date.Hour,
				(byte) date.Minute,
				(byte) date.Second);
		}

		public static int GetTimestamp(short year, byte month, byte day, byte hour, byte minute, byte second)
		{
			// values are rounded for maximum performance
			// we don't care if month has 28 or 31 days
			const int perMinute = 60;
			const int perHour = perMinute * 60;
			const int perDay = perHour * 24;
			const int perMonth = perDay * 31;
			const int perYear = perMonth * 12;

			return (year - 2018) * perYear +
				   month * perMonth +
				   day * perDay +
				   hour * perHour +
				   minute * perMinute +
				   second;
		}

		public static void SaveNginxSnippet()
		{
			var sb = new StringBuilder("# " + GlobalVars.Config.BlockNote + Environment.NewLine);

			foreach (var blockStruct in GlobalVars.Data.BlockQueue)
			{
				sb.AppendLine($"deny {blockStruct.IpAddress};");
			}

			File.WriteAllText(GlobalVars.Config.NginxBlockSnippetFile, sb.ToString());
			GlobalVars.Config.NginxReloadBashCommand.Bash();
		}
	}
}
