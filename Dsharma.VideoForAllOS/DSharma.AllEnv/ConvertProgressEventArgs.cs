using System;

namespace DSharmaLT.VideoConverter
{
	public class ConvertProgressEventArgs : EventArgs
	{
		public TimeSpan TotalDuration
		{
			get;
			private set;
		}

		public TimeSpan Processed
		{
			get;
			private set;
		}

		public ConvertProgressEventArgs(TimeSpan processed, TimeSpan totalDuration)
		{
			TotalDuration = totalDuration;
			Processed = processed;
		}
	}
}
