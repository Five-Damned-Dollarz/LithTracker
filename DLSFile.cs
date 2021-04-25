using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace DownloadableSound
{
	class DLSFile : BinaryReader
	{
		List<Instrument> instruments;
		List<WaveFile> wave_files;

		public DLSFile(Stream input) : base(input, Encoding.ASCII)
		{
			//
		}

		public Region ReadRegion()
		{
			Region region = new Region();

			region.SetRanges(ReadUInt16(), ReadUInt16(), ReadUInt16(), ReadUInt16());
			ReadUInt16(); // flags
			ReadUInt16(); // key group

			return region;
		}
	}

	class Instrument
	{
		int bank;
		int patch;
		List<Region> regions;

		string name;
	}

	class Region
	{
		ushort key_low, key_high;
		ushort velocity_low, velocity_high;
		public enum Flags : ushort
		{
			None=0,
			SelfNonExclusive, // unsure if this is actually 1
		}
		Region.Flags flags;
		ushort key_group; // 0 = no group, 1-15 = group 1-15, all other values reserved

		public void SetRanges(ushort key_low=0x00, ushort key_high=0x7F, ushort vel_low=0x00, ushort vel_high=0x7F)
		{
			this.key_low = key_low;
			this.key_high = key_high;
			velocity_low = vel_low;
			velocity_high = vel_high;
		}
	}

	class WaveSample
	{
		ushort base_note; // 0x00-0x7F; C0-??
		short fine_tune;
		int attenuation;
		uint flags;
		uint loop_count { get => (uint)loops.Count; } // technically the format can handle more than 1 loop, but..?

		public void SetPitch(ushort base_note, short fine_tune, int attenuation)
		{
			this.base_note = base_note;
			this.fine_tune = fine_tune;
			this.attenuation = attenuation;
		}

		class Loop
		{
			public enum Type : uint
			{
				Forward=0,
				// DLS1 doesn't support others
			}

			Loop.Type type; // DLS1 only has forward loop = 0?
			uint start; // start point in samples from data[0]
			uint length; // length in samples
		}
		readonly List<Loop> loops = new List<Loop>();
	}

	/*
	 * Wave block
	 */


	class WaveFile
	{
		// borrow windows names, I guess?
		public enum Format : short
		{
			None=0,
			PCM,
			ADPCM,
			IEEE_FLOAT,
		}
		WaveFile.Format wFormatTag;
		short nChannels;
		int nSamplesPerSec;
		int nAvgBytesPerSec;
		short nBlockAlign;
		short wBitsPerSample;
		short cbSize;

		ulong data_length;
		byte[] data;

		WaveSample sample;
		
		string name;
	}
}
