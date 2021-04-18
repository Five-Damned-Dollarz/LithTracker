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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using System.ComponentModel;
using System.Collections.ObjectModel;
using WinForms = System.Windows.Forms;
using Microsoft.Win32;

using SharpRiff;

namespace LTracker
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>

	using SharpRiffExt;

	class SongFile
	{
		public string file_path { get; set; }
		public string file_name { get; set; }

		public SongFile(string file_path)
		{
			this.file_path = file_path;
			this.file_name = System.IO.Path.GetFileNameWithoutExtension(file_path);
		}
	}

	enum Notes
	{
		C,
		CSharp,
		D,
		DSharp,
		E,
		F,
		FSharp,
		G,
		GSharp,
		A,
		ASharp,
		B
	}

	public class Channel
	{
		public Channel(string name, float volume, float pan, int transpose, int instrument_id)
		{
			this.name = name;
			this.volume = volume;
			this.pan = pan;
			this.transpose = transpose;
			this.high_bank_id = 0;
			this.low_bank_id = 0;
			this.instrument_id = instrument_id;
		}
		public string name { get; set; }
		public float volume { get; set; }
		public float pan { get; set; }
		public int transpose { get; set; }
		// I guess have a direct ref to whatever instrument, and automagically return the stuff we need instead of these?
		public int high_bank_id { get; set; }  // [0-127]
		public int low_bank_id { get; set; }   // ^^
		public int instrument_id { get; set; } // ^^
	}

	public class Band // exists in both DLS and Styles
	{
		public Band()
		{
			channels.Add(new Channel("Drums", 50f, 0.5f, 0, 1));
			channels.Add(new Channel("Lead", 87f, -0.5f, 0, 2));
			channels.Add(new Channel("Bass", 70f, 0.1f, 0, 3));
			channels.Add(new Channel("Memes", 100f, 0.0f, 0, 4));

			using (FileStream new_file = File.Create("test.riff"))
			using (RiffFile fs = new RiffFile(new_file, "AASE"))
			{
				var temp_chunk = fs.CreateChunk("secn");
				temp_chunk.Write((int)1025);
				temp_chunk.WriteUTF16("Name", 16);
				temp_chunk.Close();
			}
		}

		public string name;
		public List<Channel> channels = new List<Channel>();
	}

	public partial class MainWindow : Window
	{
		public readonly Guid new_guid = Guid.NewGuid();

		private readonly ObservableCollection<RiffChunk> riff_chunks = new ObservableCollection<RiffChunk>();
		private SongFile _selected_file { get; set; }
		private string song_folder_current;
		private readonly Band band_current = new Band();
		private readonly ObservableCollection<SongFile> song_files = new ObservableCollection<SongFile>();

		public MainWindow()
		{
			InitializeComponent();
			this.DataContext = this;

			ChunkList.ItemsSource = song_files;
			ChunkList.DisplayMemberPath = "file_name";

			Channels.ItemsSource = band_current.channels;
		}

		private void ReadChunks(RiffList file_in, ref string str_out)
		{
			foreach (RiffChunk thing in file_in.Descendants())
			{
				if (thing.ChunkId == "RIFF" || thing.ChunkId == "LIST")
				{
					RiffList temp_chunk = new RiffList(thing, thing.ChunkId);
					//RiffList temp_chunk = thing.ToList(); // can't do this because it doesn't think RIFF is a LIST, easy fix though
					str_out += temp_chunk.ListId + "\n";
					ReadChunks(temp_chunk, ref str_out);
				}
				else
				{
					str_out += " - " + thing.ChunkId + "\n";
					riff_chunks.Add(thing);
				}
			}
		}

		private void OpenFolder(object sender, RoutedEventArgs e)
		{
			var folder_dialog = new WinForms.FolderBrowserDialog();
			var ret = folder_dialog.ShowDialog();
			if (ret == WinForms.DialogResult.OK)
			{
				song_folder_current = folder_dialog.SelectedPath;
				song_files.Clear();
				foreach(string file in Directory.EnumerateFiles(song_folder_current))
				{
					song_files.Add(new SongFile(file));
				}
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog file_dialog = new OpenFileDialog {
				DefaultExt = ".sec",
				Filter = "All Supported Files (*.sec, *.sty, *.dls)|*.sec;*.sty;*.dls|Section Files (*.sec)|*.sec|Style Files (*.sty)|*.sty|Downloadable Sounds (*.dls)|*.dls"
			};

			if (file_dialog.ShowDialog() == true)
			{
				RiffFile riff_file = new RiffFile(System.IO.File.OpenRead(file_dialog.FileName));

				string temp_string = "";
				riff_chunks.Clear();
				ReadChunks(riff_file, ref temp_string);

				TrackerName.Text = file_dialog.FileName;
				TestTextBox.Text = temp_string;
			}
		}

		private void OpenPatternWindow(object sender, RoutedEventArgs e)
		{
			var new_window = new Tracker(new_guid);
			new_window.Show();
		}

		private void OpenSectionWindow(object sender, RoutedEventArgs e)
		{
			var new_window = new SectionViewer(new_guid);
			new_window.Show();
		}

		private void ChunkList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			_selected_file = (sender as ListBox).SelectedItem as SongFile;
			TrackerName.DataContext = _selected_file;
		}

		private void OpenCommand_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
		{
			e.CanExecute=true;
		}

		private void OpenCommand_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
		{
			OpenFolder(sender, e);
		}

		private void OpenInstrumentsWindow(object sender, RoutedEventArgs e)
		{
			var new_window = new InstrumentsViewer();
			new_window.Show();
		}
	}
}
