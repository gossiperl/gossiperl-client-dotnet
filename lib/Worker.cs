using System;
using System.Threading;
using Gossiperl.Client.Thrift;
using Thrift.Protocol;
using System.Collections.Generic;

namespace Gossiperl.Client
{

	public class OverlayWorker
	{
		private OverlayConfiguration configuration;
		private bool working;
		private State state;
		private Messaging messaging;
		private static log4net.ILog Log = log4net.LogManager.GetLogger( typeof( OverlayWorker ) );

		#region Events

		public delegate void ConnectedHandler(OverlayWorker worker);
		public event ConnectedHandler Connected;

		public delegate void DisconnectedHandler(OverlayWorker worker);
		public event DisconnectedHandler Disconnected;

		public delegate void EventHandler(OverlayWorker worker, string eventType, object member, long heartbeat);
		public event EventHandler Event;

		public delegate void SubscribeAckHandler(OverlayWorker worker, List<string> events);
		public event SubscribeAckHandler SubscribeAck;

		public delegate void UnsubscribeAckHandler(OverlayWorker worker, List<string> events);
		public event UnsubscribeAckHandler UnsubscribeAck;

		public delegate void ForwardAckHandler(OverlayWorker worker, string replyId);
		public event ForwardAckHandler ForwardAck;

		public delegate void ForwardedHandler(OverlayWorker worker, string digestType, byte[] binaryEnvelope, string envelopeId);
		public event ForwardedHandler Forwarded;

		public delegate void FailedHandler(OverlayWorker worker, Gossiperl.Client.Exceptions.GossiperlClientException error);
		public event FailedHandler Failed;

		public delegate void StoppedHandler(OverlayWorker worker);
		public event StoppedHandler Stopped;

		private void OnStopped()
		{
			if (Stopped != null) {
				Stopped (this);
			}
		}

		#endregion

		#region Event listeners

		private void MessagingEvent(string eventType, object member, long heartbeat)
		{
			if (Event != null) {
				Event (this, eventType, member, heartbeat);
			}
		}

		private void MessagingSubscribeAck(List<string> events)
		{
			if (SubscribeAck != null) {
				SubscribeAck (this, events);
			}
		}

		private void MessagingUnsubscribeAck(List<string> events)
		{
			if (UnsubscribeAck != null) {
				UnsubscribeAck (this, events);
			}
		}

		private void MessagingForwardedAck(string replyId)
		{
			if (ForwardAck != null) {
				ForwardAck (this, replyId);
			}
		}

		private void MessagingFailed(Gossiperl.Client.Exceptions.GossiperlClientException error)
		{
			if (Failed != null) {
				Failed (this, error);
			}
		}

		private void MessagingForwarded(string digestType, byte[] binaryEnvelope, string envelopeid)
		{
			if (Forwarded != null) {
				Forwarded (this, digestType, binaryEnvelope, envelopeid);
			}
		}

		private void MessagingDigestAckReceived(DigestAck digest)
		{
			state.Receive (digest);
		}

		private void StateConnected()
		{
			if (Connected != null) {
				Connected (this);
			}
		}

		private void StateDisconnected()
		{
			if (Disconnected != null) {
				Disconnected (this);
			}
		}

		#endregion

		public OverlayWorker (OverlayConfiguration configuration)
		{
			this.configuration = configuration;

			this.messaging = new Messaging (this);
			this.messaging.Event += new Messaging.EventHandler (MessagingEvent);
			this.messaging.SubscribeAck += new Messaging.SubscribeAckHandler (MessagingSubscribeAck);
			this.messaging.UnsubscribeAck += new Messaging.UnsubscribeAckHandler (MessagingUnsubscribeAck);
			this.messaging.ForwardAck += new Messaging.ForwardAckHandler (MessagingForwardedAck);
			this.messaging.Failed += new Messaging.FailedHandler (MessagingFailed);
			this.messaging.Forwarded += new Messaging.ForwardedHandler (MessagingForwarded);
			this.messaging.DigestAckReceived += new Messaging.DigestAckHandler (MessagingDigestAckReceived);

			this.state = new State (this);
			this.state.Connected += new State.ConnectedHandler (StateConnected);
			this.state.Disconnected += new State.DisconnectedHandler (StateDisconnected);

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

		public void Stop()
		{
			if (messaging.DigestExit ()) {
				working = false;
				OnStopped ();
			}
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

		public State State {
			get {
				return this.state;
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

		#region Events

		public delegate void ConnectedHandler();
		public event ConnectedHandler Connected;

		public delegate void DisconnectedHandler();
		public event DisconnectedHandler Disconnected;

		protected virtual void OnConnected()
		{
			if (Connected != null) {
				Connected ();
			}
		}

		protected virtual void OnDisconnected()
		{
			if (Disconnected != null) {
				Disconnected ();
			}
		}

		#endregion

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

		public void Receive(DigestAck ack)
		{
			if (currentStatus == Status.Disconnected) {
				OnConnected ();
				if (subscriptions.Count > 0) {
					worker.Messaging.DigestSubscribe (subscriptions);
				}
			}
			currentStatus = Status.Connected;
			lastTs = ack.Heartbeat;
		}

		private void SendDigest()
		{
			Digest digest = new Digest ();
			digest.Secret = worker.Configuration.ClientSecret;
			digest.Id = System.Guid.NewGuid ().ToString ();
			digest.Heartbeat = Util.GetTimestamp ();
			digest.Port = worker.Configuration.ClientPort;
			digest.Name = worker.Configuration.ClientName;
			Log.Debug ("[" + worker.Configuration.ClientName + "] Offering digest " + digest.Id + ".");
			worker.Messaging.Send ( digest );
		}

		public List<string> Subscribe(List<string> events)
		{
			worker.Messaging.DigestSubscribe (events);
			subscriptions.AddRange (events);
			return subscriptions;
		}

		public List<string> Unsubscribe(List<string> events)
		{
			worker.Messaging.DigestUnsubscribe (events);
			subscriptions.RemoveAll (e => events.Contains(e));
			return subscriptions;
		}

		public Status CurrentStatus
		{
			get {
				return currentStatus;
			}
		}

		public List<string> Subscriptions
		{
			get {
				return subscriptions;
			}
		}

		private void StatusWorker()
		{
			Thread.Sleep (1000);
			while (worker.IsWorking) {
				SendDigest ();
				if (Util.GetTimestamp () - lastTs > 5) {
					if (currentStatus == Status.Connected) {
						Log.Debug ("[" + worker.Configuration.ClientName + "] Accouncing disconnected.");
						OnDisconnected ();
					}
					currentStatus = Status.Disconnected;
				}
				Thread.Sleep (2000);
			}
			Log.Debug ("[" + worker.Configuration.ClientName + "] Accouncing disconnected. Worker stopped.");
			OnDisconnected ();
			currentStatus = Status.Disconnected;
			Log.Info ("[" + worker.Configuration.ClientName + "] Stopping state service.");
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

