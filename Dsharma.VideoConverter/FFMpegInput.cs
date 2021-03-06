namespace Dsharma.VideoConverter
{
	public class FFMpegInput
	{
		public string Input
		{
			get;
			set;
		}

		public string Format
		{
			get;
			set;
		}

		public string CustomInputArgs
		{
			get;
			set;
		}

		public FFMpegInput(string input)
			: this(input, null)
		{
		}

		public FFMpegInput(string input, string format)
		{
			Input = input;
			Format = format;
		}
	}
}
