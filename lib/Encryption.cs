using System;
using System.Security.Cryptography;

namespace Gossiperl.Client.Encryption
{
	public class Aes256
	{
		private byte[] key;

		public Aes256 (string key)
		{
			System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create ();
			this.key = sha256.ComputeHash (System.Text.Encoding.UTF8.GetBytes(key));
		}

		public byte[] Encrypt(byte[] data)
		{
			byte[] iv = GenerateIV ();
			RijndaelManaged aes = new RijndaelManaged ();
			aes.KeySize = 256;
			aes.IV = iv;
			aes.Key = this.key;
			aes.Padding = PaddingMode.PKCS7;
			ICryptoTransform encryptor = aes.CreateEncryptor ();
			byte[] encrypted = encryptor.TransformFinalBlock (data, 0, data.Length);
			byte[] complete = new byte[iv.Length + encrypted.Length];
			System.Buffer.BlockCopy (iv, 0, complete, 0, iv.Length);
			System.Buffer.BlockCopy (encrypted, 0, complete, iv.Length, encrypted.Length);
			return complete;
		}

		public byte[] Decrypt(byte[] data)
		{
			byte[] iv = new byte[16];
			byte[] encrypted = new byte[data.Length - iv.Length];
			System.Buffer.BlockCopy (data, 0, iv, 0, iv.Length);
			System.Buffer.BlockCopy (data, iv.Length, encrypted, 0, encrypted.Length);
			RijndaelManaged aes = new RijndaelManaged ();
			aes.KeySize = 256;
			aes.IV = iv;
			aes.Key = this.key;
			aes.Padding = PaddingMode.None;
			ICryptoTransform encryptor = aes.CreateDecryptor ();
			return encryptor.TransformFinalBlock (encrypted, 0, encrypted.Length);
		}

		private byte[] GenerateIV()
		{
			System.Security.Cryptography.RNGCryptoServiceProvider rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
			byte[] buff = new byte[16];
			rng.GetBytes(buff);
			return buff;
		}
	}
}

