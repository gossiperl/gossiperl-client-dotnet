using NUnit.Framework;
using System;
using Gossiperl.Client.Serialization;
using Gossiperl.Client.Thrift;
using Thrift.Protocol;

namespace Gossiperl.Client.Tests
{

	[TestFixture]
	public class ThriftTest
	{

		protected Serializer serializer;
		protected Digest testDigest;
		protected string encKey;

		[SetUp]
		public void Setup()
		{
			this.serializer = new Serializer ();
			this.testDigest = new Digest ();
			this.testDigest.Id = System.Guid.NewGuid ().ToString ();
			this.testDigest.Heartbeat = Gossiperl.Client.Util.GetTimestamp ();
			this.testDigest.Name = "test-serialize-client";
			this.testDigest.Port = 54321;
			this.testDigest.Secret = "test-serialize-secret";
			this.encKey = "v3JElaRswYgxOt4b";
		}

		[Test]
		public void TestSerializeDeserialize()
		{
			byte[] envelope = serializer.Serialize (testDigest);
			DeserializeResult result = serializer.Deserialize (envelope);
			Assert.IsTrue (result is DeserializeResultOK);
			DeserializeResultOK resultOk = (DeserializeResultOK)result;
			TBase deserializedResult = resultOk.Digest;
			Assert.IsTrue (deserializedResult is Digest);
			Digest deserializedDigest = (Digest)deserializedResult;
			Assert.AreEqual (deserializedDigest.Name, this.testDigest.Name);
		}
	}
}

