namespace Kumo.Structs
{
	public struct BlockStruct
	{
		public readonly string IpAddress;
		public readonly int ExpirationTime;
		public string BlockId;

		public BlockStruct(string ipAddress, int expirationTime)
		{
			IpAddress = ipAddress;
			ExpirationTime = expirationTime;
			BlockId = null;
		}
	}
}
