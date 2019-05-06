namespace Kumo.Structs
{
	struct BlockStruct
	{
		public string IpAddress;
		public int ExpirationTime;
		public string BlockId;

		public BlockStruct(string ipAddress, int expirationTime)
		{
			IpAddress = ipAddress;
			ExpirationTime = expirationTime;
			BlockId = null;
		}
	}
}
