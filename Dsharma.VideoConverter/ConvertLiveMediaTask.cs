using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Dsharma.VideoConverter
{
	public class ConvertLiveMediaTask
	{
		internal class StreamOperationContext
		{
			private bool isInput;

			private bool isRead;

			public Stream TargetStream
			{
				get;
				private set;
			}

			public bool Read => isRead;

			public bool Write => !isRead;

			public bool IsInput => isInput;

			public bool IsOutput => !isInput;

			internal StreamOperationContext(Stream stream, bool isInput, bool isRead)
			{
				TargetStream = stream;
				this.isInput = isInput;
				this.isRead = isRead;
			}
		}

		private Stream Input;

		private Stream Output;

		private FFMpegConverter FFMpegConv;

		private string FFMpegToolArgs;

		private Process FFMpegProcess;

		private Thread CopyToStdInThread;

		private Thread CopyFromStdOutThread;

		public EventHandler OutputDataReceived;

		private string lastErrorLine;

		private FFMpegProgress ffmpegProgress;

		private long WriteBytesCount;

		private Exception lastStreamException;

		internal ConvertLiveMediaTask(FFMpegConverter ffmpegConv, string ffMpegArgs, Stream inputStream, Stream outputStream, FFMpegProgress progress)
		{
			Input = inputStream;
			Output = outputStream;
			FFMpegConv = ffmpegConv;
			FFMpegToolArgs = ffMpegArgs;
			ffmpegProgress = progress;
		}

		public void Start()
		{
			lastStreamException = null;
			string fFMpegExePath = FFMpegConv.GetFFMpegExePath();
			if (!File.Exists(fFMpegExePath))
			{
				throw new FileNotFoundException("Cannot find ffmpeg tool: " + fFMpegExePath);
			}
			//License.L.Check();
			ProcessStartInfo processStartInfo = new ProcessStartInfo(fFMpegExePath, "-stdin " + FFMpegToolArgs);
			processStartInfo.CreateNoWindow = true;
			processStartInfo.UseShellExecute = false;
			processStartInfo.WorkingDirectory = Path.GetDirectoryName(fFMpegExePath);
			processStartInfo.RedirectStandardInput = true;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardError = true;
			processStartInfo.StandardOutputEncoding = Encoding.UTF8;
			FFMpegConv.InitStartInfo(processStartInfo);
			FFMpegProcess = Process.Start(processStartInfo);
			if (FFMpegConv.FFMpegProcessPriority != ProcessPriorityClass.Normal)
			{
				FFMpegProcess.PriorityClass = FFMpegConv.FFMpegProcessPriority;
			}
			lastErrorLine = null;
			ffmpegProgress.Reset();
			FFMpegProcess.ErrorDataReceived += delegate(object o, DataReceivedEventArgs args)
			{
				if (args.Data != null)
				{
					lastErrorLine = args.Data;
					ffmpegProgress.ParseLine(args.Data);
					FFMpegConv.FFMpegLogHandler(args.Data);
				}
			};
			FFMpegProcess.BeginErrorReadLine();
			if (Input != null)
			{
				CopyToStdInThread = new Thread(CopyToStdIn);
				CopyToStdInThread.Start();
			}
			else
			{
				CopyToStdInThread = null;
			}
			if (Output != null)
			{
				CopyFromStdOutThread = new Thread(CopyFromStdOut);
				CopyFromStdOutThread.Start();
			}
			else
			{
				CopyFromStdOutThread = null;
			}
		}

		public void Write(byte[] buf, int offset, int count)
		{
			if (FFMpegProcess.HasExited)
			{
				if (FFMpegProcess.ExitCode != 0)
				{
					throw new FFMpegException(FFMpegProcess.ExitCode, string.IsNullOrEmpty(lastErrorLine) ? "FFMpeg process has exited" : lastErrorLine);
				}
				throw new FFMpegException(-1, "FFMpeg process has exited");
			}
			FFMpegProcess.StandardInput.BaseStream.Write(buf, offset, count);
			FFMpegProcess.StandardInput.BaseStream.Flush();
			WriteBytesCount += count;
		}

		public void Stop()
		{
			Stop(forceFFMpegQuit: false);
		}

		public void Stop(bool forceFFMpegQuit)
		{
			if (CopyToStdInThread != null)
			{
				CopyToStdInThread = null;
			}
			if (forceFFMpegQuit)
			{
				if (Input == null && WriteBytesCount == 0L)
				{
					FFMpegProcess.StandardInput.WriteLine("q\n");
					NetStandardCompatibility.Close(FFMpegProcess.StandardInput);
				}
				else
				{
					Abort();
				}
			}
			else
			{
				NetStandardCompatibility.Close(FFMpegProcess.StandardInput.BaseStream);
			}
			Wait();
		}

		private void OnStreamError(Exception ex, bool isStdinStdout)
		{
			if (!(ex is IOException && isStdinStdout))
			{
				lastStreamException = ex;
				Abort();
			}
		}

		protected void CopyToStdIn()
		{
			byte[] array = new byte[65536];
			Thread copyToStdInThread = CopyToStdInThread;
			Process fFMpegProcess = FFMpegProcess;
			Stream baseStream = FFMpegProcess.StandardInput.BaseStream;
			while (true)
			{
				int num;
				try
				{
					num = Input.Read(array, 0, array.Length);
				}
				catch (Exception ex)
				{
					OnStreamError(ex, isStdinStdout: false);
					return;
				}
				if (num <= 0)
				{
					break;
				}
				if (FFMpegProcess == null || copyToStdInThread != CopyToStdInThread || fFMpegProcess != FFMpegProcess)
				{
					return;
				}
				try
				{
					baseStream.Write(array, 0, num);
					baseStream.Flush();
				}
				catch (Exception ex2)
				{
					OnStreamError(ex2, isStdinStdout: true);
					return;
				}
			}
			NetStandardCompatibility.Close(FFMpegProcess.StandardInput);
		}

		protected void CopyFromStdOut()
		{
			byte[] array = new byte[65536];
			Thread copyFromStdOutThread = CopyFromStdOutThread;
			Stream baseStream = FFMpegProcess.StandardOutput.BaseStream;
			while (copyFromStdOutThread == CopyFromStdOutThread)
			{
				int num;
				try
				{
					num = baseStream.Read(array, 0, array.Length);
				}
				catch (Exception ex)
				{
					OnStreamError(ex, isStdinStdout: true);
					return;
				}
				if (num > 0)
				{
					if (copyFromStdOutThread != CopyFromStdOutThread)
					{
						break;
					}
					try
					{
						Output.Write(array, 0, num);
						Output.Flush();
					}
					catch (Exception ex2)
					{
						OnStreamError(ex2, isStdinStdout: false);
						return;
					}
					if (OutputDataReceived != null)
					{
						OutputDataReceived(this, EventArgs.Empty);
					}
				}
				else
				{
					Thread.Sleep(30);
				}
			}
		}

		public void Wait()
		{
			FFMpegProcess.WaitForExit(int.MaxValue);
			if (CopyToStdInThread != null)
			{
				CopyToStdInThread = null;
			}
			if (CopyFromStdOutThread != null)
			{
				CopyFromStdOutThread = null;
			}
			if (FFMpegProcess.ExitCode != 0)
			{
				throw new FFMpegException(FFMpegProcess.ExitCode, lastErrorLine ?? "Unknown error");
			}
			if (lastStreamException != null)
			{
				throw new IOException(lastStreamException.Message, lastStreamException);
			}
			NetStandardCompatibility.Close(FFMpegProcess);
			ffmpegProgress.Complete();
		}

		public void Abort()
		{
			if (CopyToStdInThread != null)
			{
				CopyToStdInThread = null;
			}
			if (CopyFromStdOutThread != null)
			{
				CopyFromStdOutThread = null;
			}
			try
			{
				FFMpegProcess.Kill();
			}
			catch (InvalidOperationException)
			{
			}
		}
	}
}
