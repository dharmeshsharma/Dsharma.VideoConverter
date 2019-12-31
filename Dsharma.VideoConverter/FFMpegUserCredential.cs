namespace Dsharma.VideoConverter
{
	public sealed class FFMpegUserCredential
	{
		public string UserName
		{
			get;
			private set;
		}

		public string Password
		{
			get;
			private set;
		}

		public string Domain
		{
			get;
			private set;
		}

		public FFMpegUserCredential(string userName, string password)
		{
			UserName = userName;
			Password = password;
		}

		public FFMpegUserCredential(string userName, string password, string domain)
			: this(userName, password)
		{
			Domain = domain;
		}
	}
}
