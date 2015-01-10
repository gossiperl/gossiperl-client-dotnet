using NUnit.Framework;
using System;
using Gossiperl.Client.Serialization;

namespace Gossiperl.Client.Tests
{
	[TestFixture]
	public class SerializationTest
	{
		private Serializer serializer;
		private string digestType;
		private System.Collections.Generic.List<CustomDigestField> digestInfo;

		[SetUp] public void SetUp()
		{
			serializer = new Serializer ();
			digestType = "someDigestType";
			digestInfo = new System.Collections.Generic.List<CustomDigestField> ();
			digestInfo.Add (new CustomDigestField ("some_property", "this is some string to test", "string", 0));
			digestInfo.Add (new CustomDigestField ("some_other_property", 1234, "i32", 1));
		}

		[Test] public void TestSerializeDeserialize ()
		{
			byte[] envelope = serializer.SerializeArbitrary( digestType, digestInfo );
			DeserializeResult result = serializer.DeserializeArbitrary( digestType, envelope, digestInfo );
			Assert.IsTrue (result is DeserializeResultCustomOK);
			DeserializeResultCustomOK finalResult = (DeserializeResultCustomOK)result;
			Assert.AreEqual ( digestType, finalResult.DigestType );
			Assert.IsTrue ( finalResult.ResultData.ContainsKey("some_property") );
			Assert.IsTrue ( finalResult.ResultData.ContainsKey("some_other_property") );
			Assert.AreEqual( finalResult.ResultData["some_property"], "this is some string to test" );
			Assert.AreEqual( finalResult.ResultData["some_other_property"], 1234 );
		}
	}
}

