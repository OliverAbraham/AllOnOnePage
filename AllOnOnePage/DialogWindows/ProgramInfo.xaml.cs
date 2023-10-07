using Abraham.AutoUpdater;
using System;
using System.Windows;

namespace AllOnOnePage.DialogWindows
{
	public partial class ProgramInfo : Window
	{
		#region ------------- Fields --------------------------------------------------------------
		private HelpTexts _texts;
		private string _installedVersion;
		private Updater _updater;
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public ProgramInfo(HelpTexts texts, string installedVersion, Updater updater)
		{
			_texts            = texts            ?? throw new ArgumentNullException(nameof(texts           ));
			_installedVersion = installedVersion ?? throw new ArgumentNullException(nameof(installedVersion));
			_updater          = updater;
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			labelVersion.Content = $"Version {_installedVersion}";

			if (_updater != null)
			{
				var newerVersion = _updater.SearchForNewerVersionOnHomepage();
				if (!string.IsNullOrWhiteSpace(newerVersion))
				{
					labelVersionCheck.Content = $"Eine neuere Version ist verfügbar: " + _updater.NewVersion;
					return;
				}
			}

			labelVersionCheck.Content = $"Sie haben die neueste Version";
		}
		#endregion

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
