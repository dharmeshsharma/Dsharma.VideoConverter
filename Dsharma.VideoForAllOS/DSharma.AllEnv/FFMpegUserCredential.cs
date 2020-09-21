using System.Security;

namespace DSharmaLT.VideoConverter
{
	public sealed class FFMpegUserCredential
	{
		public string UserName
		{
			get;
			private set;
		}

		public SecureString Password
		{
			get;
			private set;
		}

		public string Domain
		{
			get;
			private set;
		}

		public FFMpegUserCredential(string userName, SecureString password)
		{
			UserName = userName;
			Password = password;
		}

		public FFMpegUserCredential(string userName, SecureString password, string domain)
			: this(userName, password)
		{
			Domain = domain;
		}
	}
}
