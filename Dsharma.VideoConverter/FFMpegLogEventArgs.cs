using System;

namespace Dsharma.VideoConverter
{
	public class FFMpegLogEventArgs : EventArgs
	{
		public string Data
		{
			get;
			private set;
		}

		public FFMpegLogEventArgs(string logData)
		{
			Data = logData;
		}
	}
}
