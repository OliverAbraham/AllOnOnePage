using System;
using System.Windows;
using System.ComponentModel;
using AllOnOnePage.Plugins;
using System.Globalization;

namespace AllOnOnePage.DialogWindows
{
	public partial class EditModuleSettings : Window, INotifyPropertyChanged
    {
        #region ------------- Properties ----------------------------------------------------------
		#endregion



		#region ------------- Control Properties --------------------------------------------------
		#endregion



		#region ------------- INotifyPropertyChanged ----------------------------------------------
		public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                _propertyChanged += value;
            }
            remove
            {
                _propertyChanged -= value;
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler Handler = _propertyChanged; // avoid race condition
            if (Handler != null)
                Handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion



        #region ------------- Fields --------------------------------------------------------------
        private IPlugin _plugin;
		private HelpTexts _texts;

		private ModuleConfig _config => _plugin.GetModuleConfig();
		private ModuleConfig _configBackup;
        [NonSerialized]
        private PropertyChangedEventHandler _propertyChanged;
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public EditModuleSettings(IPlugin plugin, HelpTexts texts)
		{
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _texts  = texts  ?? throw new ArgumentNullException(nameof(texts ));

            _configBackup = _plugin.GetModuleConfig().Clone();
			InitializeComponent();
            DataContext = this;
            _propertyGrid.SelectedObject = _plugin.GetModuleSpecificConfig();
		}
        #endregion



        #region ------------- Implementation ------------------------------------------------------

		private void Window_Closed(object sender, EventArgs e)
		{
            _plugin?.CleanupModuleSpecificConfig();
        }

		private void Button_Test_Click(object sender, RoutedEventArgs e)
		{
            this.Cursor = System.Windows.Input.Cursors.Wait;
            (bool success, string messages) = _plugin.Validate();
            this.Cursor = System.Windows.Input.Cursors.Arrow;

            if (!success)
			{
                var wnd = new MessageBoxWindow(this);
                wnd.Title = "Problem";
                wnd.ContentBox.Text = messages;
			    wnd.ShowDialog();
			}
			else
			{
                (success, messages) = _plugin.Test();
                var wnd = new MessageBoxWindow(this);
                wnd.Title = "Test";
                wnd.ContentBox.Text = messages;
			    wnd.ShowDialog();
			}
		}

        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            (bool success, string messages) = _plugin.Validate();
            if (!success)
			{
                var wnd = new MessageBoxWindow(this);
                wnd.Title = "Problem";
                wnd.ContentBox.Text = messages;
			    wnd.ShowDialog();
                return;
			}

            _plugin?.Save();
            DialogResult = true;
            Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            _config.CopyPropertiesFrom(_configBackup);
            DialogResult = false;
            Close();
        }

        private void _propertyGrid_SelectedPropertyItemChanged(object sender, RoutedPropertyChangedEventArgs<Xceed.Wpf.Toolkit.PropertyGrid.PropertyItemBase> e)
        {
            _plugin.Validate();
            _plugin.UpdateLayout();
        }

        private void Button_Info_Click(object sender, RoutedEventArgs e)
		{
            var wnd = new MessageBoxWindow(this);
            wnd.Title = _texts[HelpTexts.ID.MODULE_HELP];
            
            var moduleHelp = _plugin.GetHelp();
            var cultureInfo = CultureInfo.CurrentCulture;
            if (moduleHelp.ContainsKey(cultureInfo.Name))
                wnd.ContentBox.Text = moduleHelp[cultureInfo.Name];
            else if (moduleHelp.ContainsKey("en-EN"))
                wnd.ContentBox.Text = moduleHelp["en-EN"];
            else
                wnd.ContentBox.Text = "Sorry, the plugin has no help text.";

			wnd.ShowDialog();
		}
		#endregion
	}
}
