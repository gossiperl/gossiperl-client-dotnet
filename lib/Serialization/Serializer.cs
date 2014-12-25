using System;
using Thrift.Protocol;
using Thrift.Transport;
using Gossiperl.Client.Thrift;

namespace Gossiperl.Client.Serialization
{
	public class Serializer
	{

		public Serializer() {
			this.Types.Add (Serializer.DIGEST_ERROR, "Gossiperl.Client.Thrift.DigestError");
			this.Types.Add (Serializer.DIGEST_FORWARDED_ACK, "Gossiperl.Client.Thrift.DigestForwardedAck");
			this.Types.Add (Serializer.DIGEST_ENVELOPE, "Gossiperl.Client.Thrift.DigestEnvelope");
			this.Types.Add (Serializer.DIGEST, "Gossiperl.Client.Thrift.Digest");
			this.Types.Add (Serializer.DIGEST_ACK, "Gossiperl.Client.Thrift.DigestAck");
			this.Types.Add (Serializer.DIGEST_SUBSCRIPTIONS, "Gossiperl.Client.Thrift.DigestSubscriptions");
			this.Types.Add (Serializer.DIGEST_EXIT, "Gossiperl.Client.Thrift.DigestExit");
			this.Types.Add (Serializer.DIGEST_SUBSCRIBE, "Gossiperl.Client.Thrift.DigestSubscribe");
			this.Types.Add (Serializer.DIGEST_SUBSCRIBE_ACK, "Gossiperl.Client.Thrift.DigestSubscribeAck");
			this.Types.Add (Serializer.DIGEST_UNSUBSCRIBE, "Gossiperl.Client.Thrift.DigestUnsubscribe");
			this.Types.Add (Serializer.DIGEST_UNSUBSCRIBE_ACK, "Gossiperl.Client.Thrift.DigestUnsubscribeAck");
			this.Types.Add (Serializer.DIGEST_EVENT, "Gossiperl.Client.Thrift.DigestEvent");
		}

		public byte[] Serialize( TBase Digest ) {
			string digestType = this.GetDigestName (Digest); 
			if (digestType.Equals (Serializer.DIGEST_ENVELOPE)) {
				return this.DigestToBinary( Digest );
			}
			DigestEnvelope envelope = new DigestEnvelope();
			envelope.Id = System.Guid.NewGuid ().ToString();
			envelope.Payload_type = digestType;
			envelope.Bin_payload = System.Convert.ToBase64String (this.DigestToBinary ( Digest ));
			return this.DigestToBinary( envelope );
		}

		public TBase Deserialize(byte[] binDigest) {
			DigestEnvelope envelope = (DigestEnvelope)this.DigestFromBinary (DIGEST_ENVELOPE, binDigest);
			if (this.Types.ContainsKey (envelope.Payload_type)) {
				TBase digest = this.DigestFromBinary (envelope.Payload_type, System.Convert.FromBase64String (envelope.Bin_payload));
				return digest;
			}
			return null;
		}

		private byte[] DigestToBinary( TBase Digest ) {
			System.IO.MemoryStream Stream = new System.IO.MemoryStream ();
			TStreamTransport transport = new TStreamTransport (null, Stream);
			TBinaryProtocol protocol = new TBinaryProtocol (transport);
			Digest.Write (protocol);
			return Stream.ToArray ();
		}

		private TBase DigestFromBinary( string digestType, byte[] binDigest ) {
			if (this.Types.ContainsKey (digestType)) {
				System.IO.MemoryStream Stream = new System.IO.MemoryStream (binDigest);
				TStreamTransport transport = new TStreamTransport (Stream, null);
				TBinaryProtocol protocol = new TBinaryProtocol (transport);
				TBase digest = (TBase)Activator.CreateInstance (Type.GetType( this.Types [digestType] ));
				digest.Read (protocol);
				return digest;
			} else {
				throw new NotImplementedException ("Needs to throw correct exception type.");
			}
		}

		private string GetDigestName( TBase Digest ) {
			string ClsName = Digest.GetType ().Name;
			return ClsName.Substring (0, 1).ToLower () + ClsName.Substring (1, ClsName.Length-1);
		}

		#region Static part

		public static string DIGEST_ERROR { get { return "digestError"; } }
		public static string DIGEST_FORWARDED_ACK { get { return "digestForwardedAck"; } }
		public static string DIGEST_ENVELOPE { get { return "digestEnvelope"; } }
		public static string DIGEST { get { return "digest"; } }
		public static string DIGEST_ACK { get { return "digestAck"; } }
		public static string DIGEST_SUBSCRIPTIONS { get { return "digestSubscriptions"; } }
		public static string DIGEST_EXIT { get { return "digestExit"; } }
		public static string DIGEST_SUBSCRIBE { get { return "digestSubscribe"; } }
		public static string DIGEST_SUBSCRIBE_ACK { get { return "digestSubscribeAck"; } }
		public static string DIGEST_UNSUBSCRIBE { get { return "digestUnsubscribe"; } }
		public static string DIGEST_UNSUBSCRIBE_ACK { get { return "digestUnsubscribeAck"; } }
		public static string DIGEST_EVENT { get { return "digestEvent"; } }

		protected System.Collections.Generic.Dictionary<string, string> Types = new System.Collections.Generic.Dictionary<string, string> ();

		#endregion

	}
}

