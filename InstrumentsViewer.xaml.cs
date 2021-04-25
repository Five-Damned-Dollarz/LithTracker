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
using System.ComponentModel;
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
		public short start { get; set; }
		public short end { get; set; }

		public override string ToString()
		{
			return start + "-" + end;
		}
	}

	public class Region
	{
		public Range key_range { get; set; } // 0-127
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
						Range temp_range=new Range();
						temp_range.start = chunk.ReadInt16();
						temp_range.end = chunk.ReadInt16();
						key_range = temp_range;

						temp_range.start = chunk.ReadInt16();
						temp_range.end = chunk.ReadInt16();
						velocity = temp_range;

						chunk.ReadUInt16(); // flags
						chunk.ReadUInt16(); // key group
						break;

					case "wsmp":
						int size=(int)(chunk.Length-chunk.ReadUInt32());
						base_note = chunk.ReadInt16();
						chunk.ReadInt16(); // fine tune
						chunk.ReadInt32(); // attenuation
						chunk.ReadUInt32(); // flags
						/*loop_count=*/chunk.ReadUInt32(); // loop count

						if (size > 0) // loops array
						{
							chunk.ReadBytes(size);

							/*for(int i=0; i<loop_count; ++i)
							{
								Loop temp_loop = new Loop();
								temp_loop.type = (LoopType)chunk.ReadUInt32();
								temp_loop.start = chunk.ReadUInt32();
								temp_loop.end = chunk.ReadUInt32();
								loops.Add(temp_loop);
							}*/
						}
						break;
					case "wlnk":
						chunk.ReadUInt16(); // flags
						chunk.ReadUInt16(); // phase group
						chunk.ReadUInt32(); // channel id
						chunk.ReadUInt32(); // table index
						break;
				}
			}
		}
	}

	public class Instrument : INotifyPropertyChanged
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
			if (list.ListId != "ins ")
				throw new ArgumentOutOfRangeException();

			foreach(RiffChunk chunk in list.Descendants())
			{
				switch(chunk.ChunkId)
				{
					case "insh":
						Debug.WriteLine("insh: regions " + chunk.ReadUInt32()+" bank "+chunk.ReadUInt32()+" id "+chunk.ReadUInt32());
						break;
					case "LIST":
						RiffList as_chunk = chunk.ToList();
						switch (as_chunk.ListId)
						{
							case "lrgn":
								foreach (RiffChunk ins_chunk in as_chunk.Descendants())
								{
									Region new_region = new Region(this);
									new_region.Load(ins_chunk.ToList());
									regions.Add(new_region);
								}
								break;
							case "lart":
								foreach(RiffChunk art_chunk in as_chunk.Descendants())
								{
									int size = (int)(art_chunk.Length - art_chunk.ReadUInt32());
									art_chunk.ReadUInt32(); // block count
									if (size > 0)
										art_chunk.ReadBytes(size);
								}
								break;
							case "INFO":
								foreach (RiffChunk new_chunk in as_chunk.Descendants())
								{
									switch(new_chunk.ChunkId)
									{
										case "INAM":
											byte[] str_array = new_chunk.ReadBytes((int)new_chunk.Length);
											name = Encoding.ASCII.GetString(str_array, 0, str_array.Length-1);
											Debug.WriteLine("Instrument name: "+name);
											break;
									}
								}
								break;
						}
						break;
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(String propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class DLS
	{
		public string name { get; set; }

		public ObservableCollection<Instrument> instruments { get; }
		//public readonly ObservableCollection<Wave> waves = new ObservableCollection<Wave>();

		public DLS()
		{
			instruments = new ObservableCollection<Instrument>();
		}

		public void Load(RiffList list)
		{
			foreach(RiffChunk chunk in list.Descendants())
			{
				switch(chunk.ChunkId)
				{
					case "colh":
						Debug.WriteLine("colh: "+chunk.ReadUInt32());
						break;

					case "vers":
						Debug.WriteLine("vers: "+chunk.ReadUInt16()+chunk.ReadUInt16()+chunk.ReadUInt16()+chunk.ReadUInt16());
						break;

					case "msyn":
						Debug.WriteLine("msyn: "+chunk.ReadUInt32());
						break;

					case "ptbl":
						Debug.WriteLine("ptbl len: "+chunk.ReadBytes((int)chunk.Length).Length);
						break;

					case "LIST":
						RiffList as_list = chunk.ToList();
						switch (as_list.ListId)
						{
							case "lins":
								foreach (RiffChunk ins_chunk in as_list.Descendants())
								{
									Instrument new_instrument = new Instrument();
									new_instrument.Load(ins_chunk.ToList());
									instruments.Add(new_instrument);
								}
								break;

							case "wvpl":
								Debug.WriteLine("wvpl len: "+chunk.ReadBytes((int)chunk.Length-4).Length);
								break;

							case "INFO":
								foreach(RiffChunk info_chunk in as_list.Descendants())
								{
									switch(info_chunk.ChunkId)
									{
										case "INAM":
											byte[] str_array = info_chunk.ReadBytes((int)info_chunk.Length);
											name = Encoding.ASCII.GetString(str_array, 0, str_array.Length-1);
											Debug.WriteLine("DLS name: " + name);
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
		public readonly ObservableCollection<DLS> test_dls = new ObservableCollection<DLS>();

		public InstrumentsViewer()
		{
			InitializeComponent();

			this.DataContext = this;
			CollectionList.ItemsSource = test_dls;
			//InstrumentList.ItemsSource = test_dls.instruments;

			//var temp = new Instrument();
			//temp.name = "Test Instrument";
			//test_dls.instruments.Add(temp);
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
				var new_dls = new DLS();
				new_dls.Load(riff_file);
				test_dls.Add(new_dls);
				//ReadChunks(riff_file);
			}
		}

		/*private void ReadChunks(RiffList file_in)
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
		}*/

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
