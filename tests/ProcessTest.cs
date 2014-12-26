using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using Gossiperl.Client;

namespace Gossiperl.Client.Tests
{
	[TestFixture]
	public class ProcessTest
	{
		private Supervisor supervisor;
		private OverlayConfiguration configuration;
		private List<string> subscriptions;

		[SetUp] public void SetUp()
		{
			configuration = new OverlayConfiguration () {
				ClientName = "test-dotnet-client",
				ClientPort = 54321,
				ClientSecret = "dotnet-client-secret",
				OverlayName = "gossiperl_overlay_remote",
				OverlayPort = 6666,
				SymmetricKey = "v3JElaRswYgxOt4b"
			};
			supervisor = new Supervisor ();
			subscriptions = new List<string> ();
			subscriptions.Add ("member_in");
			subscriptions.Add ("digestForwardableTest");
		}

		[Test] public void TestProcess ()
		{
			supervisor.Connect (configuration);
			Thread.Sleep (3000);
			Assert.AreEqual (supervisor.CurrentStatus (configuration.OverlayName), Status.Connected );
			Assert.Catch (new TestDelegate (() => supervisor.Connect (configuration)));
			Assert.AreEqual (supervisor.Subscribe (configuration.OverlayName, subscriptions), subscriptions);
			Thread.Sleep (3000);
			Assert.AreEqual (supervisor.Unsubscribe (configuration.OverlayName, subscriptions), new List<string>());
			Thread.Sleep (3000);
			supervisor.Disconnect (configuration.OverlayName);
			Assert.AreEqual (supervisor.NumberOfConnections, 0);
		}

		[Test] public void TestNonExistingOverlay()
		{
			Assert.Catch (new TestDelegate (() => supervisor.CurrentStatus( "non_existing" )));
		}
	}
}

