using Abraham.WPFWindowLayoutManager;
using System.Windows;

namespace AllOnOnePage.DialogWindows
{
	public partial class EditSettings : Window
	{
        #region ------------- Properties ----------------------------------------------------------
        public WindowLayoutManager LayoutManager { get; internal set; }
        #endregion



        #region ------------- Fields --------------------------------------------------------------
        private Configuration _configuration;
		private Configuration _configurationBackup;
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public EditSettings(Configuration configuration)
		{
			_configuration = configuration;
            _configurationBackup = configuration.Clone();
			InitializeComponent();
            _propertyGrid.SelectedObject = _configuration;
		}

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutManager.RestoreSizeAndPosition(this, nameof(EditSettings));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LayoutManager.SaveSizeAndPosition(this, nameof(EditSettings));
        }
		#endregion



		#region ------------- Implementation ------------------------------------------------------
        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Button_PrtgSettings_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            _configuration.CopyPropertiesFrom(_configurationBackup);
            DialogResult = false;
            Close();
        }
        #endregion
    }
}
