using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AllOnOnePage
{
    public partial class EditBackground : Window
	{
		#region ------------- Fields --------------------------------------------------------------
		private MainWindow _parent;
		private Configuration _configuration;
		private ImageSource _savedBackgroundImage;
		private List<string> _availableBackgrounds;
		private Configuration.BackgroundType _newBackground;
		private string _newBackgroundImage;
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public EditBackground(MainWindow parent, Configuration configuration)
		{
			if (parent        == null) throw new ArgumentNullException(nameof(parent));
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));
			_parent = parent;
			_configuration = configuration;

			_availableBackgrounds = new List<string>();
			InitializeComponent();
			ComboboxType_Fill();
			Combobox_Stretch_Fill();
			WaitAndThenCallMethod(wait_time_seconds:1, action:InitPictureButtons);
			SaveCurrentBackgroundImage();
		}
		#endregion



		#region ------------- Methods -------------------------------------------------------------
        public static void RestoreSelectedBackground(MainWindow parent, Configuration configuration)
		{
            switch (configuration.Background)
			{
				case Configuration.BackgroundType.Image:
					try
					{
						parent.BackgroundImage.Source = CreateBitmapImageFromFile(configuration.BackgroundImage);
						parent.BackgroundImage.Stretch = Stretch.UniformToFill;
					}
					catch(Exception)
					{
						parent.BackgroundImage.Source = CreateBitmapImage("default.jpg");
						parent.BackgroundImage.Stretch = Stretch.UniformToFill;
					}
					break;
			}
			if (configuration.FullScreenDisplay)
				parent.WindowStyle = WindowStyle.None;
		}
		#endregion



		#region ------------- Implementation ------------------------------------------------------
		private void ComboboxType_Fill()
		{
			var _items = new List<string>();
			_items.Add("Bild");
			_items.Add("Volltonfarbe");
			comboboxType.ItemsSource = _items;
			comboboxType.SelectedItem = _items[0];
			comboboxType.IsEnabled = false;
		}

		private void Combobox_Stretch_Fill()
		{
			var _items = new List<string>();
			_items.Add("Ausfüllen");
			_items.Add("Anpassen");
			_items.Add("Dehnen");
			_items.Add("Kachel");
			_items.Add("Zentriert");
			_items.Add("Strecken");
			comboboxStretch.ItemsSource = _items;
			comboboxStretch.SelectedItem = _items[0];
			comboboxStretch.IsEnabled = false;
		}

		private void InitPictureButtons()
        {
			CollectAvailableImages();
            DisplayFoundImagesInStackPanel();
        }

        private void CollectAvailableImages()
        {
            var userDocumentsImageFolder = GetDocumentsDirectoryImageFolder();
            if (Directory.Exists(userDocumentsImageFolder))
            {
                var imageFilenames = Directory.GetFiles(userDocumentsImageFolder, "*");
                _availableBackgrounds.AddRange(imageFilenames);
            }

            var programDirectoryImageFolder = GetProgramDirectoryImageFolder();
            if (Directory.Exists(programDirectoryImageFolder))
            {
                var imageFilenames = Directory.GetFiles(programDirectoryImageFolder, "*");
                _availableBackgrounds.AddRange(imageFilenames);
            }
        }

        private void DisplayFoundImagesInStackPanel()
        {
            foreach (var background in _availableBackgrounds)
            {
                AddImageToStackPanel(background);
            }
        }

        private void AddImageToStackPanel(string filename)
        {
            imagesStackPanel.Children.Add(CreateImageControl(filename));
        }

        private static Image CreateImageControl(string filename)
        {
			var image = CreateBitmapImageFromFile(filename);
            return new Image() { Width = 70, Height = 70, Margin = new Thickness(0, 0, 10, 0), Source = image, Tag = filename };
        }

        private string GetDocumentsDirectoryImageFolder()
        {
            return "backgrounds";
        }

        private string GetProgramDirectoryImageFolder()
        {
			var process = Process.GetCurrentProcess();
			var fullPath = process.MainModule.FileName;
			var appDirectory = Path.GetDirectoryName(fullPath);
			appDirectory = Path.Combine(appDirectory, "backgrounds");
			return appDirectory;
        }

        private void OnStackPanelSelectImage(object sender, MouseButtonEventArgs e)
		{
			var filename = (string)((Image)e.Source).Tag;
			SetBackgroundImage(filename);
		}

		private void SetBackgroundImage(int i)
		{
			_parent.BackgroundImage.Source = CreateBitmapImage(i);
			_parent.BackgroundImage.Stretch = Stretch.UniformToFill;
			_newBackground = Configuration.BackgroundType.Image;
			_newBackgroundImage = _availableBackgrounds[i];
		}

		private void SetBackgroundImage(string filename)
		{
			_parent.BackgroundImage.Source = CreateBitmapImageFromFile(filename);
			_parent.BackgroundImage.Stretch = Stretch.UniformToFill;
			_newBackground = Configuration.BackgroundType.Image;
			_newBackgroundImage = filename;
		}

		private BitmapImage CreateBitmapImage(int i)
		{
			return CreateBitmapImage(_availableBackgrounds[i]);
		}

		private static BitmapImage CreateBitmapImage(string filename)
		{
			return new BitmapImage(new Uri($"pack://application:,,/Pictures/{filename}"));
		}

		private static BitmapImage CreateBitmapImageFromFile(string filename)
		{
			return new BitmapImage(new Uri(filename));
		}

		private void SaveCurrentBackgroundImage()
		{
			_savedBackgroundImage = _parent.BackgroundImage.Source;
		}

		private void RestoreSavedBackgroundImage()
		{
			if (_parent.BackgroundImage.Source != _savedBackgroundImage)
				_parent.BackgroundImage.Source = _savedBackgroundImage;
		}

		private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Pictures|*jpg;*png";
            dlg.Title = "Hintergrundbild auswählen";

            if (dlg.ShowDialog() == true)
            {
				var filename = dlg.FileName;
				AddImageToStackPanel(filename);
				SetBackgroundImage(filename);
			}
		}

		private void ButtonSave_Click(object sender, RoutedEventArgs e)
		{
			_configuration.Background = _newBackground;
			_configuration.BackgroundImage = _newBackgroundImage;
			DialogResult = true;
			Close();
		}

		private void ButtonCancel_Click(object sender, RoutedEventArgs e)
		{
			RestoreSavedBackgroundImage();
			Close();
		}

        private Timer CreateAndStartTimer(ElapsedEventHandler timerProc, int interval_in_seconds, bool repeatedly = true)
        {
            var timer = new Timer();
            timer.Interval = interval_in_seconds * 1000;
            timer.Elapsed += timerProc;
            timer.AutoReset = repeatedly;
            timer.Start();
            return timer;
        }

        private Timer WaitAndThenCallMethod(int wait_time_seconds, Action action)
        {
			return CreateAndStartTimer(
                delegate(object sender, ElapsedEventArgs e)
                {
                    try
                    {
						Dispatcher.Invoke(() =>
						{
                            try
							{
                                action();
							}
							catch (Exception ex)
							{
                                Debug.WriteLine(ex.ToString());
							}
						});
                    }
                    catch (Exception) { }
                }, wait_time_seconds, repeatedly:false);
        }
		#endregion
	}
}
