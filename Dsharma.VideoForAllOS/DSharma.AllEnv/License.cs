using System;
using System.Security.Cryptography;
using System.Text;

namespace DSharmaLT.VideoConverter
{
	public static class License
	{
		public sealed class LicenseInfo
		{
			public string Owner
			{
				get;
				private set;
			}

			public string Key
			{
				get;
				private set;
			}

			internal LicenseInfo(string owner, string key)
			{
				Owner = owner;
				Key = key;
			}
		}

		internal sealed class LicenseInternal
		{
			internal struct LicenseKeyInfo
			{
				internal string Owner;

				internal string Key;

				internal int CheckResult;
			}

			private LicenseKeyInfo I;

			private int cnt;

			private readonly string InvalidKeyStr = "Invalid license key";

			private const int magic_pub_idx = 20;

			private const int magic_size = 4;

			internal LicenseInternal()
			{
				I = default(LicenseKeyInfo);
				I.CheckResult = -1;
			}

			internal int S()
			{
				return ((I.Owner != null) ? I.Owner.Length : 0) + ((I.Key != null) ? I.Key.Length : 0);
			}

			internal LicenseKeyInfo @int()
			{
				return I;
			}

			internal bool IsLicensed()
			{
				if (I.CheckResult < 0)
				{
					return false;
				}
				if (!S().Equals(I.CheckResult))
				{
					throw new Exception(InvalidKeyStr);
				}
				cnt++;
				if (cnt % 10000 == 0)
				{
					VerifyKey();
				}
				return true;
			}

			internal void Check()
			{
				if (IsLicensed() && S() == I.CheckResult)
				{
					return;
				}
				throw new Exception("Commercial license key is required (https://www.nrecosite.com/video_converter_net.aspx)");
			}

			private void VerifyKey()
			{
				if (I.Key == null || I.Key.Length == 0)
				{
					throw new Exception(InvalidKeyStr);
				}
				if (I.Owner == null || I.Owner.Length == 0)
				{
					throw new Exception(InvalidKeyStr);
				}
				I.CheckResult = I.Owner.Length;
				byte[] licenseKeyBytes = GetLicenseKeyBytes(I.Key);
				byte[] publicKey = typeof(LicenseInternal).Assembly.GetName().GetPublicKey();
				if (publicKey == null)
				{
					throw new Exception("NReco.VideoConverter.LT is not strongly signed");
				}
				byte[] bytes = Encoding.UTF8.GetBytes(I.Owner);
				using (RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider())
				{
					RSAParameters publicKeyRSAParameters = GetPublicKeyRSAParameters(publicKey);
					rSACryptoServiceProvider.PersistKeyInCsp = false;
					rSACryptoServiceProvider.ImportParameters(publicKeyRSAParameters);
					SHA1CryptoServiceProvider halg = new SHA1CryptoServiceProvider();
					try
					{
						if (!rSACryptoServiceProvider.VerifyData(bytes, halg, licenseKeyBytes))
						{
							throw new Exception();
						}
						I.CheckResult += I.Key.Length;
					}
					catch (Exception)
					{
						throw new Exception(InvalidKeyStr);
					}
				}
			}

			internal void SetLicenseKey(string owner, string key)
			{
				I.Key = key;
				I.Owner = owner;
				VerifyKey();
			}

			private byte[] GetLicenseKeyBytes(string key)
			{
				try
				{
					return Convert.FromBase64String(key);
				}
				catch
				{
					throw new Exception("Invalid license key");
				}
			}

			private static RSAParameters GetPublicKeyRSAParameters(byte[] keyBytes)
			{
				RSAParameters result = default(RSAParameters);
				if (keyBytes == null || keyBytes.Length < 1)
				{
					throw new ArgumentNullException("keyBytes");
				}
				int num = 20 + 4;
				int num2 = 4;
				int num3 = num + num2;
				int num4 = 4;
				int num5 = num3 + num4;
				int num6 = 128;
				int num7 = num5 + num6;
				int num8 = 64;
				int num9 = num7 + num8;
				int num10 = 64;
				int num11 = num9 + num10;
				int num12 = 64;
				int num13 = num11 + num12;
				int num14 = 64;
				int num15 = num13 + num14;
				int num16 = 64;
				int startAt = num15 + num16;
				int size = 128;
				result.Exponent = BlockCopy(keyBytes, num3, num4);
				Array.Reverse((Array)result.Exponent);
				result.Modulus = BlockCopy(keyBytes, num5, num6);
				Array.Reverse((Array)result.Modulus);
				if (true)
				{
					return result;
				}
				result.P = BlockCopy(keyBytes, num7, num8);
				Array.Reverse((Array)result.P);
				result.Q = BlockCopy(keyBytes, num9, num10);
				Array.Reverse((Array)result.Q);
				result.DP = BlockCopy(keyBytes, num11, num12);
				Array.Reverse((Array)result.DP);
				result.DQ = BlockCopy(keyBytes, num13, num14);
				Array.Reverse((Array)result.DQ);
				result.InverseQ = BlockCopy(keyBytes, num15, num16);
				Array.Reverse((Array)result.InverseQ);
				result.D = BlockCopy(keyBytes, startAt, size);
				Array.Reverse((Array)result.D);
				return result;
			}

			private static byte[] BlockCopy(byte[] source, int startAt, int size)
			{
				if (source == null || source.Length < startAt + size)
				{
					return null;
				}
				byte[] array = new byte[size];
				Buffer.BlockCopy(source, startAt, array, 0, size);
				return array;
			}
		}

		internal static readonly LicenseInternal L = new LicenseInternal();

		public static void SetLicenseKey(string owner, string key)
		{
			L.SetLicenseKey(owner, key);
		}

		public static LicenseInfo GetLicense()
		{
			if (!L.IsLicensed())
			{
				return null;
			}
			return new LicenseInfo(L.@int().Owner, L.@int().Key);
		}
	}
}
