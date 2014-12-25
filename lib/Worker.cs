using System;
using System.Threading;
using Gossiperl.Client.Thrift;
using Thrift.Protocol;

namespace Gossiperl.Client
{

	public class OverlayWorker
	{
		private OverlayConfiguration configuration;
		private bool working;

		private State state;
		private Messaging messaging;

		private static log4net.ILog Log = log4net.LogManager.GetLogger( typeof( OverlayWorker ) );  

		public OverlayWorker (OverlayConfiguration configuration)
		{
			this.configuration = configuration;
			this.messaging = new Messaging (this);
			this.state = new State (this);
			this.working = true;
			Log.Info ("[" + Configuration.ClientName + "] Overlay worker initialized.");
		}

		public void Start()
		{
			Thread t = new Thread (new ThreadStart (OverlayWorkerTask));
			t.Start ();
			Log.Info ("[" + Configuration.ClientName + "] Overlay worker started.");
			t.Join ();
		}

		private void OverlayWorkerTask()
		{
			this.messaging.Start ();
			this.state.Start ();
		}

		public Messaging Messaging {
			get {
				return this.messaging;
			}
		}

		public bool IsWorking {
			get {
				return this.working;
			}
		}

		public OverlayConfiguration Configuration {
			get {
				return this.configuration;
			}
		}
	}

	#region State

	public enum Status {
		Connected,
		Disconnected
	}

	public class State
	{
		private OverlayWorker worker;
		private System.Collections.Generic.List<string> subscriptions;
		private Status currentStatus;
		private long lastTs = 0;

		private static log4net.ILog Log = log4net.LogManager.GetLogger( typeof( State ) );  

		public State(OverlayWorker worker)
		{
			this.subscriptions = new System.Collections.Generic.List<string> ();
			this.currentStatus = Status.Disconnected;
			this.worker = worker;
			Log.Info ("[" + worker.Configuration.ClientName + "] State initialized.");
		}

		public void Start()
		{
			(new Thread(new ThreadStart(StatusWorker))).Start();
			Log.Info ("[" + worker.Configuration.ClientName + "] State started.");
		}

		private void SendDigest()
		{
			Digest digest = new Digest ();
			digest.Secret = worker.Configuration.ClientSecret;
			digest.Id = System.Guid.NewGuid ().ToString ();
			digest.Heartbeat = Util.GetTimestamp ();
			digest.Port = worker.Configuration.ClientPort;
			digest.Name = worker.Configuration.ClientName;
			Log.Info ("[" + worker.Configuration.ClientName + "] Offering digest " + digest.Id + ".");
			worker.Messaging.Send ( digest );
		}

		private void StatusWorker()
		{
			Thread.Sleep (1000);
			while (worker.IsWorking) {
				SendDigest ();
				if (Util.GetTimestamp () - lastTs > 5) {
					if (currentStatus == Status.Connected) {
						// TODO: notify about disconnection
					}
					currentStatus = Status.Disconnected;
				}
				Thread.Sleep (2000);
			}
			// TODO: notify about disconnection
			currentStatus = Status.Disconnected;
		}

	}

	#endregion

	#region Overlay configuration

	public class OverlayConfiguration
	{
		private string overlayName;
		private string clientName;
		private string clientSecret;
		private string symmetricKey;
		private int overlayPort;
		private int clientPort;
		private int thriftWindowSize = 16777216;

		public string OverlayName {
			get {
				return this.overlayName;
			}
			set {
				this.overlayName = value;
			}
		}

		public string ClientName {
			get {
				return this.clientName;
			}
			set {
				this.clientName = value;
			}
		}

		public string ClientSecret {
			get {
				return this.clientSecret;
			}
			set {
				this.clientSecret = value;
			}
		}

		public string SymmetricKey {
			get {
				return this.symmetricKey;
			}
			set {
				this.symmetricKey = value;
			}
		}

		public int OverlayPort {
			get {
				return this.overlayPort;
			}
			set {
				this.overlayPort = value;
			}
		}

		public int ClientPort {
			get {
				return this.clientPort;
			}
			set {
				this.clientPort = value;
			}
		}

		public int ThriftWindowSize {
			get {
				return this.thriftWindowSize;
			}
			set {
				this.thriftWindowSize = value;
			}
		}

	}

	#endregion
}

