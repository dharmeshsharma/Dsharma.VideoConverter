namespace Dsharma.VideoConverter
{
	public class OutputSettings
	{
		public int? AudioSampleRate;

		public string AudioCodec;

		public int? VideoFrameRate;

		public int? VideoFrameCount;

		public string VideoFrameSize;

		public string VideoCodec;

		public float? MaxDuration;

		public string CustomOutputArgs;

		public void SetVideoFrameSize(int width, int height)
		{
			VideoFrameSize = $"{width}x{height}";
		}

		internal void CopyTo(OutputSettings outputSettings)
		{
			outputSettings.AudioSampleRate = AudioSampleRate;
			outputSettings.AudioCodec = AudioCodec;
			outputSettings.VideoFrameRate = VideoFrameRate;
			outputSettings.VideoFrameCount = VideoFrameCount;
			outputSettings.VideoFrameSize = VideoFrameSize;
			outputSettings.VideoCodec = VideoCodec;
			outputSettings.MaxDuration = MaxDuration;
			outputSettings.CustomOutputArgs = CustomOutputArgs;
		}
	}
}
