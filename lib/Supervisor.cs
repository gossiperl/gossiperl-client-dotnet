using System;
using System.Collections.Generic;
using Gossiperl.Client.Serialization;

namespace Gossiperl.Client
{
	public class Supervisor
	{
		private static log4net.ILog Log = log4net.LogManager.GetLogger( typeof( Supervisor ) );

		private Dictionary<string, OverlayWorker> connections;

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

		#endregion

		#region Event handlers

		private void OverlayWorkerConnected(OverlayWorker worker)
		{
			if (Connected != null) {
				Connected (worker);
			} else {
				Log.Info ("[" + worker.Configuration.ClientName + "] Connected.");
			}
		}

		private void OverlayWorkerDisconnected(OverlayWorker worker)
		{
			if (Disconnected != null) {
				Disconnected (worker);
			} else {
				Log.Info ("[" + worker.Configuration.ClientName + "] Disconnected.");
			}
		}

		private void OverlayWorkerEvent(OverlayWorker worker, string eventType, object member, long heartbeat)
		{
			if (Event != null) {
				Event (worker, eventType, member, heartbeat);
			} else {
				Log.Info("[" + worker.Configuration.ClientName + "] Received member " + member.ToString() + " event " + eventType + " at " + heartbeat + ".");
			}
		}

		private void OverlayWorkerSubscribeAck(OverlayWorker worker, List<string> events)
		{
			if (SubscribeAck != null) {
				SubscribeAck(worker, events);
			} else {
				System.Text.StringBuilder sb = new System.Text.StringBuilder ();
				foreach (string _event in events) {
					if (sb.Length > 0) {
						sb.Append (", ");
					}
					sb.Append (_event);
				}
				Log.Info ("[" + worker.Configuration.ClientName + "] Subscribed to " + sb.ToString() + ".");
			}
		}

		private void OverlayWorkerUnsubscribeAck(OverlayWorker worker, List<string> events)
		{
			if (UnsubscribeAck != null) {
				UnsubscribeAck(worker, events);
			} else {
				System.Text.StringBuilder sb = new System.Text.StringBuilder ();
				foreach (string _event in events) {
					if (sb.Length > 0) {
						sb.Append (", ");
					}
					sb.Append (_event);
				}
				Log.Info ("[" + worker.Configuration.ClientName + "] Unsubscribed from " + sb.ToString() + ".");
			}
		}

		private void OverlayWorkerForwardAck(OverlayWorker worker, string replyId)
		{
			if (ForwardAck != null) {
				ForwardAck (worker, replyId);
			} else {
				Log.Info ("[" + worker.Configuration.ClientName + "] Received confirmation of a forward message. Message ID: " + replyId + ".");
			}
		}

		private void OverlayWorkerForwarded(OverlayWorker worker, string digestType, byte[] binaryEnvelope, string envelopeId)
		{
			if (Forwarded != null) {
				Forwarded (worker, digestType, binaryEnvelope, envelopeId);
			} else {
				Log.Info ("[" + worker.Configuration.ClientName + "] Received forward digest " + digestType + ". Digest id: " + envelopeId + ".");
			}
		}

		private void OverlayWorkerFailed(OverlayWorker worker, Gossiperl.Client.Exceptions.GossiperlClientException error)
		{
			if (Failed != null) {
				Failed(worker, error);
			} else {
				Log.Info ("[" + worker.Configuration.ClientName + "] Encountered an error: " + error.Message + ".");
			}
		}

		private void OnOverlayWorkerStopped(OverlayWorker worker)
		{
			connections.Remove (worker.Configuration.OverlayName);
		}

		#endregion

		public Supervisor()
		{
			this.connections = new Dictionary<string, OverlayWorker> ();
		}

		public void Connect(OverlayConfiguration configuration)
		{
			if (IsConnection (configuration.OverlayName)) {
				throw new Gossiperl.Client.Exceptions.GossiperlClientException ("Client for " + configuration.OverlayName + " already present.");
			}
			OverlayWorker worker = new OverlayWorker (configuration);
			worker.Connected += new OverlayWorker.ConnectedHandler (OverlayWorkerConnected);
			worker.Disconnected += new OverlayWorker.DisconnectedHandler (OverlayWorkerDisconnected);
			worker.Event += new OverlayWorker.EventHandler (OverlayWorkerEvent);
			worker.Failed += new OverlayWorker.FailedHandler (OverlayWorkerFailed);
			worker.ForwardAck += new OverlayWorker.ForwardAckHandler (OverlayWorkerForwardAck);
			worker.Forwarded += new OverlayWorker.ForwardedHandler (OverlayWorkerForwarded);
			worker.Stopped += new OverlayWorker.StoppedHandler (OnOverlayWorkerStopped);
			worker.SubscribeAck += new OverlayWorker.SubscribeAckHandler (OverlayWorkerSubscribeAck);
			worker.UnsubscribeAck += new OverlayWorker.UnsubscribeAckHandler (OverlayWorkerUnsubscribeAck);
			connections.Add (configuration.OverlayName, worker);
			worker.Start ();
		}

		public void Disconnect(string overlayName)
		{
			if (IsConnection (overlayName)) {
				connections [overlayName].Stop ();
			} else {
				throw new Gossiperl.Client.Exceptions.GossiperlClientException ("[supervisor] No overlay connection: " + overlayName);
			}
		}

		public List<string> Subscribe(string overlayName, List<string> events) 
		{
			if (IsConnection (overlayName)) {
				return connections [overlayName].State.Subscribe (events);
			} else {
				throw new Gossiperl.Client.Exceptions.GossiperlClientException ("[supervisor] No overlay connection: " + overlayName);
			}
		}

		public List<string> Unsubscribe(string overlayName, List<string> events)
		{
			if (IsConnection (overlayName)) {
				return connections [overlayName].State.Unsubscribe (events);
			} else {
				throw new Gossiperl.Client.Exceptions.GossiperlClientException ("[supervisor] No overlay connection: " + overlayName);
			}
		}

		public void Send(string overlayName, string digestType, List<CustomDigestField> digestData)
		{
			if (IsConnection (overlayName)) {
				connections [overlayName].Messaging.Send (digestType, digestData);
			} else {
				throw new Gossiperl.Client.Exceptions.GossiperlClientException ("[supervisor] No overlay connection: " + overlayName);
			}
		}

		public DeserializeResult Read(string digestType, byte[] binDigest, List<CustomDigestField> digestInfo)
		{
			return (new Serializer ()).DeserializeArbitrary (digestType, binDigest, digestInfo);
		}

		public Status CurrentStatus(string overlayName)
		{
			if (IsConnection (overlayName)) {
				return connections [overlayName].State.CurrentStatus;
			} else {
				throw new Gossiperl.Client.Exceptions.GossiperlClientException ("[supervisor] No overlay connection: " + overlayName);
			}
		}

		public List<string> Subscriptions(string overlayName)
		{
			if (IsConnection (overlayName)) {
				return connections [overlayName].State.Subscriptions;
			} else {
				throw new Gossiperl.Client.Exceptions.GossiperlClientException ("[supervisor] No overlay connection: " + overlayName);
			}
		}

		public bool IsConnection(string overlayName)
		{
			return connections.ContainsKey (overlayName);
		}

		public void Stop()
		{
			foreach (OverlayWorker worker in connections.Values) {
				worker.Stop ();
			}
		}

		public int NumberOfConnections {
			get {
				return connections.Count;
			}
		}

	}
}

