using System;
using Gossiperl.Client;
using Gossiperl.Client.Encryption;
using Thrift.Protocol;
using System.Reflection;
using System.IO;

namespace gossiperlclientdotnetexec
{
	class MainClass
	{

		public static void Main (string[] args)
		{
			OverlayConfiguration config = new OverlayConfiguration () {
				ClientName = "test-dotnet-client",
				ClientPort = 54321,
				ClientSecret = "dotnet-client-secret",
				OverlayName = "gossiperl_overlay_remote",
				OverlayPort = 6666,
				SymmetricKey = "v3JElaRswYgxOt4b"
			};
			Supervisor supervisor = new Supervisor ();
			supervisor.Connect (config);
		}
	}
}
