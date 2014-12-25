using System;
using Gossiperl.Client.Serialization;
using Gossiperl.Client.Thrift;
using Thrift.Protocol;

namespace gossiperlclientdotnetexec
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ( Gossiperl.Client.Util.GetTimestamp() );
		}
	}
}
