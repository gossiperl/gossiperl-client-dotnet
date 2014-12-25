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
			log4net.ILog Log = log4net.LogManager.GetLogger( typeof( MainClass ) );
			Log.Info ("Doing something!");
			OverlayConfiguration config = new OverlayConfiguration () {
				ClientName = "test-dotnet-client",
				ClientPort = 54321,
				ClientSecret = "dotnet-client-secret",
				OverlayName = "gossiperl_overlay_remote",
				OverlayPort = 6666,
				SymmetricKey = "v3JElaRswYgxOt4b"
			};
			OverlayWorker worker = new OverlayWorker (config);
			worker.Start ();
		}
	}
}
