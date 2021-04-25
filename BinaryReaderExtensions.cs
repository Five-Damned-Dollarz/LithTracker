using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace LTracker
{
	static class BinaryReaderExtensions
	{
		public static string ReadStringNullTerminated(this BinaryReader reader)
		{
			StringBuilder builder = new StringBuilder();
			byte rc;
			while((rc=reader.ReadByte())!='\0')
			{
				builder.Append((char)rc);
			}

			return builder.ToString();
		}

		public static void WriteStringNullTerminated(this BinaryWriter writer, string str)
		{
			byte[] buffer = Encoding.ASCII.GetBytes(str+'\0');
			writer.Write(buffer);
		}
	}
}