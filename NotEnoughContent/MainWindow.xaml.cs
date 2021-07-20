using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace NotEnoughContent
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Dict: Title - ID
        public Dictionary<string, string> SlidesDict { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            ToggleAudioEnabled(false);
        }

        private void Rectangle_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files[0].Contains("index.html"))
                {
                    // Pass file to handler.
                    HandleIndexFile(files[0]);
                }
            }
        }

        private void HandleIndexFile(string file)
        {
            string mainDir = new FileInfo(file).Directory.FullName;
            string manifestPath = Path.Combine(mainDir, "content\\manifest.json");
            string[] manifest = File.ReadAllLines(manifestPath);

            List<int> titleLine = new List<int>();
            string titleStr = "";
            for (int i = 0; i < manifest.Length; i++)
            {
                if (manifest[i].Contains("\"title\":") && !manifest[i + 1].Contains("\"type\": \"root\""))
                {
                    titleLine.Add(i);
                }
                else if (i + 1 < manifest.Length && manifest[i + 1].Contains("\"type\": \"root\""))
                {
                    titleStr = manifest[i].Trim();
                    titleStr = titleStr.Remove(0, 10);
                    titleStr = titleStr.Remove(titleStr.Length - 2);
                }
            }

            SlidesDict = new Dictionary<string, string>();

            foreach (int lineNo in titleLine)
            {
                string trimmedTitle = manifest[lineNo].Trim(); //Remove Whitespace
                trimmedTitle = trimmedTitle.Remove(0, 10); //Remove "title":
                trimmedTitle = trimmedTitle.Remove(trimmedTitle.Length - 2); //Remove stray punctuation at the end

                string trimmedId = manifest[lineNo - 4].Trim();
                trimmedId = trimmedId.Remove(0, 7);
                trimmedId = trimmedId.Remove(trimmedId.Length - 2);

                SlidesDict.Add(trimmedId, trimmedTitle);
            }
            ComboBoxSlides.ItemsSource = SlidesDict;

            LabelStatus.Content = "Projekt \"" + titleStr + "\" erfolgreich geladen.";
        }

        private void TextBoxAudiofile_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files[0].EndsWith(".mp3") || files[0].EndsWith(".wav"))
                {
                    // Pass file to handler.
                    HandleAudioFile(files[0]);
                }
            }
        }

        private void ButtonOpenAudio_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".mp3";
            dlg.Filter = "MP3 Files (*.mp3)|*.mp3|Waveform Audio Files (*.wav)|*.wav";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                HandleAudioFile(dlg.FileName);
            }
        }

        private void HandleAudioFile(string filePath)
        {
            TextBoxAudiofile.Text = filePath;
        }

        private void ButtonAudioHelp_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Loop: Audiodatei endlos wiederholen.\n" +
                "Controls: Play/Pause anzeigen.\n" +
                "Autoplay: Audiofile automatisch abspielen, sobald sie geladen wird.\n" +
                "Muted: Audiodatei stummschalten.\n", "Erklärung");
        }

        private void TextBoxAudiofile_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void ToggleAudioEnabled(bool enable)
        {
            TextBoxAudiofile.IsEnabled = enable;
            ButtonOpenAudio.IsEnabled = enable;
            CheckBoxAutoplay.IsEnabled = enable;
            CheckBoxLoop.IsEnabled = enable;
            CheckBoxControls.IsEnabled = enable;
            CheckBoxMuted.IsEnabled = enable;
            ButtonAudioHelp.IsEnabled = enable;
            ButtonAudioInsert.IsEnabled = enable;
        }

        private void ButtonAudioInsert_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(TextBoxAudiofile.Text) || !(TextBoxAudiofile.Text.EndsWith(".mp3") || TextBoxAudiofile.Text.EndsWith(".wav")))
            {
                MessageBox.Show("Bitte geben Sie eine gültige Audiodatei an!");
            }
            else
            {
                
            }
        }

        private void ComboBoxSlides_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxSlides.SelectedItem != null && ComboBoxSlides.SelectedItem.ToString() != "")
                ToggleAudioEnabled(true);
            else
                ToggleAudioEnabled(false);
        }
    }
}
