using System;
using System.Text.RegularExpressions;

namespace Dsharma.VideoConverter
{
	internal class FFMpegProgress
	{
		private static Regex DurationRegex = new Regex("Duration:\\s(?<duration>[0-9:.]+)([,]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

		private static Regex ProgressRegex = new Regex("time=(?<progress>[0-9:.]+)\\s", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

		internal float? Seek;

		internal float? MaxDuration;

		private Action<ConvertProgressEventArgs> ProgressCallback;

		private ConvertProgressEventArgs lastProgressArgs;

		private bool Enabled = true;

		private int progressEventCount;

		internal FFMpegProgress(Action<ConvertProgressEventArgs> progressCallback, bool enabled)
		{
			ProgressCallback = progressCallback;
			Enabled = enabled;
		}

		internal void Reset()
		{
			progressEventCount = 0;
			lastProgressArgs = null;
		}

		internal void ParseLine(string line)
		{
			if (!Enabled)
			{
				return;
			}
			TimeSpan timeSpan = (lastProgressArgs != null) ? lastProgressArgs.TotalDuration : TimeSpan.Zero;
			Match match = DurationRegex.Match(line);
			if (match.Success)
			{
				TimeSpan result = TimeSpan.Zero;
				if (TimeSpan.TryParse(match.Groups["duration"].Value, out result))
				{
					TimeSpan totalDuration = timeSpan.Add(result);
					lastProgressArgs = new ConvertProgressEventArgs(TimeSpan.Zero, totalDuration);
				}
			}
			Match match2 = ProgressRegex.Match(line);
			if (!match2.Success)
			{
				return;
			}
			TimeSpan result2 = TimeSpan.Zero;
			if (TimeSpan.TryParse(match2.Groups["progress"].Value, out result2))
			{
				if (progressEventCount == 0)
				{
					timeSpan = CorrectDuration(timeSpan);
				}
				lastProgressArgs = new ConvertProgressEventArgs(result2, (timeSpan != TimeSpan.Zero) ? timeSpan : result2);
				ProgressCallback(lastProgressArgs);
				progressEventCount++;
			}
		}

		private TimeSpan CorrectDuration(TimeSpan totalDuration)
		{
			if (totalDuration != TimeSpan.Zero)
			{
				if (Seek.HasValue)
				{
					TimeSpan timeSpan = TimeSpan.FromSeconds(Seek.Value);
					totalDuration = ((totalDuration > timeSpan) ? totalDuration.Subtract(timeSpan) : TimeSpan.Zero);
				}
				if (MaxDuration.HasValue)
				{
					TimeSpan timeSpan2 = TimeSpan.FromSeconds(MaxDuration.Value);
					if (totalDuration > timeSpan2)
					{
						totalDuration = timeSpan2;
					}
				}
			}
			return totalDuration;
		}

		internal void Complete()
		{
			if (Enabled && lastProgressArgs != null && lastProgressArgs.Processed < lastProgressArgs.TotalDuration)
			{
				ProgressCallback(new ConvertProgressEventArgs(lastProgressArgs.TotalDuration, lastProgressArgs.TotalDuration));
			}
		}
	}
}
