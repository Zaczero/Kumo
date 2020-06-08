using System.Text.RegularExpressions;

namespace Kumo
{
	public static class RegexPatterns
	{
		public static readonly Regex AbuseRegex = new Regex(@"^(?<year>\d{4})\/(?<month>\d{2})\/(?<day>\d{2}) (?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2}) \[error\] \d+#\d+: \*\d+ limiting requests, excess: \d+\.\d+ by zone "".*?"", client: (?<ip>[\da-f\.:]+)", RegexOptions.Compiled);
	}
}
