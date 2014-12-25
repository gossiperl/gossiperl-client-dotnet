using System;
using Thrift.Protocol;
using Thrift.Transport;
using Gossiperl.Client.Thrift;

namespace Gossiperl.Client.Serialization
{
	public class Serializer
	{

		public Serializer()
		{
			EnsureSerializableTypes ();
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

		public byte[] SerializeArbitrary(string digestType, System.Collections.Generic.List<CustomDigestField> digestData)
		{
			System.IO.MemoryStream stream = new System.IO.MemoryStream ();
			TStreamTransport transport = new TStreamTransport (null, stream);
			TBinaryProtocol protocol = new TBinaryProtocol (transport);
			protocol.WriteStructBegin (new TStruct (digestType));
			foreach (CustomDigestField field in digestData)
			{
				TType fieldType = SerializableTypes [field.Type];
				protocol.WriteFieldBegin (new TField (field.FieldName, fieldType, field.FieldOrder));
				if (fieldType == TType.Bool) {
					try {
						protocol.WriteBool ((bool)field.Value);
					} catch (TProtocolException ex) {
						throw new Gossiperl.Client.Exceptions.GossiperlClientException ("Failed to write value " + field.Value.ToString() + " as BOOL.", ex);
					}
				} else if (fieldType == TType.Byte) {
					try {
						protocol.WriteByte((sbyte)field.Value);
					} catch (TProtocolException ex) {
						throw new Gossiperl.Client.Exceptions.GossiperlClientException ("Failed to write value " + field.Value.ToString() + " as BYTE.", ex);
					}
				} else if (fieldType == TType.Double) {
					try {
						protocol.WriteDouble ((double)field.Value);
					} catch (TProtocolException ex) {
						throw new Gossiperl.Client.Exceptions.GossiperlClientException ("Failed to write value " + field.Value.ToString() + " as DOUBLE.", ex);
					}
				} else if (fieldType == TType.I16) {
					try {
						protocol.WriteI16((short)field.Value);
					} catch (TProtocolException ex) {
						throw new Gossiperl.Client.Exceptions.GossiperlClientException ("Failed to write value " + field.Value.ToString() + " as I16.", ex);
					}
				} else if (fieldType == TType.I32) {
					try {
						protocol.WriteI32((int)field.Value);
					} catch (TProtocolException ex) {
						throw new Gossiperl.Client.Exceptions.GossiperlClientException ("Failed to write value " + field.Value.ToString() + " as I32.", ex);
					}
				} else if (fieldType == TType.I64) {
					try {
						protocol.WriteI64((long)field.Value);
					} catch (TProtocolException ex) {
						throw new Gossiperl.Client.Exceptions.GossiperlClientException ("Failed to write value " + field.Value.ToString() + " as I64.", ex);
					}
				} else if (fieldType == TType.String) {
					try {
						protocol.WriteString (field.Value.ToString());
					} catch (TProtocolException ex) {
						throw new Gossiperl.Client.Exceptions.GossiperlClientException ("Failed to write value " + field.Value.ToString() + " as STRING.", ex);
					}
				}
				protocol.WriteFieldEnd ();
			}
			protocol.WriteFieldStop ();
			protocol.WriteStructEnd ();
			DigestEnvelope envelope = new DigestEnvelope ();
			envelope.Payload_type = digestType;
			envelope.Bin_payload = System.Convert.ToBase64String (stream.ToArray ());
			envelope.Id = System.Guid.NewGuid ().ToString ();
			return this.DigestToBinary (envelope);
		}

		public byte[] Serialize( TBase digest ) {
			string digestType = this.GetDigestName (digest); 
			if (digestType.Equals (Serializer.DIGEST_ENVELOPE)) {
				return this.DigestToBinary( digest );
			}
			DigestEnvelope envelope = new DigestEnvelope();
			envelope.Id = System.Guid.NewGuid ().ToString();
			envelope.Payload_type = digestType;
			envelope.Bin_payload = System.Convert.ToBase64String (this.DigestToBinary ( digest ));
			return this.DigestToBinary( envelope );
		}

		public DeserializeResult DeserializeArbitrary(string digestType, byte[] binDigest, System.Collections.Generic.List<CustomDigestField> digestInfo)
		{
			DigestEnvelope envelope = (DigestEnvelope)this.DigestFromBinary (DIGEST_ENVELOPE, binDigest);
			byte[] digest = System.Convert.FromBase64String (envelope.Bin_payload);

			System.IO.MemoryStream stream = new System.IO.MemoryStream (digest);
			TStreamTransport transport = new TStreamTransport (stream, null);
			TBinaryProtocol protocol = new TBinaryProtocol (transport);

			System.Collections.Generic.Dictionary<string, object> result = new System.Collections.Generic.Dictionary<string, object> ();

			protocol.ReadStructBegin ();

			foreach (CustomDigestField field in digestInfo) {
				TField thriftFieldInfo = protocol.ReadFieldBegin ();
				CustomDigestField digestField = this.GetFidData ( thriftFieldInfo.ID, digestInfo );
				if (digestField != null) {
					if (Serializer.IsSerializableType (digestField.Type)) {
						if (Serializer.SerializableTypes [digestField.Type] == thriftFieldInfo.Type) {
							if (thriftFieldInfo.Type == TType.String) {
								result.Add (digestField.FieldName, protocol.ReadString ());
							} else if (thriftFieldInfo.Type == TType.Bool) {
								result.Add (digestField.FieldName, protocol.ReadBool ());
							} else if (thriftFieldInfo.Type == TType.Byte) {
								result.Add (digestField.FieldName, protocol.ReadByte ());
							} else if (thriftFieldInfo.Type == TType.Double) {
								result.Add (digestField.FieldName, protocol.ReadDouble ());
							} else if (thriftFieldInfo.Type == TType.I16) {
								result.Add (digestField.FieldName, protocol.ReadI16 ());
							} else if (thriftFieldInfo.Type == TType.I32) {
								result.Add (digestField.FieldName, protocol.ReadI32 ());
							} else if (thriftFieldInfo.Type == TType.I64) {
								result.Add (digestField.FieldName, protocol.ReadI64 ());
							} else {
								TProtocolUtil.Skip (protocol, thriftFieldInfo.Type);
							}
						} else {
							TProtocolUtil.Skip (protocol, thriftFieldInfo.Type);
						}
					} else {
						TProtocolUtil.Skip (protocol, thriftFieldInfo.Type);
					}
				} else {
					TProtocolUtil.Skip (protocol, thriftFieldInfo.Type);
				}
				protocol.ReadFieldEnd ();
			}

			protocol.ReadStructEnd ();
			return new DeserializeResultCustomOK (digestType, result);
		}

		public DeserializeResult Deserialize(byte[] binDigest)
		{
			DigestEnvelope envelope = (DigestEnvelope)this.DigestFromBinary (DIGEST_ENVELOPE, binDigest);
			if (this.Types.ContainsKey (envelope.Payload_type)) {
				try {
					TBase digest = this.DigestFromBinary (envelope.Payload_type, System.Convert.FromBase64String (envelope.Bin_payload));
					return new DeserializeResultOK (envelope.Payload_type, digest);
				} catch (Gossiperl.Client.Exceptions.GossiperlClientException ex) {
					return new DeserializeResultError (ex);
				}
			} else {
				return new DeserializeResultForward (envelope.Payload_type, binDigest, envelope.Id);
			}
		}

		private byte[] DigestToBinary( TBase digest )
		{
			try
			{
				System.IO.MemoryStream stream = new System.IO.MemoryStream ();
				TStreamTransport transport = new TStreamTransport (null, stream);
				TBinaryProtocol protocol = new TBinaryProtocol (transport);
				digest.Write (protocol);
				return stream.ToArray ();
			}
			catch (TProtocolException Ex)
			{
				throw new Gossiperl.Client.Exceptions.GossiperlClientException ("Could not write Thrift digest.", Ex);
			}
		}

		private TBase DigestFromBinary( string digestType, byte[] binDigest ) {
			if (this.Types.ContainsKey (digestType))
			{
				try
				{
					System.IO.MemoryStream stream = new System.IO.MemoryStream (binDigest);
					TStreamTransport transport = new TStreamTransport (stream, null);
					TBinaryProtocol protocol = new TBinaryProtocol (transport);
					TBase digest = (TBase)Activator.CreateInstance (Type.GetType( this.Types [digestType] ));
					digest.Read (protocol);
					return digest;
				}
				catch (TProtocolException Ex)
				{
					throw new Gossiperl.Client.Exceptions.GossiperlClientException ("Could not read Thrift digest.", Ex);
				}
			}
			else
			{
				throw new Gossiperl.Client.Exceptions.GossiperlClientException ("Digest type " + digestType + " unknown.");
			}
		}

		private string GetDigestName( TBase digest )
		{
			string ClsName = digest.GetType ().Name;
			return ClsName.Substring (0, 1).ToLower () + ClsName.Substring (1, ClsName.Length-1);
		}

		private CustomDigestField GetFidData(short id, System.Collections.Generic.List<CustomDigestField> digestInfo)
		{
			foreach (CustomDigestField field in digestInfo) {
				if (field.FieldOrder == id) {
					return field;
				}
			}
			return null;
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

		private System.Collections.Generic.Dictionary<string, string> Types = new System.Collections.Generic.Dictionary<string, string> ();
		private static System.Collections.Generic.Dictionary<string, TType> SerializableTypes = new System.Collections.Generic.Dictionary<string, TType> ();

		public static bool IsSerializableType(string type)
		{
			EnsureSerializableTypes ();
			return SerializableTypes.ContainsKey (type);
		}

		private static void EnsureSerializableTypes()
		{
			if (SerializableTypes.Count == 0) {
				SerializableTypes.Add ("string", TType.String);
				SerializableTypes.Add ("bool", TType.Bool);
				SerializableTypes.Add ("byte", TType.Byte);
				SerializableTypes.Add ("double", TType.Double);
				SerializableTypes.Add ("i16", TType.I16);
				SerializableTypes.Add ("i32", TType.I32);
				SerializableTypes.Add ("i64", TType.I64);
			}
		}

		#endregion

	}

	#region Deserialization results

	public class DeserializeResult
	{
	}

	public class DeserializeResultOK : DeserializeResult
	{
		private string digestType;
		private TBase digest;

		public DeserializeResultOK(string digestType, TBase digest)
		{
			this.digest = digest;
			this.digestType = digestType;
		}

		public string DigestType {
			get {
				return this.digestType;
			}
		}

		public TBase Digest {
			get {
				return this.digest;
			}
		}
	}

	public class DeserializeResultError : DeserializeResult
	{
		private Gossiperl.Client.Exceptions.GossiperlClientException cause;

		public DeserializeResultError(Gossiperl.Client.Exceptions.GossiperlClientException cause)
		{
			this.cause = cause;
		}

		public Gossiperl.Client.Exceptions.GossiperlClientException Cause {
			get {
				return this.cause;
			}
		}
	}

	public class DeserializeResultForward : DeserializeResult
	{
		private string digestType;
		private byte[] binaryEnvelope;
		private string envelopeId;

		public DeserializeResultForward(string digestType, byte[] binaryEnvelope, string envelopeId)
		{
			this.digestType = digestType;
			this.binaryEnvelope = binaryEnvelope;
			this.envelopeId = envelopeId;
		}

		public string DigestType {
			get {
				return this.digestType;
			}
		}

		public byte[] BinaryEnvelope {
			get {
				return this.binaryEnvelope;
			}
		}

		public string EnvelopeId {
			get {
				return this.envelopeId;
			}
		}
	}

	public class DeserializeResultCustomOK : DeserializeResult
	{
		private string digestType;
		private System.Collections.Generic.Dictionary<string, object> resultData;

		public DeserializeResultCustomOK(string digestType, System.Collections.Generic.Dictionary<string, object> resultData)
		{
			this.digestType = digestType;
			this.resultData = resultData;
		}

		public string DigestType {
			get {
				return this.digestType;
			}
		}

		public System.Collections.Generic.Dictionary<string, object> ResultData {
			get {
				return this.resultData;
			}
		}
	}

	#endregion

	#region Custom serialization
	public class CustomDigestField
	{
		private string fieldName;
		private object value;
		private string type;
		private short fieldOrder;

		public CustomDigestField(string fieldName, object value, string type, short fieldOrder)
		{
			if (!Serializer.IsSerializableType (type)) {
				throw new Gossiperl.Client.Exceptions.GossiperlUnsupportedSerializableTypeException (type);
			}
			this.fieldName = fieldName;
			this.value = value;
			this.type = type;
			this.fieldOrder = fieldOrder;
		}

		public string FieldName {
			get {
				return this.fieldName;
			}
		}

		public object Value {
			get {
				return this.value;
			}
		}

		public string Type {
			get {
				return this.type;
			}
		}

		public short FieldOrder {
			get {
				return this.fieldOrder;
			}
		}

	}
	#endregion
}

