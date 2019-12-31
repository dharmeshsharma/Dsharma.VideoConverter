using System;

namespace Dsharma.VideoConverter
{
	public class FFMpegException : Exception
	{
		public int ErrorCode
		{
			get;
			private set;
		}

		public FFMpegException(int errCode, string message)
			: base($"{message} (exit code: {errCode})")
		{
			ErrorCode = errCode;
		}
	}
}
