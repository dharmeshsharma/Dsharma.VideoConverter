using System.Diagnostics;
using System.IO;

namespace Dsharma.VideoConverter
{
	internal static class NetStandardCompatibility
	{
		internal static void Close(this StreamWriter wr)
		{
			wr.Dispose();
		}

		internal static void Close(this Stream stream)
		{
			stream.Dispose();
		}

		internal static void Close(this Process p)
		{
			p.Dispose();
		}
	}
}
