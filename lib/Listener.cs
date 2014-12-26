using System;

namespace Gossiperl.Client
{
	interface GossiperlClientListener
	{
		void Connected(OverlayWorker worker);
		void Disconnected(OverlayWorker worker);
		void Event( OverlayWorker worker, String eventType, Object member, long heartbeat );
		void subscribeAck( OverlayWorker worker, List<String> events );
		void unsubscribeAck( OverlayWorker worker, List<String> events );
		void forwardAck( OverlayWorker worker, String reply_id );
		void forwarded( OverlayWorker worker, String digestType, byte[] binaryEnvelope, String envelopeId );
		void failed( OverlayWorker worker, GossiperlClientException error );
	}
}

