using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.IO;
using System.Collections.ObjectModel;
using Microsoft.Win32;

using System.Diagnostics;

using SharpRiff;

namespace LTracker
{
	/// <summary>
	/// Interaction logic for InstrumentsViewer.xaml
	/// </summary>

	//
	public struct Range
	{
		public short start;
		public short end;
	}

	public class Region
	{
		public Range range { get; set; } // 0-127
		public Range velocity { get; set; } // 0-127
		public int base_note { get; set; }

		// Wave sample { get; set; }

		public Instrument parent;

		public Region(Instrument parent)
		{
			this.parent = parent;
		}

		public void Load(RiffList list)
		{
			foreach(RiffChunk chunk in list.Descendants())
			{
				switch(chunk.ChunkId)
				{
					case "rgnh":
						Range temp_range;
						temp_range.start = chunk.ReadInt16();
						temp_range.end = chunk.ReadInt16();
						range = temp_range;

						temp_range.start = chunk.ReadInt16();
						temp_range.end = chunk.ReadInt16();
						velocity = temp_range;
						break;

					case "wsmp":
						chunk.ReadUInt32();
						base_note = chunk.ReadInt16();
						break;
					case "wlnk":
						break;
				}
			}
		}
	}

	public class Instrument
	{
		public string name { get; set; }
		public int patch_id { get; set; }

		public ObservableCollection<Region> regions { get; set; }

		public Instrument()
		{
			regions = new ObservableCollection<Region>();

			//regions.Add(new Region(this));
		}

		public void Load(RiffList list)
		{
			foreach(RiffChunk chunk in list.Descendants())
			{
				switch(chunk.ChunkId)
				{
					case "insh":
						//
						break;
					case "LIST":
						switch (chunk.ToList().ListId)
						{
							case "lrgn":
								foreach (RiffChunk ins_chunk in chunk.ToList().Descendants())
								{
									Region new_region = new Region(this);
									new_region.Load(ins_chunk.ToList());
									regions.Add(new_region);
								}
								break;
							case "INFO":
								foreach (RiffChunk new_chunk in chunk.ToList().Descendants())
								{
									switch(new_chunk.ChunkId)
									{
										case "INAM":
											name = new_chunk.ReadChars((int)new_chunk.Length).ToString();
											Debug.WriteLine(name);
											break;
									}
								}
								break;
						}
						break;
				}
			}
		}
	}

	public class DLS
	{
		public string name { get; set; }

		public readonly ObservableCollection<Instrument> instruments = new ObservableCollection<Instrument>();
	}

	public struct WaveFormat
	{
		public enum FormatTag : ushort
		{
        WAVE_FORMAT_UNKNOWN=0,
        WAVE_FORMAT_PCM=1,
        WAVE_FORMAT_ADPCM,
        WAVE_FORMAT_IEEE_FLOAT
		}

		FormatTag format_tag;
		short channel_count;
		int samples_per_second;
		int average_bytes_per_sec;
		short block_align;
		short bits_per_sample;
		short extra_size; // should be 0 if we don't need extension, count includes data above
	}

	public class Sample
	{
		WaveFormat format;

		int base_note;
		int fine_tune;
		int attenuation;
		//int flags;
		int loop_count;

		byte[] data;
	}

	public partial class InstrumentsViewer : Window
	{
		public readonly DLS test_dls = new DLS();

		public InstrumentsViewer()
		{
			InitializeComponent();

			this.DataContext = this;
			InstrumentList.ItemsSource = test_dls.instruments;

			var temp = new Instrument();
			temp.name = "Test Instrument";
			test_dls.instruments.Add(temp);
		}

		private void Command_CanAlwaysExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void CommandOpen_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var file_dialog = new OpenFileDialog();
			if (file_dialog.ShowDialog() == true)
			{
				RiffFile riff_file = new RiffFile(System.IO.File.OpenRead(file_dialog.FileName));
				ReadChunks(riff_file);
			}
		}

		private void ReadChunks(RiffList file_in)
		{
			foreach (RiffChunk thing in file_in.Descendants())
			{
				if (thing.ChunkId == "RIFF" || thing.ChunkId == "LIST")
				{
					RiffList temp_chunk = new RiffList(thing, thing.ChunkId);

					switch (temp_chunk.ListId)
					{
						case "lins":
							// instruments list
							Instrument new_inst = new Instrument();
							new_inst.Load(temp_chunk);
							test_dls.instruments.Add(new_inst);
							break;

						case "wvpl":
							// wave table
							break;
					}
				}
			}
		}

		private void AddRegion(object sender, RoutedEventArgs e)
		{
			FrameworkElement element = sender as FrameworkElement;
			(element.DataContext as Instrument).regions.Add(new Region(element.DataContext as Instrument));
		}

		private void DeleteRegion(object sender, RoutedEventArgs e)
		{
			var element = sender as FrameworkElement;
			var regions = ((Region)element.DataContext).parent.regions;
			regions.Remove((Region)element.DataContext);
		}
	}
}
