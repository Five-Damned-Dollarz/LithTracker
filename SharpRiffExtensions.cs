using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpRiff;

namespace SharpRiffExt
{
	public static class RiffChunkExt
	{
		public static void WriteUTF16(this RiffChunk chunk, string value, int buffer_size)
		{
			if (value == null)
				throw new ArgumentOutOfRangeException("value");

			byte[] buffer = new byte[buffer_size*2];
			Encoding.Unicode.GetBytes(value /*.ToCharArray()*/, 0, value.Length, buffer, 0);
			//value.PadRight(buffer_size, '\0');
			//byte[] buffer = Encoding.Unicode.GetBytes(value);
			
			chunk.Write(buffer);
		}
	}
}