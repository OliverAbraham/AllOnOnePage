using System;
using System.Collections.Generic;
using System.Windows;
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
		private string[] _availableBackgrounds;
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

			InitializeComponent();
			ComboboxType_Fill();
			Combobox_Stretch_Fill();
			InitPictureButtons();
			SaveCurrentBackgroundImage();
		}
		#endregion



		#region ------------- Methods -------------------------------------------------------------
        public static void RestoreSelectedBackground(MainWindow parent, Configuration configuration)
		{
            switch (configuration.Background)
			{
				case Configuration.BackgroundType.Image:
					parent.BackgroundImage.Source = CreateBitmapImage(configuration.BackgroundImage);
					parent.BackgroundImage.Stretch = Stretch.UniformToFill;
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
			_availableBackgrounds = new string[]
			{
				"sky.jpg",
				"Autumn.jpg",
				"Paper.jpg",
				"sunset.jpg",
				"sunset2.jpg",
				"abstract-5719419_1280.jpg",
				"fog-6559957_1280.jpg",
				"stars-2367421_1280.jpg",
			};

			I1.Source = CreateBitmapImage(0);
			I2.Source = CreateBitmapImage(1);
			I3.Source = CreateBitmapImage(2);
			I4.Source = CreateBitmapImage(3);
			I5.Source = CreateBitmapImage(4);
			I6.Source = CreateBitmapImage(5);
			I7.Source = CreateBitmapImage(6);
			I8.Source = CreateBitmapImage(7);
		}

		private void Image1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			SetBackgroundImage(0);
		}

		private void Image2_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			SetBackgroundImage(1);
		}

		private void Image3_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			SetBackgroundImage(2);
		}

		private void Image4_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			SetBackgroundImage(3);
		}

		private void Image5_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			SetBackgroundImage(4);
		}

		private void Image6_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			SetBackgroundImage(5);
		}

		private void Image7_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			SetBackgroundImage(6);
		}

		private void Image8_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			SetBackgroundImage(7);
		}

		private void SetBackgroundImage(int i)
		{
			_parent.BackgroundImage.Source = CreateBitmapImage(i);
			_parent.BackgroundImage.Stretch = Stretch.UniformToFill;
			_newBackground = Configuration.BackgroundType.Image;
			_newBackgroundImage = _availableBackgrounds[i];
		}

		private BitmapImage CreateBitmapImage(int i)
		{
			return CreateBitmapImage(_availableBackgrounds[i]);
		}

		private static BitmapImage CreateBitmapImage(string filename)
		{
			return new BitmapImage(new Uri($"pack://application:,,/Pictures/{filename}"));
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
		#endregion
	}
}
