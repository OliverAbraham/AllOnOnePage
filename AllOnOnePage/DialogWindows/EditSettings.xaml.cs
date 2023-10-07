using System.Windows;

namespace AllOnOnePage.DialogWindows
{
	public partial class EditSettings : Window
	{
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
