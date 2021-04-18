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

using SharpRiff;
using SharpRiffExt;

namespace LTracker
{
	/// <summary>
	/// Interaction logic for Tracker.xaml
	/// </summary>

	public class Key
	{
		public Key(NoteDivision parent)
		{
			this.parent = parent;

			note = 0;
			start_ticks = 0;
			start_random = 0;
			length_ticks = 1;
			length_random = 0;
			velocity = 64;
			velocity_random = 0;
			variation_flags = 0xFFFF;
		}

		public NoteDivision parent;

		public byte note { get; set; } // C0 + n

		public short start_ticks { get; set; }
		public byte start_random { get; set; }

		public short length_ticks { get; set; }
		public byte length_random { get; set; }
		
		public byte velocity { get; set; }
		public byte velocity_random { get; set; }

		public ushort variation_flags { get; set; }

		public void Save(RiffChunk stream)
		{
			stream.Write((byte)9);
			stream.Write((byte)4);
			stream.Write(start_ticks);
			stream.Write(variation_flags);
			stream.Write((short)0); // unknown
			stream.Write(note);
			stream.Write(velocity);
			stream.Write((byte)0);
			stream.Write((byte)0);
			stream.Write(length_ticks);
			stream.Write(start_random);
			stream.Write(length_random);
			stream.Write(velocity_random);
			stream.Write((byte)135);
		}
	}
 
	public class NoteDivision
	{
		public int division_id { get; set; }
		public ObservableCollection<Key> note_presses { get; set; }

		public NoteDivision(int division_id)
		{
			this.division_id = division_id;
			note_presses = new ObservableCollection<Key>();
			//note_presses.Add(new Key(this) { note = 12, start_random = 5, length_ticks=64 });
		}

		public void Save(RiffList stream)
		{
			RiffList new_list = stream.CreateList("AACL");
			RiffChunk new_chunk = new_list.CreateChunk("clik");
			new_chunk.Write((short)division_id);
			new_chunk.Close();

			if (note_presses.Count > 0)
			{
				new_chunk = new_list.CreateChunk("note");
				new_chunk.Write((short)18); // unknown
				foreach (Key key in note_presses)
				{
					key.Save(new_chunk);
				}
				new_chunk.Close();
			}

			new_list.Close();
		}
	}

	public class NoteLane
	{
		private readonly Tracker owner_window;

		public byte instrument_id;

		public readonly List<NoteDivision> note_divisions = new List<NoteDivision>();

		public NoteLane(Window owner)
		{
			owner_window = owner as Tracker;
		}

		public void Save(RiffList stream)
		{
			RiffList new_list = stream.CreateList("AAPT", "RIFF");

			RiffChunk new_chunk = new_list.CreateChunk("patt");
			new_chunk.Write(Enumerable.Repeat((byte)0, 5).ToArray());
			new_chunk.Write((short)(3*owner_window.bar_count*(owner_window.beat_ticks/192)));
			new_chunk.Write(Enumerable.Repeat((byte)0, 5).ToArray());
			new_chunk.Write((short)(owner_window.beat_ticks/ owner_window.note_division));
			new_chunk.Write((short)owner_window.note_division); // beat size in nths of a note
			new_chunk.Write((short)owner_window.beat_ticks); // note ticks
			new_chunk.Write((short)owner_window.bar_count); // bar count
			new_chunk.WriteUTF16("patt_name", 16);
			new_chunk.Write(Enumerable.Repeat((byte)0, 42).ToArray());

			new_chunk.Write(Enumerable.Repeat((byte)0xFF, 32 * 16).ToArray()); // variation tables

			new_chunk.Write(Enumerable.Repeat((byte)0, 19).ToArray());
			new_chunk.Close();

			foreach(var note_div in note_divisions)
			{
				note_div.Save(new_list);
			}

			new_list.Close();
		}
	}

	public partial class Tracker : Window
	{
		public NoteLane test_notelane;

		private Guid new_guid;
		
		public int beat_count { get; set; }
		public int note_division { get; set; }
		public int bar_count { get; set; }
		public int tempo { get; set; }
		public int beat_ticks { get; set; }

		public Tracker(Guid guid)
		{
			InitializeComponent();

			test_notelane = new NoteLane(this);

			new_guid = guid;

			beat_count = 4;
			note_division = 4;
			bar_count = 4;
			tempo = 110;
			beat_ticks = 192;

			test_notelane.note_divisions.AddRange(Enumerable.Range(0, 32).Select(x => new NoteDivision(x)));

			this.DataContext = this;
			NoteLane.ItemsSource = test_notelane.note_divisions;
		}

		private void Command_CanAlwaysExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void CommandSave_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var file_dialog = new SaveFileDialog {
				AddExtension = true,
				DefaultExt = ".sty",
				Filter = "Style Files (*.sty)|*.sty"
			};

			if (file_dialog.ShowDialog() == true)
			{
				using (var file = System.IO.File.Create(file_dialog.FileName))
				{
					RiffFile new_file = new RiffFile(file, "AASY");
					RiffChunk new_chunk = new_file.CreateChunk("styl");
					new_chunk.WriteUTF16("Whatever", 16);
					new_chunk.Write((short)beat_count);
					new_chunk.Write((short)note_division);
					new_chunk.Write((short)(beat_ticks/note_division));
					new_chunk.Write((short)beat_ticks);
					new_chunk.Write((short)(beat_ticks*beat_count));
					new_chunk.Write(tempo);
					new_chunk.Write(new_guid.ToByteArray()); // guid
					new_chunk.WriteUTF16("", 16);
					new_chunk.Close();

					new_chunk = new_file.CreateChunk("pref");
					new_chunk.Write((short)60);
					new_chunk.WriteUTF16("Silence", 30);
					new_chunk.WriteUTF16("SILENCE\0", 16);
					new_chunk.Close();

					// band
					RiffList new_list = new_file.CreateList("AABN", "RIFF");

					new_chunk = new_list.CreateChunk("band");
					new_chunk.WriteUTF16("Default", 16);
					new_chunk.Write(new byte[8]); // unknown
					new_chunk.Write(new byte[16]); // instrument ids?
					new_chunk.Write(Enumerable.Repeat((byte)127, 16).ToArray()); // volumes
					new_chunk.Write(Enumerable.Repeat((byte)64, 16).ToArray()); // pans
					new_chunk.Write(new byte[16]); // transpositions
					new_chunk.Write((short)0); // unknown
					new_chunk.Write(new byte[32]); // instrument banks
					new_chunk.Write(new byte[16]); // instrument ids?
					new_chunk.Write(new byte[16]);
					byte[] buffer = Encoding.UTF8.GetBytes("Electrons\0".PadRight(16, '\0'));
					//Array.Resize(ref buffer, 16);
					new_chunk.Write(buffer/*, 0, 16*/); // DLS name
					new_chunk.Write(new byte[16]);
					new_chunk.Close();

					new_list.Close();

					//
					test_notelane.Save(new_file);

					new_file.Close();
				}
			}
		}

		private void AddNewKey(object sender, RoutedEventArgs e)
		{
			var element = sender as FrameworkElement;
			var notes = ((NoteDivision)element.DataContext).note_presses;
			notes.Add(new Key((NoteDivision)element.DataContext));
		}

		private void DeleteKey(object sender, RoutedEventArgs e)
		{
			var element = sender as FrameworkElement;
			var notes = ((Key)element.DataContext).parent.note_presses;
			notes.Remove((Key)element.DataContext);
		}
	}

	public class NoteValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is byte)
			{
				int temp_value = (byte)value / 12;
				Notes note = (Notes)((byte)value % 12);

				return note.ToString() + temp_value.ToString();
			}

			return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			//string temp_str = value as string;

			int temp_value = (byte)value / 12;
			Notes note = (Notes)((byte)value % 12);

			return note.ToString() + temp_value.ToString();
		}
	}
}
