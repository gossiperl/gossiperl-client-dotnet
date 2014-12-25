using System;
using Gossiperl.Client.Serialization;
using Gossiperl.Client.Thrift;
using Thrift.Protocol;

namespace gossiperlclientdotnetexec
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Serializer s = new Serializer ();
			DigestAck ack = new DigestAck ();
			ack.Heartbeat = 12345;
			ack.Membership = new System.Collections.Generic.List<DigestMember> ();
			ack.Name = "meh";
			ack.Reply_id = "1234567890";
			byte[] serialized = s.Serialize (ack);

			TBase Deserialized = s.Deserialize (serialized);

			Console.WriteLine (((DigestAck)Deserialized).Name);
		}
	}
}
