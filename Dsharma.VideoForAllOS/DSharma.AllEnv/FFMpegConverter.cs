using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace DSharmaLT.VideoConverter
{
	public class FFMpegConverter
	{
		private Process FFMpegProcess;

		private static object globalObj = new object();

		public string FFMpegToolPath
		{
			get;
			set;
		}

		public string FFMpegExeName
		{
			get;
			set;
		}

		public TimeSpan? ExecutionTimeout
		{
			get;
			set;
		}

		public ProcessPriorityClass FFMpegProcessPriority
		{
			get;
			set;
		}

		public FFMpegUserCredential FFMpegProcessUser
		{
			get;
			set;
		}

		public string LogLevel
		{
			get;
			set;
		}

		public event EventHandler<ConvertProgressEventArgs> ConvertProgress;

		public event EventHandler<FFMpegLogEventArgs> LogReceived;

		public FFMpegConverter()
		{
			FFMpegProcessPriority = ProcessPriorityClass.Normal;
			LogLevel = "info";
			FFMpegToolPath = AppDomain.CurrentDomain.BaseDirectory;
			if (string.IsNullOrEmpty(FFMpegToolPath))
			{
				FFMpegToolPath = Path.GetDirectoryName(typeof(FFMpegConverter).Assembly.Location);
			}
			FFMpegExeName = "ffmpeg.exe";
		}

		private void CopyStream(Stream inputStream, Stream outputStream, int bufSize)
		{
			byte[] array = new byte[bufSize];
			int count;
			while ((count = inputStream.Read(array, 0, array.Length)) > 0)
			{
				outputStream.Write(array, 0, count);
			}
		}

		public void ConvertMedia(string inputFile, string outputFile, string outputFormat)
		{
			ConvertMedia(inputFile, null, outputFile, outputFormat, null);
		}

		public void ConvertMedia(string inputFile, string inputFormat, string outputFile, string outputFormat, ConvertSettings settings)
		{
			if (inputFile == null)
			{
				throw new ArgumentNullException("inputFile");
			}
			if (outputFile == null)
			{
				throw new ArgumentNullException("outputFile");
			}
			if (File.Exists(inputFile) && string.IsNullOrEmpty(Path.GetExtension(inputFile)) && inputFormat == null)
			{
				throw new Exception("Input format is required for file without extension");
			}
			if (string.IsNullOrEmpty(Path.GetExtension(outputFile)) && outputFormat == null)
			{
				throw new Exception("Output format is required for file without extension");
			}
			Media input = new Media
			{
				Filename = inputFile,
				Format = inputFormat
			};
			Media output = new Media
			{
				Filename = outputFile,
				Format = outputFormat
			};
			ConvertMedia(input, output, settings ?? new ConvertSettings());
		}

		public void ConvertMedia(string inputFile, Stream outputStream, string outputFormat)
		{
			ConvertMedia(inputFile, null, outputStream, outputFormat, null);
		}

		public void ConvertMedia(string inputFile, string inputFormat, Stream outputStream, string outputFormat, ConvertSettings settings)
		{
			if (inputFile == null)
			{
				throw new ArgumentNullException("inputFile");
			}
			if (File.Exists(inputFile) && string.IsNullOrEmpty(Path.GetExtension(inputFile)) && inputFormat == null)
			{
				throw new Exception("Input format is required for file without extension");
			}
			if (outputFormat == null)
			{
				throw new ArgumentNullException("outputFormat");
			}
			Media input = new Media
			{
				Filename = inputFile,
				Format = inputFormat
			};
			Media output = new Media
			{
				DataStream = outputStream,
				Format = outputFormat
			};
			ConvertMedia(input, output, settings ?? new ConvertSettings());
		}

		public void ConvertMedia(FFMpegInput[] inputs, string output, string outputFormat, OutputSettings settings)
		{
			if (inputs == null || inputs.Length == 0)
			{
				throw new ArgumentException("At least one ffmpeg input should be specified");
			}
			FFMpegInput fFMpegInput = inputs[inputs.Length - 1];
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < inputs.Length - 1; i++)
			{
				FFMpegInput fFMpegInput2 = inputs[i];
				if (fFMpegInput2.Format != null)
				{
					stringBuilder.Append(" -f " + fFMpegInput2.Format);
				}
				if (fFMpegInput2.CustomInputArgs != null)
				{
					stringBuilder.AppendFormat(" {0} ", fFMpegInput2.CustomInputArgs);
				}
				stringBuilder.AppendFormat(" -i {0} ", CommandArgParameter(fFMpegInput2.Input));
			}
			ConvertSettings convertSettings = new ConvertSettings();
			settings.CopyTo(convertSettings);
			convertSettings.CustomInputArgs = stringBuilder.ToString() + fFMpegInput.CustomInputArgs;
			if (fFMpegInput.Format != null)
			{
				convertSettings.CustomInputArgs = convertSettings.CustomInputArgs + " -f " + fFMpegInput.Format;
			}
			ConvertMedia(fFMpegInput.Input, null, output, outputFormat, convertSettings);
		}

		public ConvertLiveMediaTask ConvertLiveMedia(string inputFormat, Stream outputStream, string outputFormat, ConvertSettings settings)
		{
			return ConvertLiveMedia((Stream)null, inputFormat, outputStream, outputFormat, settings);
		}

		public ConvertLiveMediaTask ConvertLiveMedia(string inputSource, string inputFormat, Stream outputStream, string outputFormat, ConvertSettings settings)
		{
			EnsureFFMpegLibs();
			string toolArgs = ComposeFFMpegCommandLineArgs(inputSource, inputFormat, "-", outputFormat, settings);
			return CreateLiveMediaTask(toolArgs, null, outputStream, settings);
		}

		public ConvertLiveMediaTask ConvertLiveMedia(Stream inputStream, string inputFormat, string outputFile, string outputFormat, ConvertSettings settings)
		{
			EnsureFFMpegLibs();
			string toolArgs = ComposeFFMpegCommandLineArgs("-", inputFormat, outputFile, outputFormat, settings);
			return CreateLiveMediaTask(toolArgs, inputStream, null, settings);
		}

		public ConvertLiveMediaTask ConvertLiveMedia(Stream inputStream, string inputFormat, Stream outputStream, string outputFormat, ConvertSettings settings)
		{
			EnsureFFMpegLibs();
			string toolArgs = ComposeFFMpegCommandLineArgs("-", inputFormat, "-", outputFormat, settings);
			return CreateLiveMediaTask(toolArgs, inputStream, outputStream, settings);
		}

		private ConvertLiveMediaTask CreateLiveMediaTask(string toolArgs, Stream inputStream, Stream outputStream, ConvertSettings settings)
		{
			FFMpegProgress fFMpegProgress = new FFMpegProgress(OnConvertProgress, this.ConvertProgress != null);
			if (settings != null)
			{
				fFMpegProgress.Seek = settings.Seek;
				fFMpegProgress.MaxDuration = settings.MaxDuration;
			}
			return new ConvertLiveMediaTask(this, toolArgs, inputStream, outputStream, fFMpegProgress);
		}

		public void GetVideoThumbnail(string inputFile, Stream outputJpegStream)
		{
			GetVideoThumbnail(inputFile, outputJpegStream, null);
		}

		public void GetVideoThumbnail(string inputFile, string outputFile)
		{
			GetVideoThumbnail(inputFile, outputFile, null);
		}

		public void GetVideoThumbnail(string inputFile, Stream outputJpegStream, float? frameTime)
		{
			GetVideoThumbnail(inputFile, outputJpegStream, frameTime, null);
		}

		public void GetVideoThumbnail(string inputFile, Stream outputJpegStream, float? frameTime, ConvertSettings settings)
		{
			Media input = new Media
			{
				Filename = inputFile
			};
			Media output = new Media
			{
				DataStream = outputJpegStream,
				Format = "mjpeg"
			};
			if (settings == null)
			{
				settings = new ConvertSettings();
			}
			settings.VideoFrameCount = 1;
			if (frameTime.HasValue)
			{
				settings.Seek = frameTime;
			}
			ConvertMedia(input, output, settings);
		}

		public void GetVideoThumbnail(string inputFile, string outputFile, float? frameTime)
		{
			GetVideoThumbnail(inputFile, outputFile, frameTime, null);
		}

		public void GetVideoThumbnail(string inputFile, string outputFile, float? frameTime, ConvertSettings settings)
		{
			Media input = new Media
			{
				Filename = inputFile
			};
			Media output = new Media
			{
				Filename = outputFile,
				Format = "mjpeg"
			};
			if (settings == null)
			{
				settings = new ConvertSettings();
			}
			settings.VideoFrameCount = 1;
			if (frameTime.HasValue)
			{
				settings.Seek = frameTime;
			}
			ConvertMedia(input, output, settings);
		}

		private string CommandArgParameter(string arg)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append('"');
			stringBuilder.Append(arg);
			stringBuilder.Append('"');
			return stringBuilder.ToString();
		}

		internal void InitStartInfo(ProcessStartInfo startInfo)
		{
			if (FFMpegProcessUser != null)
			{
				if (FFMpegProcessUser.Domain != null)
				{
					startInfo.Domain = FFMpegProcessUser.Domain;
				}
				if (FFMpegProcessUser.UserName != null)
				{
					startInfo.UserName = FFMpegProcessUser.UserName;
				}
				if (FFMpegProcessUser.Password != null)
				{
					startInfo.Password = FFMpegProcessUser.Password;
				}
			}
		}

		internal string GetFFMpegExePath()
		{
			return Path.Combine(FFMpegToolPath, FFMpegExeName);
		}

		public void ConcatMedia(string[] inputFiles, string outputFile, string outputFormat, ConcatSettings settings)
		{
			EnsureFFMpegLibs();
			string fFMpegExePath = GetFFMpegExePath();
			//License.L.Check();
			if (!File.Exists(fFMpegExePath))
			{
				throw new FileNotFoundException("Cannot find ffmpeg tool: " + fFMpegExePath);
			}
			StringBuilder stringBuilder = new StringBuilder();
			foreach (string text in inputFiles)
			{
				if (!File.Exists(text))
				{
					throw new FileNotFoundException("Cannot find input video file: " + text);
				}
				stringBuilder.AppendFormat(" -i {0} ", CommandArgParameter(text));
			}
			StringBuilder stringBuilder2 = new StringBuilder();
			ComposeFFMpegOutputArgs(stringBuilder2, outputFormat, settings);
			stringBuilder2.Append(" -filter_complex \"");
			stringBuilder2.AppendFormat("concat=n={0}", inputFiles.Length);
			if (settings.ConcatVideoStream)
			{
				stringBuilder2.Append(":v=1");
			}
			if (settings.ConcatAudioStream)
			{
				stringBuilder2.Append(":a=1");
			}
			if (settings.ConcatVideoStream)
			{
				stringBuilder2.Append(" [v]");
			}
			if (settings.ConcatAudioStream)
			{
				stringBuilder2.Append(" [a]");
			}
			stringBuilder2.Append("\" ");
			if (settings.ConcatVideoStream)
			{
				stringBuilder2.Append(" -map \"[v]\" ");
			}
			if (settings.ConcatAudioStream)
			{
				stringBuilder2.Append(" -map \"[a]\" ");
			}
			string arguments = string.Format("-y -loglevel {3} {0} {1} {2}", stringBuilder.ToString(), stringBuilder2, CommandArgParameter(outputFile), LogLevel);
			try
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo(fFMpegExePath, arguments);
				processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				processStartInfo.UseShellExecute = false;
				processStartInfo.CreateNoWindow = true;
				processStartInfo.WorkingDirectory = Path.GetDirectoryName(FFMpegToolPath);
				processStartInfo.RedirectStandardInput = true;
				processStartInfo.RedirectStandardOutput = true;
				processStartInfo.RedirectStandardError = true;
				InitStartInfo(processStartInfo);
				if (FFMpegProcess != null)
				{
					throw new InvalidOperationException("FFMpeg process is already started");
				}
				FFMpegProcess = Process.Start(processStartInfo);
				if (FFMpegProcessPriority != ProcessPriorityClass.Normal)
				{
					FFMpegProcess.PriorityClass = FFMpegProcessPriority;
				}
				string lastErrorLine = string.Empty;
				FFMpegProgress ffmpegProgress = new FFMpegProgress(OnConvertProgress, this.ConvertProgress != null);
				if (settings != null)
				{
					ffmpegProgress.MaxDuration = settings.MaxDuration;
				}
				FFMpegProcess.ErrorDataReceived += delegate(object o, DataReceivedEventArgs args)
				{
					if (args.Data != null)
					{
						lastErrorLine = args.Data;
						ffmpegProgress.ParseLine(args.Data);
						FFMpegLogHandler(args.Data);
					}
				};
				FFMpegProcess.OutputDataReceived += delegate
				{
				};
				FFMpegProcess.BeginOutputReadLine();
				FFMpegProcess.BeginErrorReadLine();
				WaitFFMpegProcessForExit();
				if (FFMpegProcess.ExitCode != 0)
				{
					throw new FFMpegException(FFMpegProcess.ExitCode, lastErrorLine);
				}
				FFMpegProcess.Close();
				FFMpegProcess = null;
				ffmpegProgress.Complete();
			}
			catch (Exception)
			{
				EnsureFFMpegProcessStopped();
				throw;
			}
		}

		protected void WaitFFMpegProcessForExit()
		{
			if (FFMpegProcess == null)
			{
				throw new FFMpegException(-1, "FFMpeg process was aborted");
			}
			if (!FFMpegProcess.HasExited)
			{
				int milliseconds = ExecutionTimeout.HasValue ? ((int)ExecutionTimeout.Value.TotalMilliseconds) : int.MaxValue;
				if (!FFMpegProcess.WaitForExit(milliseconds))
				{
					EnsureFFMpegProcessStopped();
					throw new FFMpegException(-2, $"FFMpeg process exceeded execution timeout ({ExecutionTimeout}) and was aborted");
				}
			}
		}

		protected void EnsureFFMpegProcessStopped()
		{
			if (FFMpegProcess == null)
			{
				return;
			}
			if (!FFMpegProcess.HasExited)
			{
				try
				{
					FFMpegProcess.Kill();
				}
				catch (Exception)
				{
				}
			}
			FFMpegProcess = null;
		}

		protected void ComposeFFMpegOutputArgs(StringBuilder outputArgs, string outputFormat, OutputSettings settings)
		{
			if (settings != null)
			{
				if (settings.MaxDuration.HasValue)
				{
					outputArgs.AppendFormat(CultureInfo.InvariantCulture, " -t {0}", settings.MaxDuration);
				}
				if (outputFormat != null)
				{
					outputArgs.AppendFormat(" -f {0} ", outputFormat);
				}
				if (settings.AudioSampleRate.HasValue)
				{
					outputArgs.AppendFormat(" -ar {0}", settings.AudioSampleRate);
				}
				if (settings.AudioCodec != null)
				{
					outputArgs.AppendFormat(" -acodec {0}", settings.AudioCodec);
				}
				if (settings.VideoFrameCount.HasValue)
				{
					outputArgs.AppendFormat(" -vframes {0}", settings.VideoFrameCount);
				}
				if (settings.VideoFrameRate.HasValue)
				{
					outputArgs.AppendFormat(" -r {0}", settings.VideoFrameRate);
				}
				if (settings.VideoCodec != null)
				{
					outputArgs.AppendFormat(" -vcodec {0}", settings.VideoCodec);
				}
				if (settings.VideoFrameSize != null)
				{
					outputArgs.AppendFormat(" -s {0}", settings.VideoFrameSize);
				}
				if (settings.CustomOutputArgs != null)
				{
					outputArgs.AppendFormat(" {0} ", settings.CustomOutputArgs);
				}
			}
		}

		protected string ComposeFFMpegCommandLineArgs(string inputFile, string inputFormat, string outputFile, string outputFormat, ConvertSettings settings)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (settings.AppendSilentAudioStream)
			{
				stringBuilder.Append(" -f lavfi -i aevalsrc=0 ");
			}
			if (settings.Seek.HasValue)
			{
				stringBuilder.AppendFormat(CultureInfo.InvariantCulture, " -ss {0}", settings.Seek);
			}
			if (inputFormat != null)
			{
				stringBuilder.Append(" -f " + inputFormat);
			}
			if (settings.CustomInputArgs != null)
			{
				stringBuilder.AppendFormat(" {0} ", settings.CustomInputArgs);
			}
			StringBuilder stringBuilder2 = new StringBuilder();
			ComposeFFMpegOutputArgs(stringBuilder2, outputFormat, settings);
			if (settings.AppendSilentAudioStream)
			{
				stringBuilder2.Append(" -shortest ");
			}
			return string.Format("-y -loglevel {4} {0} -i {1} {2} {3}", stringBuilder.ToString(), CommandArgParameter(inputFile), stringBuilder2.ToString(), CommandArgParameter(outputFile), LogLevel);
		}

		public void Invoke(string ffmpegArgs)
		{
			EnsureFFMpegLibs();
			//License.L.Check();
			try
			{
				string fFMpegExePath = GetFFMpegExePath();
				if (!File.Exists(fFMpegExePath))
				{
					throw new FileNotFoundException("Cannot find ffmpeg tool: " + fFMpegExePath);
				}
				ProcessStartInfo processStartInfo = new ProcessStartInfo(fFMpegExePath, ffmpegArgs);
				processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				processStartInfo.CreateNoWindow = true;
				processStartInfo.UseShellExecute = false;
				processStartInfo.WorkingDirectory = Path.GetDirectoryName(FFMpegToolPath);
				processStartInfo.RedirectStandardInput = true;
				processStartInfo.RedirectStandardOutput = false;
				processStartInfo.RedirectStandardError = true;
				InitStartInfo(processStartInfo);
				if (FFMpegProcess != null)
				{
					throw new InvalidOperationException("FFMpeg process is already started");
				}
				FFMpegProcess = Process.Start(processStartInfo);
				if (FFMpegProcessPriority != ProcessPriorityClass.Normal)
				{
					FFMpegProcess.PriorityClass = FFMpegProcessPriority;
				}
				string lastErrorLine = string.Empty;
				FFMpegProcess.ErrorDataReceived += delegate(object o, DataReceivedEventArgs args)
				{
					if (args.Data != null)
					{
						lastErrorLine = args.Data;
						FFMpegLogHandler(args.Data);
					}
				};
				FFMpegProcess.BeginErrorReadLine();
				WaitFFMpegProcessForExit();
				if (FFMpegProcess.ExitCode != 0)
				{
					throw new FFMpegException(FFMpegProcess.ExitCode, lastErrorLine);
				}
				FFMpegProcess.Close();
				FFMpegProcess = null;
			}
			catch (Exception)
			{
				EnsureFFMpegProcessStopped();
				throw;
			}
		}

		internal void FFMpegLogHandler(string line)
		{
			if (this.LogReceived != null)
			{
				this.LogReceived(this, new FFMpegLogEventArgs(line));
			}
		}

		internal void OnConvertProgress(ConvertProgressEventArgs args)
		{
			if (this.ConvertProgress != null)
			{
				this.ConvertProgress(this, args);
			}
		}

		internal void ConvertMedia(Media input, Media output, ConvertSettings settings)
		{
			EnsureFFMpegLibs();
			//License.L.Check();
			string text = input.Filename;
			if (text == null)
			{
				text = Path.GetTempFileName();
				using (FileStream outputStream = new FileStream(text, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					CopyStream(input.DataStream, outputStream, 262144);
				}
			}
			string text2 = output.Filename;
			if (text2 == null)
			{
				text2 = Path.GetTempFileName();
			}
			if ((output.Format == "flv" || Path.GetExtension(text2).ToLower() == ".flv") && !settings.AudioSampleRate.HasValue)
			{
				settings.AudioSampleRate = 44100;
			}
			try
			{
				string fFMpegExePath = GetFFMpegExePath();
				if (!File.Exists(fFMpegExePath))
				{
					throw new FileNotFoundException("Cannot find ffmpeg tool: " + fFMpegExePath);
				}
				string arguments = ComposeFFMpegCommandLineArgs(text, input.Format, text2, output.Format, settings);
				ProcessStartInfo processStartInfo = new ProcessStartInfo(fFMpegExePath, arguments);
				processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				processStartInfo.CreateNoWindow = true;
				processStartInfo.UseShellExecute = false;
				processStartInfo.WorkingDirectory = Path.GetDirectoryName(FFMpegToolPath);
				processStartInfo.RedirectStandardInput = true;
				processStartInfo.RedirectStandardOutput = true;
				processStartInfo.RedirectStandardError = true;
				InitStartInfo(processStartInfo);
				if (FFMpegProcess != null)
				{
					throw new InvalidOperationException("FFMpeg process is already started");
				}
				FFMpegProcess = Process.Start(processStartInfo);
				if (FFMpegProcessPriority != ProcessPriorityClass.Normal)
				{
					FFMpegProcess.PriorityClass = FFMpegProcessPriority;
				}
				string lastErrorLine = string.Empty;
				FFMpegProgress ffmpegProgress = new FFMpegProgress(OnConvertProgress, this.ConvertProgress != null);
				if (settings != null)
				{
					ffmpegProgress.Seek = settings.Seek;
					ffmpegProgress.MaxDuration = settings.MaxDuration;
				}
				FFMpegProcess.ErrorDataReceived += delegate(object o, DataReceivedEventArgs args)
				{
					if (args.Data != null)
					{
						lastErrorLine = args.Data;
						ffmpegProgress.ParseLine(args.Data);
						FFMpegLogHandler(args.Data);
					}
				};
				FFMpegProcess.OutputDataReceived += delegate
				{
				};
				FFMpegProcess.BeginOutputReadLine();
				FFMpegProcess.BeginErrorReadLine();
				WaitFFMpegProcessForExit();
				if (FFMpegProcess.ExitCode != 0)
				{
					throw new FFMpegException(FFMpegProcess.ExitCode, lastErrorLine);
				}
				FFMpegProcess.Close();
				FFMpegProcess = null;
				ffmpegProgress.Complete();
				if (output.Filename == null)
				{
					using (FileStream inputStream = new FileStream(text2, FileMode.Open, FileAccess.Read, FileShare.None))
					{
						CopyStream(inputStream, output.DataStream, 262144);
					}
				}
			}
			catch (Exception)
			{
				EnsureFFMpegProcessStopped();
				throw;
			}
			finally
			{
				if (text != null && input.Filename == null && File.Exists(text))
				{
					File.Delete(text);
				}
				if (text2 != null && output.Filename == null && File.Exists(text2))
				{
					File.Delete(text2);
				}
			}
		}

		private void EnsureFFMpegLibs()
		{
		}

		public void Abort()
		{
			EnsureFFMpegProcessStopped();
		}

		public bool Stop()
		{
			if (FFMpegProcess != null && !FFMpegProcess.HasExited && FFMpegProcess.StartInfo.RedirectStandardInput)
			{
				FFMpegProcess.StandardInput.WriteLine("q\n");
				FFMpegProcess.StandardInput.Close();
				WaitFFMpegProcessForExit();
				return true;
			}
			return false;
		}
	}
}
