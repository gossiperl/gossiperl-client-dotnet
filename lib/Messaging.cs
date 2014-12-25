using System;
using System.Collections.Concurrent;
using System.Threading;
using Gossiperl.Client.Encryption;
using Gossiperl.Client.Serialization;
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

		public Messaging (OverlayWorker worker)
		{
			this.worker = worker;
			this.incomingQueue = new BlockingCollection<DeserializeResult> ();
			this.outgoingQueue = new BlockingCollection<OutgoingData> ();
			this.trasport = new Udp (this.worker);
			Log.Info ("[" + worker.Configuration.ClientName + "] Messaging initialized.");
		}

		public void Start()
		{
			this.trasport.Start ();
			(new Thread (new ThreadStart (OutgoingQueueWorker))).Start ();
			(new Thread (new ThreadStart (IncomingQueueWorker))).Start ();
			Log.Info ("[" + worker.Configuration.ClientName + "] Messaging started.");
		}

		public void Send(TBase digest)
		{
			Log.Info ("[" + worker.Configuration.ClientName + "] Putting the digest on the outgoing queue.");
			this.outgoingQueue.Add (new OutgoingData (digest));
		}

		private void OutgoingQueueWorker()
		{
			while (worker.IsWorking) {
				OutgoingData data = outgoingQueue.Take ();
				if (data.DataType == OutgoingDataType.Digest) {
					trasport.Send (data.Digest);
				} else if (data.DataType == OutgoingDataType.Arbitrary) {
				} else if (data.DataType == OutgoingDataType.KillPill) {
					break;
				}
			}
		}

		private void IncomingQueueWorker()
		{
			while (worker.IsWorking) {
				DeserializeResult data = incomingQueue.Take ();
				Log.Info ("Received some data! ::: " + data);
			}
		}

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
					//Handle the received message
					Log.Debug("Recieved " + msgLen + " bytes from " + ((IPEndPoint)clientEP).Address + ":" + ((IPEndPoint)clientEP).Port);
				} else {
					throw new NotImplementedException ("Verify this is stop.");
				}

			} catch (ObjectDisposedException) {
				throw new NotImplementedException ("Needs to publish a message to the incoming queue - this is an error");
			}
		}

		public void Send(TBase digest)
		{
			byte[] serialized = this.serializer.Serialize (digest);
			byte[] encrypted = this.encryption.Encrypt (serialized);
			Log.Info ("[" + worker.Configuration.ClientName + "] Attempting sending digest " + digest.GetType().Name + " out.");
			Send (encrypted);
		}

		public void Send(byte[] digestData)
		{
			EndPoint ep = new IPEndPoint (IPAddress.Parse ("127.0.0.1"), worker.Configuration.OverlayPort);
			udpSock.SendTo(digestData, ep);
			Log.Info ("[" + worker.Configuration.ClientName + "] Digest data sent to 127.0.0.1:" + worker.Configuration.OverlayPort + ".");
		}
	}

	#endregion
}

