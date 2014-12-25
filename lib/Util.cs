using System;

namespace Gossiperl.Client
{
	public class Util
	{
		public static long GetTimestamp() {
			Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
			return (long)unixTimestamp;
		}
	}
}

