using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NotEnoughContent
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Dict: Title - ID
        public Dictionary<string, string> SlidesDict { get; private set; }
        public List<string> mods { get; private set; }
        string mainDir;

        public MainWindow()
        {
            InitializeComponent();
            this.Title += Assembly.GetExecutingAssembly().GetName().Version.Major.ToString() + "." + Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString();
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
            mainDir = new FileInfo(file).Directory.FullName;
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

            PopulateAudio(titleLine, manifest);
            PopulateSplash();

            LabelStatus.Content = "Projekt \"" + titleStr + "\" erfolgreich geladen.";
        }

        private void PopulateAudio(List<int> titleLine, string[] manifest)
        {
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

            LoadMods();

            ButtonDelAudio.IsEnabled = true;
        }

        private void PopulateSplash()
        {
            //Find Image
            string logoPath = null;
            string[] loadscreenHtml = File.ReadAllLines(Path.Combine(mainDir, @"resources\style\controls\loadscreen.ctl.html"));
            foreach (string line in loadscreenHtml)
                if (line.Contains("resources/style/images/"))
                    logoPath = line.Trim();
            logoPath = logoPath.Replace("<img src=\"", "").Replace("\"/>", "");
            logoPath = Path.GetFullPath(Path.Combine(mainDir, logoPath));

            loadSplash(logoPath);
        }

        private void loadSplash(string logoPath)
        {
            bool isSvg = logoPath.EndsWith(".svg");

            BitmapImage img = new BitmapImage();
            img.BeginInit();
            //Set Image
            if (isSvg)
            {
                var svgDoc = SvgDocument.Open(logoPath);
                MemoryStream ms = new MemoryStream();
                svgDoc.Draw().Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                img.StreamSource = ms;
            }
            else
                img.UriSource = new Uri(logoPath);
            img.EndInit();
            ImageLogo.Source = img;
        }

        private void LoadMods()
        {
            mods = new List<string>();
            foreach (string line in File.ReadLines(Path.Combine(mainDir, @"content\assets\course.js")))
            {
                if (line.EndsWith("//Added by NotEnoughContent"))
                { //Expandable when necessary.
                    switch (line)
                    {
                        case string audio when audio.Contains("audio"):
                            string currMod = line.Remove(0, 45);
                            currMod = currMod.Remove(currMod.Length - 38);
                            mods.Add(currMod);
                            break;
                        default:
                            MessageBox.Show("Unbekannte Modfikationen gefunden. Wurden sie mit einer neueren Version von NotEnoughContent erstellt?", "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning);
                            break;
                    }
                }
            }
            ListBoxMods.ItemsSource = mods;
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

        private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
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
                string audioName = Path.GetFileName(TextBoxAudiofile.Text);
                string slideName = ((KeyValuePair<string, string>)ComboBoxSlides.SelectedItem).Value;
                string slideId = ((KeyValuePair<string, string>)ComboBoxSlides.SelectedItem).Key;
                string jsInsert = "$.getScript(\"content/assets/notenoughcontent/" + audioName + "-" + slideName + "-audio.js\")//Added by NotEnoughContent";
                
                List<string> jsMain = new List<string>() { //This is the JS code necessary to watch the main viewport container for changes and insert an html audio element.
                    "var observer = new MutationObserver(audio" + slideName + ");",
                    "var targetNode = document.getElementById(\"imc-viewport-container\");",
                    "observer.observe(targetNode, { childList: true, subtree: true });",
                    "function audio" + slideName + "() {",
                    "setTimeout(function() {",
                    "if (document.getElementById(\"" + slideId + "\").style.transform != \"\" && document.getElementById(\"audio" + slideName + "\") == null){",
                    "var audio" + slideName + "node = document.getElementById(\"" + slideId + "\");",
                    "var audio" + slideName + "element = document.createElement(\"audio\");",
                    "audio" + slideName + "element.src = \"content/assets/notenoughcontent/" + audioName + "\";",
                    "audio" + slideName + "element.id = \"audio" + slideName + "\";",
                    "audio" + slideName + "node.appendChild(audio" + slideName + "element);",
                    "}",
                    "}, 200);",
                    "}"
                };
                if (CheckBoxAutoplay.IsChecked == true)
                    jsMain.Insert(9, "audio" + slideName + "element.autoplay = true;");
                if (CheckBoxControls.IsChecked == true)
                    jsMain.Insert(9, "audio" + slideName + "element.controls = true;");
                if (CheckBoxLoop.IsChecked == true)
                    jsMain.Insert(9, "audio" + slideName + "element.loop = true;");
                if (CheckBoxMuted.IsChecked == true)
                    jsMain.Insert(9, "audio" + slideName + "element.muted = true;");

                string workDir = Path.Combine(mainDir, @"content\assets\notenoughcontent\");
                if (!Directory.Exists(workDir))
                    Directory.CreateDirectory(workDir);
                File.Copy(TextBoxAudiofile.Text, Path.Combine(workDir, audioName), true);

                string filePath = Path.Combine(workDir, audioName + "-" + slideName + "-audio.js");
                using (StreamWriter outputFile = new StreamWriter(filePath))
                {
                    foreach (string line in jsMain)
                    {
                        outputFile.WriteLine(line);
                    }
                }
                filePath = Path.Combine(mainDir, @"content\assets\course.js");
                using (StreamWriter outputFile = new StreamWriter(filePath, true))
                {
                    outputFile.WriteLine(jsInsert);
                }

                LoadMods();
            }
        }

        private void ComboBoxSlides_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxSlides.SelectedItem != null && ComboBoxSlides.SelectedItem.ToString() != "")
                ToggleAudioEnabled(true);
            else
                ToggleAudioEnabled(false);
        }

        private void ButtonDelAudio_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Möchten Sie diesen Eintrag entfernen?", "Löschen?", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    string tempFile = Path.GetTempFileName();
                    string filePath = Path.Combine(mainDir, @"content\assets\course.js");
                    string name = ListBoxMods.SelectedItem.ToString();

                    using (var sr = new StreamReader(filePath))
                    using (var sw = new StreamWriter(tempFile))
                    {
                        string line;

                        while ((line = sr.ReadLine()) != null)
                        {
                            if (!line.Contains(name))
                                sw.WriteLine(line);
                        }
                    }

                    File.Delete(filePath);
                    File.Move(tempFile, filePath);

                    File.Delete(Path.Combine(mainDir, @"content\assets\notenoughcontent\", name + "-audio.js"));
                    LoadMods();
                    //This method always leaves behind the audio file itself. This is intentional, as it may be used by another extension.
                }
                catch (NullReferenceException)
                {
                    MessageBox.Show("Kein Eintrag ausgewählt.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
