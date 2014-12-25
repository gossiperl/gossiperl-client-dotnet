using System;

namespace Gossiperl.Client.Exceptions
{
	public class GossiperlClientException : Exception
	{
		public GossiperlClientException ()
		{
		}

		public GossiperlClientException (string Message)
			: base(Message)
		{
		}

		public GossiperlClientException (string Message, Exception Ex)
			: base(Message, Ex)
		{
		}
	}

	public class GossiperlUnsupportedSerializableTypeException : Exception
	{
		public GossiperlUnsupportedSerializableTypeException (string Message)
			: base(Message)
		{
		}
	}
}

