using System.IO;

namespace Dsharma.VideoConverter
{
	internal class Media
	{
		public string Filename
		{
			get;
			set;
		}

		public string Format
		{
			get;
			set;
		}

		public Stream DataStream
		{
			get;
			set;
		}
	}
}
