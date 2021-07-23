using ColorPickerWPF;
using Svg;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using ColorPickerWPF.Code;
using System.Text.RegularExpressions;
using System.IO.Compression;

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
        string certPath = null;
        string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string titleStr = null;

        public MainWindow()
        {
            InitializeComponent();
            this.Title += Assembly.GetExecutingAssembly().GetName().Version.Major.ToString() + "." + Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString();
            ToggleSlideEnabled(false);
            ToggleLoadEnabled(false);
        }

        private void HandleIndexFile(string file)
        {
            mainDir = new FileInfo(file).Directory.FullName;
            string manifestPath = Path.Combine(mainDir, "content\\manifest.json");
            string[] manifest = File.ReadAllLines(manifestPath);
            List<int> titleLine = new List<int>();
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
            PopulateCss();
            PopulateCert();

            ToggleLoadEnabled(true);
            LabelStatus.Content = "Projekt \"" + titleStr + "\" erfolgreich geladen.";
        }

        private void HandleZipFile(string file)
        {
            Directory.CreateDirectory(tempPath);
            string workDir = Path.Combine(tempPath, @"workdir\");
            ZipFile.ExtractToDirectory(file, workDir);
            ButtonSaveZip.Visibility = Visibility.Visible;
            HandleIndexFile(Path.Combine(workDir, "index.html"));
        }

        private void PopulateAudio(List<int> titleLine, string[] manifest)
        {
            SlidesDict = new Dictionary<string, string>();

            foreach (int lineNo in titleLine)
            {
                string trimmedTitle = manifest[lineNo].Trim(); //Remove Whitespace
                trimmedTitle = trimmedTitle.Remove(0, 10); //Remove "title":
                trimmedTitle = trimmedTitle.Remove(trimmedTitle.Length - 2); //Remove stray punctuation at the end

                string trimmedId = null;

                for (int i = 0; i < 20; i++)
                {
                    if (manifest[lineNo - i].Contains("\"id\""))
                    {
                        trimmedId = manifest[lineNo - i].Trim();
                        break;
                    }
                }
                if (trimmedId == null)
                {
                    MessageBox.Show("Fehler beim Laden des Projekts. Fehlercode: PopulateAudio-1", "Kritischer Fehler", MessageBoxButton.OK, MessageBoxImage.Stop);
                    Environment.Exit(63);
                }

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

            LoadDataToImage(logoPath, ImageLogo);
        }

        private void PopulateCss()
        {
            TextBoxColorCode.Text = ParseCssBackground();
            TextBoxWidth.Text = ParseCssWidth();
        }

        private void PopulateCert()
        {
            string dirPath = Path.Combine(mainDir, @"content\assets\");
            string[] svgFiles = Directory.GetFiles(dirPath, "*.svg");
            foreach (string file in svgFiles)
            {
                if (Path.GetFileName(file).StartsWith("standard"))
                {
                    certPath = file;
                    break;
                }
            }

            if (certPath != null)
            {
                TabItemCert.IsEnabled = true;
                LoadDataToImage(certPath, ImageCertificate);
            }
        }

        private void LoadDataToImage(string imgPath, System.Windows.Controls.Image imgCtrl)
        {
            bool isSvg = imgPath.EndsWith(".svg");

            BitmapImage img = new BitmapImage();
            img.BeginInit();
            //Set Image
            if (isSvg)
            {
                var svgDoc = SvgDocument.Open(imgPath);
                MemoryStream ms = new MemoryStream();
                svgDoc.Draw().Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                img.StreamSource = ms;
            }
            else
                img.UriSource = new Uri(imgPath);
            img.EndInit();
            imgCtrl.Source = img;
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
                            MessageBox.Show("Unbekannte Modifikationen gefunden. Wurden sie mit einer neueren Version von NotEnoughContent erstellt?", "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning);
                            break;
                    }
                }
            }
            ListBoxMods.ItemsSource = mods;
        }

        private void ToggleSlideEnabled(bool enable)
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
        private void ToggleLoadEnabled(bool enable)
        {
            TextBoxColorCode.IsEnabled = enable;
            ButtonColorPicker.IsEnabled = enable;
            ButtonOpenSplash.IsEnabled = enable;
            ButtonSplashInsert.IsEnabled = enable;
            TextBoxSplashfile.IsEnabled = enable;
            TextBoxWidth.IsEnabled = enable;
            TextBoxCert.IsEnabled = enable;
            ButtonCertInsert.IsEnabled = enable;
            ButtonOpenCert.IsEnabled = enable;
        }

        private void HandleAudioFile(string filePath)
        {
            TextBoxAudiofile.Text = filePath;
        }

        private void HandleSplashFile(string filePath)
        {
            TextBoxSplashfile.Text = filePath;
        }

        private void HandleCertFile(string filePath)
        {
            TextBoxCert.Text = filePath;
        }

        private string ParseCssBackground()
        {
            string cssPath = Path.Combine(mainDir, @"resources\style\loadscreen.css");

            foreach (string line in File.ReadAllLines(cssPath))
            {
                if (line.Contains("background-color"))
                {
                    string retstr = line.Trim();
                    retstr = retstr.Remove(retstr.Length - 1).Remove(0, 19);
                    return retstr;
                }
            }

            return null;
        }

        private string ParseCssWidth()
        {
            string cssPath = Path.Combine(mainDir, @"resources\style\loadscreen.css");
            int i = 0;

            foreach (string line in File.ReadAllLines(cssPath))
            {
                if (line.Contains("width") && i == 0)
                    i++;
                else if (line.Contains("width") && i == 1)
                {
                    string retstr = line.Trim();
                    retstr = Regex.Replace(retstr, @"\s+", "");
                    retstr = retstr.Remove(retstr.Length - 1).Remove(0, 6);
                    return retstr;
                }
            }

            return null;
        }

        private void WriteCss(Func<string> ParseCss, string searchExpr, System.Windows.Controls.TextBox textBox)
        {
            string tempFile = Path.GetTempFileName();
            string cssPath = Path.Combine(mainDir, @"resources\style\loadscreen.css");

            using (var sr = new StreamReader(cssPath))
            using (var sw = new StreamWriter(tempFile))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (!line.Contains(searchExpr))
                        sw.WriteLine(line);
                    else
                        sw.WriteLine(line.Replace(ParseCss(), textBox.Text));
                }
            }

            File.Delete(cssPath);
            File.Move(tempFile, cssPath);
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
                else if (files[0].EndsWith(".zip"))
                {
                    HandleZipFile(files[0]);
                }
            }
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
                ToggleSlideEnabled(true);
            else
                ToggleSlideEnabled(false);
        }

        private void ButtonDelAudio_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Möchten Sie diesen Eintrag entfernen?", "Löschen?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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

        private void ButtonOpenSplash_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG Files (*.png)|*.png|SVG Files (*.svg)|*.svg|GIF Files (*.gif)|*.gif|JPEG Files (*.jpg, *.jpeg, *.jpe, *.jfif)|*.jpg; *.jpeg; *.jpe; *.jfif";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                HandleSplashFile(dlg.FileName);
            }
        }

        private void TextBoxSplashfile_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files[0].EndsWith(".png") || files[0].EndsWith(".svg") || files[0].EndsWith(".gif") || files[0].EndsWith(".jpg") || files[0].EndsWith(".jpeg") || files[0].EndsWith(".jpe") || files[0].EndsWith(".jfif"))
                {
                    // Pass file to handler.
                    HandleSplashFile(files[0]);
                }
            }
        }

        private void ButtonSplashInsert_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(TextBoxSplashfile.Text) || !(TextBoxSplashfile.Text.EndsWith(".png") || TextBoxSplashfile.Text.EndsWith(".svg") || TextBoxSplashfile.Text.EndsWith(".gif") || TextBoxSplashfile.Text.EndsWith(".jpg") || TextBoxSplashfile.Text.EndsWith(".jpeg") || TextBoxSplashfile.Text.EndsWith(".jpe") || TextBoxSplashfile.Text.EndsWith(".jfif")))
            {
                MessageBox.Show("Bitte geben Sie eine gültige Bilddatei an!");
            }
            else
            {
                string imgName = "loadscreen-" + Path.GetFileName(TextBoxSplashfile.Text);

                string imgPath = Path.Combine(mainDir, @"resources\style\images\", imgName);

                File.Copy(TextBoxSplashfile.Text, imgPath, true);

                string tempFile = Path.GetTempFileName();
                string filePath = Path.Combine(mainDir, @"resources\style\controls\loadscreen.ctl.html");

                using (var sr = new StreamReader(filePath))
                using (var sw = new StreamWriter(tempFile))
                {
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (!line.Contains("resources/style/images/"))
                            sw.WriteLine(line);
                        else
                            sw.WriteLine("<img src=\"resources/style/images/" + imgName + "\"/>");
                    }
                }

                File.Delete(filePath);
                File.Move(tempFile, filePath);

                LoadDataToImage(imgPath, ImageLogo);
            }
        }

        private void ButtonColorPicker_Click(object sender, RoutedEventArgs e)
        {
            Color color;
            bool ok = ColorPickerWindow.ShowDialog(out color, ColorPickerDialogOptions.SimpleView);

            TextBoxColorCode.Text = color.ToString().Remove(1,2);
        }

        private void TextBoxColorCode_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Regex rgx = new Regex(@"^#(.)\1\1$|^#[a-fA-F0-9]{6}$");
            SolidColorBrush black = new SolidColorBrush(Colors.Black);
            SolidColorBrush red = new SolidColorBrush(Colors.Red);
            if (rgx.IsMatch(TextBoxColorCode.Text))
            {
                WriteCss(ParseCssBackground, "background-color", TextBoxColorCode);
                LabelColorStatus.Content = "Gespeichert!";
                TextBoxColorCode.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(TextBoxColorCode.Text));
                LabelColorStatus.Foreground = black; 
            }
            else
            {
                LabelColorStatus.Content = "nicht OK!";
                LabelColorStatus.Foreground = red;
            }
        }

        private void TextBoxWidth_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Regex rgx = new Regex(@"\d+(cm|mm|in|px|pt|pc|em|ex|ch|rem|vw|vh|vmin|vmax|%)");
            SolidColorBrush black = new SolidColorBrush(Colors.Black);
            SolidColorBrush red = new SolidColorBrush(Colors.Red);

            if (rgx.IsMatch(TextBoxWidth.Text))
            {
                WriteCss(ParseCssWidth, "width", TextBoxWidth);
                LabelWidthStatus.Content = "Gespeichert!";
                LabelWidthStatus.Foreground = black;
            }
            else
            {
                LabelWidthStatus.Content = "nicht OK!";
                LabelWidthStatus.Foreground = red;
            }
        }

        private void ButtonOpenCert_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".svg";
            dlg.Filter = "SVG Files (*.svg)|*.svg";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                HandleCertFile(dlg.FileName);
            }
        }

        private void TextBoxCert_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files[0].EndsWith(".svg"))
                {
                    // Pass file to handler.
                    HandleCertFile(files[0]);
                }
            }
        }

        private void ButtonCertInsert_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(TextBoxCert.Text) || !TextBoxSplashfile.Text.EndsWith(".svg"))
                MessageBox.Show("Bitte geben Sie eine gültige SVG Datei an!");
            else
            {
                File.Copy(TextBoxCert.Text, certPath, true);
                LoadDataToImage(certPath, ImageCertificate);
            }
        }

        private void ButtonSaveZip_Click(object sender, RoutedEventArgs e)
        {
            // Configure save file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = titleStr; // Default file name
            dlg.DefaultExt = ".zip"; // Default file extension
            dlg.Filter = "ZIP Files (.zip)|*.zip"; // Filter files by extension

            // Show save file dialog box
            bool? result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                ZipFile.CreateFromDirectory(Path.Combine(tempPath, "workdir"), Path.Combine(tempPath, titleStr + ".zip"));
                File.Move(Path.Combine(tempPath, titleStr + ".zip"), dlg.FileName);
                MessageBox.Show("Speichervorgang abgeschlossen.");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }
}
