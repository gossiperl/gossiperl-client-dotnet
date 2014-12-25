using System;
using Gossiperl.Client.Encryption;
using Thrift.Protocol;

namespace gossiperlclientdotnetexec
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			string data = "This is my data to encrypt. Heh.";
			Aes256 aes = new Aes256 ("v3JElaRswYgxOt4b");
			byte[] encrypted = aes.Encrypt (System.Text.Encoding.UTF8.GetBytes(data));
			byte[] decrypted = aes.Decrypt (encrypted);
			Console.WriteLine ( System.Text.Encoding.UTF8.GetString( decrypted ) );
		}
	}
}
