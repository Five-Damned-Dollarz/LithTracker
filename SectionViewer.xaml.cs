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
using Microsoft.Win32;

using SharpRiff;

namespace LTracker
{
	/// <summary>
	/// Interaction logic for SectionViewer.xaml
	/// </summary>

	using SharpRiffExt;

	public class Section
	{
		public string name { get; set; }
		public int tempo { get; set; }
		public int bars { get; set; }
		public short groove { get; set; }
		public short bar_ticks { get; set; }

		public string style_name { get; set; }

		public void Load(Stream stream)
		{
			throw new NotImplementedException();
		}

		public void Save(Stream stream, Guid guid)
		{
			RiffFile new_file = new RiffFile(stream, "AASE");

			RiffChunk new_chunk = new_file.CreateChunk("secn");
			new_chunk.Write((int)0);
			new_chunk.WriteUTF16(name, 16);
			new_chunk.Write((short)tempo);
			new_chunk.Write((short)0);
			new_chunk.Write((short)bars);
			new_chunk.Write((short)bar_ticks);
			new_chunk.Write(new byte[12]);
			new_chunk.Write(guid.ToByteArray()); // [16]
			new_chunk.Write(new byte[16]);
			new_chunk.Write(new byte[32], 0, 32);
			new_chunk.Close();

			new_chunk = new_file.CreateChunk("sref");
			string temp_string = style_name + "\0";
			new_chunk.WriteUTF16(temp_string, temp_string.Length * 2);
			new_chunk.Close();

			new_chunk = new_file.CreateChunk("prnm");
			new_chunk.WriteUTF16("Silence\0", 16);
			new_chunk.Close();

			RiffList new_list = new_file.CreateList("AABN", "RIFF");

			new_chunk = new_list.CreateChunk("band");
			new_chunk.WriteUTF16("Default", 16);
			new_chunk.Write(new byte[8]); // unknown
			new_chunk.Write(new byte[16]); // instrument ids?
			new_chunk.Write(Enumerable.Repeat((byte)127, 16).ToArray()); // volumes
			new_chunk.Write(Enumerable.Repeat((byte)64, 16).ToArray()); // pans
			new_chunk.Write(new byte[16]); // transpositions
			new_chunk.Write((short)0); // unknown, seems to be 0 in SEC, but 1 in STY
			new_chunk.Write(new byte[32]); // instrument banks
			new_chunk.Write(new byte[16]); // instrument ids?
			new_chunk.Write(new byte[16]);
			byte[] buffer = Encoding.UTF8.GetBytes("Electrons\0");
			Array.Resize(ref buffer, 16);
			new_chunk.Write(buffer, 0, 16); // DLS name
			new_chunk.Write(new byte[16]);
			new_chunk.Close();

			new_list.Close();

			new_chunk = new_file.CreateChunk("cmnd");
			new_chunk.Write((short)8);
			new_chunk.Write((short)0);
			new_chunk.Write((byte)0);
			new_chunk.Write((short)(1 << (7 + groove)));
			new_chunk.Write((byte)0);
			new_chunk.Write((short)0);
			new_chunk.Close();

			new_file.Close();
		}
	}

	public partial class SectionViewer : Window
	{
		public Guid new_guid;

		public Section section_current { get; private set; }

		public SectionViewer(Guid guid)
		{
			InitializeComponent();

			new_guid = guid;

			this.DataContext = this;

			section_current = new Section();
		}

		private void Command_CanAlwaysExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute=true;
		}

		private void CommandOpen_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var file_dialog=new OpenFileDialog {
				DefaultExt = ".sec",
				Filter = "Section Files (*.sec)|*.sec"
			};

			if (file_dialog.ShowDialog() == true)
			{
				using (var file = System.IO.File.OpenRead(file_dialog.FileName))
				{
					section_current.Load(file);
				}
			}
		}

		private void CommandSave_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var file_dialog = new SaveFileDialog {
				AddExtension = true,
				DefaultExt = ".sec",
				Filter = "Section Files (*.sec)|*.sec"
			};

			if (file_dialog.ShowDialog()==true)
			{
				using (var file = System.IO.File.Create(file_dialog.FileName))
				{
					section_current.Save(file, new_guid);
				}
			}
		}
	}
}
