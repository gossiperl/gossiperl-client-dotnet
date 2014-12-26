using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Gossiperl.Client.Encryption;
using Gossiperl.Client.Serialization;
using Gossiperl.Client.Thrift;
using System.Net.Sockets;
using System.Net;
using Thrift.Protocol;

namespace Gossiperl.Client
{
	#region Messaging

	public class Messaging
	{
		private static log4net.ILog Log = log4net.LogManager.GetLogger( typeof( Messaging ) );

		private OverlayWorker worker;
		private Udp trasport;

		private BlockingCollection<DeserializeResult> incomingQueue;
		private BlockingCollection<OutgoingData> outgoingQueue;

		#region Events

		public delegate void EventHandler(string eventType, object member, long heartbeat);
		public event EventHandler Event;

		private void OnEvent(string eventType, object member, long heartbeat)
		{
			if (Event != null) {
				Event (eventType, member, heartbeat);
			}
		}

		public delegate void SubscribeAckHandler(List<string> events);
		public event SubscribeAckHandler SubscribeAck;

		private void OnSubscribeAck(List<string> events)
		{
			if (SubscribeAck != null) {
				SubscribeAck (events);
			}
		}

		public delegate void UnsubscribeAckHandler(List<string> events);
		public event UnsubscribeAckHandler UnsubscribeAck;

		private void OnUnsubscribeAck(List<string> events)
		{
			if (UnsubscribeAck != null) {
				UnsubscribeAck (events);
			}
		}

		public delegate void ForwardAckHandler(string replyId);
		public event ForwardAckHandler ForwardAck;

		private void OnForwardAck(string replyId)
		{
			if (ForwardAck != null) {
				ForwardAck (replyId);
			}
		}

		public delegate void DigestAckHandler(DigestAck digest);
		public event DigestAckHandler DigestAckReceived;

		private void OnDigestAck(DigestAck digest)
		{
			if (DigestAckReceived != null) {
				DigestAckReceived (digest);
			}
		}

		public delegate void ForwardedHandler(string digestType, byte[] binaryEnvelope, string envelopeId);
		public event ForwardedHandler Forwarded;

		private void OnForwarded(string digestType, byte[] binaryEnvelope, string envelopeId)
		{
			if (Forwarded != null) {
				Forwarded (digestType, binaryEnvelope, envelopeId);
			}
		}

		public delegate void FailedHandler(Gossiperl.Client.Exceptions.GossiperlClientException error);
		public event FailedHandler Failed;

		private void OnFailed(Gossiperl.Client.Exceptions.GossiperlClientException error)
		{
			if (Failed != null) {
				Failed (error);
			}
		}

		#endregion

		#region Event handlers
		private void OnTransportData(DeserializeResult result)
		{
			incomingQueue.Add (result);
		}
		#endregion

		public Messaging (OverlayWorker worker)
		{
			this.worker = worker;
			this.incomingQueue = new BlockingCollection<DeserializeResult> ();
			this.outgoingQueue = new BlockingCollection<OutgoingData> ();
			this.trasport = new Udp (this.worker);
			this.trasport.Data += new Udp.DataHandler (OnTransportData);
			Log.Info ("[" + worker.Configuration.ClientName + "] Messaging initialized.");
		}

		public void Start()
		{
			this.trasport.Start ();
			(new Thread (new ThreadStart (OutgoingQueueWorker))).Start ();
			(new Thread (new ThreadStart (IncomingQueueWorker))).Start ();
			Log.Info ("[" + worker.Configuration.ClientName + "] Messaging started.");
		}

		public void Send(string digestType, List<CustomDigestField> digestData)
		{
			Log.Debug ("[" + worker.Configuration.ClientName + "] Putting the custom digest on the outgoing queue.");
			this.outgoingQueue.Add (new OutgoingData(digestType, digestData));
		}

		public void Send(TBase digest)
		{
			Log.Debug ("[" + worker.Configuration.ClientName + "] Putting the digest on the outgoing queue.");
			this.outgoingQueue.Add (new OutgoingData (digest));
		}

		private void OutgoingQueueWorker()
		{
			while (worker.IsWorking) {
				OutgoingData data = outgoingQueue.Take ();
				if (data.DataType == OutgoingDataType.Digest) {
					trasport.Send (data.Digest);
				} else if (data.DataType == OutgoingDataType.Arbitrary) {
					trasport.Send (data.DigestType, data.ArbitraryData);
				} else if (data.DataType == OutgoingDataType.KillPill) {
					break;
				}
			}
		}

		private void IncomingQueueWorker()
		{
			while (worker.IsWorking) {
				DeserializeResult data = incomingQueue.Take ();
				if (data is DeserializeResultOK) {
					TBase digest = ((DeserializeResultOK)data).Digest;
					if (digest is Digest) {
						DigestAck ((Digest)digest);
					} else if (digest is DigestAck) {
						OnDigestAck ((DigestAck)digest);
					} else if (digest is DigestEvent) {
						DigestEvent eventDigest = (DigestEvent)digest;
						OnEvent (eventDigest.Event_type, eventDigest.Event_object, eventDigest.Heartbeat);
					} else if (digest is DigestSubscribeAck) {
						OnSubscribeAck (((DigestSubscribeAck)digest).Event_types);
					} else if (digest is DigestUnsubscribeAck) {
						OnUnsubscribeAck (((DigestUnsubscribeAck)digest).Event_types);
					} else if (digest is DigestForwardedAck) {
						OnForwardAck (((DigestForwardedAck)digest).Reply_id);
					} else {
						OnFailed (new Gossiperl.Client.Exceptions.GossiperlClientException ("Unknown digest type " + ((DeserializeResultOK)data).DigestType));
					}
				} else if (data is DeserializeResultError) {
					OnFailed (((DeserializeResultError)data).Cause);
				} else if (data is DeserializeResultForward) {
					OnForwarded (((DeserializeResultForward)data).DigestType, ((DeserializeResultForward)data).BinaryEnvelope, ((DeserializeResultForward)data).EnvelopeId);
					DigestForwardedAck (((DeserializeResultForward)data).EnvelopeId);
				} else if (data is DeserializeResultKillPill) {
					Log.Info ("[" + worker.Configuration.ClientName + "] Received request to stop icoming queue worker. Stopping.");
					break;
				} else {
					Log.Error ("[" + worker.Configuration.ClientName + "] Skiiping unknown incoming message: " + data.GetType().Name);
				}
			}
		}

		#region Digests

		public void DigestAck(Digest digest)
		{
			DigestAck ack = new DigestAck ();
			ack.Name = worker.Configuration.ClientName;
			ack.Heartbeat = Util.GetTimestamp ();
			ack.Reply_id = digest.Id;
			ack.Membership = new List<DigestMember> ();
			Send (ack);
		}

		public void DigestForwardedAck(string digestId)
		{
			DigestForwardedAck ack = new DigestForwardedAck ();
			ack.Name = worker.Configuration.ClientName;
			ack.Reply_id = digestId;
			ack.Secret = worker.Configuration.ClientSecret;
			Send (ack);
		}

		public bool DigestExit()
		{
			DigestExit digest = new DigestExit ();
			digest.Name = worker.Configuration.ClientName;
			digest.Heartbeat = Util.GetTimestamp ();
			digest.Secret = worker.Configuration.ClientSecret;
			Send (digest);
			return true;
		}

		public void DigestSubscribe(List<string> events)
		{
			DigestSubscribe digest = new DigestSubscribe ();
			digest.Event_types = events;
			digest.Heartbeat = Util.GetTimestamp ();
			digest.Id = System.Guid.NewGuid ().ToString ();
			digest.Name = worker.Configuration.ClientName;
			digest.Secret = worker.Configuration.ClientSecret;
			Send (digest);
		}

		public void DigestUnsubscribe(List<string> events)
		{
			DigestUnsubscribe digest = new DigestUnsubscribe ();
			digest.Event_types = events;
			digest.Heartbeat = Util.GetTimestamp ();
			digest.Id = System.Guid.NewGuid ().ToString ();
			digest.Name = worker.Configuration.ClientName;
			digest.Secret = worker.Configuration.ClientSecret;
			Send (digest);
		}

		#endregion

		#region Support
		public enum OutgoingDataType {
			Arbitrary,
			Digest,
			KillPill
		}

		public class OutgoingData {
			private OutgoingDataType type;
			private TBase digest;
			private System.Collections.Generic.List<CustomDigestField> arbitraryData;
			private string arbitraryType;
			public OutgoingData(OutgoingDataType type)
			{
				if ( type != OutgoingDataType.KillPill ) {
					throw new Gossiperl.Client.Exceptions.GossiperlClientException("Single argument constructor valid only for KillPill.");
				}
				this.type = type;
			}
			public OutgoingData(TBase digest)
			{
				if ( digest == null ) {
					throw new Gossiperl.Client.Exceptions.GossiperlClientException("Digest can't be null.");
				}
				this.type = OutgoingDataType.Digest;
				this.digest = digest;
			}
			public OutgoingData(string digestType, System.Collections.Generic.List<CustomDigestField> arbitraryData)
			{
				if ( digestType == null ) {
					throw new Gossiperl.Client.Exceptions.GossiperlClientException("Digest type can't be null.");
				}
				if ( arbitraryData == null ) {
					throw new Gossiperl.Client.Exceptions.GossiperlClientException("Arbitrary data can't be null.");
				}
				this.type = OutgoingDataType.Arbitrary;
				this.arbitraryType = digestType;
				this.arbitraryData = arbitraryData;
			}

			public string DigestType {
				get {
					if (type == OutgoingDataType.Arbitrary) {
						return this.arbitraryType;
					} else if (type == OutgoingDataType.Digest) {
						return this.digest.GetType ().Name;
					}
					return null;
				}
			}

			public OutgoingDataType DataType {
				get {
					return this.type;
				}
			}

			public TBase Digest {
				get {
					return this.digest;
				}
			}

			public System.Collections.Generic.List<CustomDigestField> ArbitraryData {
				get {
					return this.arbitraryData;
				}
			}

		}
		#endregion
	}

	#endregion

	#region Transport

	public class Udp
	{
		private static log4net.ILog Log = log4net.LogManager.GetLogger( typeof( Udp ) );

		private OverlayWorker worker;
		private Serializer serializer;
		private Aes256 encryption;
		private Socket udpSock;
		private byte[] buffer;

		#region Events

		public delegate void DataHandler(DeserializeResult result);
		public event DataHandler Data;

		private void OnData(DeserializeResult result)
		{
			if (Data != null) {
				Data (result);
			}
		}

		#endregion

		public Udp(OverlayWorker worker)
		{
			this.worker = worker;
			this.serializer = new Serializer ();
			this.encryption = new Aes256 (worker.Configuration.SymmetricKey);
		}

		public void Start()
		{
			buffer = new byte[ worker.Configuration.ThriftWindowSize ];
			udpSock = new Socket (AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			udpSock.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), worker.Configuration.ClientPort));
			Log.Info ("[" + worker.Configuration.ClientName + "] Datagram server started on port " + worker.Configuration.ClientPort + ".");
			EndPoint newClientEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), worker.Configuration.OverlayPort);
			udpSock.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref newClientEP, DoReceiveFrom, udpSock);
		}

		private void DoReceiveFrom(IAsyncResult iar){
			try
			{
				Socket recvSock = (Socket)iar.AsyncState;
				EndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);
				int msgLen = recvSock.EndReceiveFrom(iar, ref clientEP);
				byte[] localMsg = new byte[msgLen];
				Array.Copy(buffer, localMsg, msgLen);
				if (worker.IsWorking) {
					//Start listening for a new message.
					EndPoint newClientEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), worker.Configuration.OverlayPort);
					udpSock.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref newClientEP, DoReceiveFrom, udpSock);

					try
					{
						byte[] decrypted = encryption.Decrypt(localMsg);
						OnData( serializer.Deserialize(decrypted) );
					}
					catch (Gossiperl.Client.Exceptions.GossiperlClientException ex)
					{
						OnData( new DeserializeResultError(ex) );
					}
					catch (Exception ex)
					{
						OnData( new DeserializeResultError( new Gossiperl.Client.Exceptions.GossiperlClientException("Error while reading sent data.", ex) ) );
					}

				} else {
					udpSock.Dispose ();
					Log.Info("[" + worker.Configuration.ClientName + "] Worker is stopping. Stopping transport.");
				}

			} catch (ObjectDisposedException ex) {
				OnData ( new DeserializeResultError(new Gossiperl.Client.Exceptions.GossiperlClientException("There was a problem while receiving the data.", ex)) );
			}
		}

		public void Send(string digestType, List<CustomDigestField> digestData)
		{
			byte[] serialized = this.serializer.SerializeArbitrary (digestType, digestData);
			byte[] encrypted = this.encryption.Encrypt (serialized);
			Log.Debug ("[" + worker.Configuration.ClientName + "] Attempting sending arbitrary digest " + digestType + " out.");
			Send (encrypted);
		}

		public void Send(TBase digest)
		{
			byte[] serialized = this.serializer.Serialize (digest);
			byte[] encrypted = this.encryption.Encrypt (serialized);
			Log.Debug ("[" + worker.Configuration.ClientName + "] Attempting sending digest " + digest.GetType().Name + " out.");
			Send (encrypted);
		}

		public void Send(byte[] digestData)
		{
			EndPoint ep = new IPEndPoint (IPAddress.Parse ("127.0.0.1"), worker.Configuration.OverlayPort);
			udpSock.SendTo(digestData, ep);
			Log.Debug ("[" + worker.Configuration.ClientName + "] Digest data sent to 127.0.0.1:" + worker.Configuration.OverlayPort + ".");
		}
	}

	#endregion
}

