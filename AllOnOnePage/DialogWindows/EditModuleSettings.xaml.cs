using System;
using System.Windows;
using System.ComponentModel;
using AllOnOnePage.Plugins;
using System.Globalization;
using Abraham.WPFWindowLayoutManager;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace AllOnOnePage.DialogWindows
{
	public partial class EditModuleSettings : Window, INotifyPropertyChanged
    {
        #region ------------- Fields --------------------------------------------------------------
        private IPlugin _plugin;
		private HelpTexts _texts;
        private Dispatcher _dispatcher;
        private WindowLayoutManager _layoutManager;

        private ModuleConfig _config => _plugin.GetModuleConfig();
		private ModuleConfig _configBackup;
        [NonSerialized]
        private PropertyChangedEventHandler _propertyChanged;
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



        #region ------------- Init ----------------------------------------------------------------
        public EditModuleSettings(IPlugin plugin, HelpTexts texts, Dispatcher dispatcher, WindowLayoutManager layoutManager)
		{
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _texts  = texts  ?? throw new ArgumentNullException(nameof(texts ));
            _dispatcher = dispatcher;
            _layoutManager = layoutManager;

            _configBackup = _plugin.GetModuleConfig().Clone();
			InitializeComponent();
            DataContext = this;
            _propertyGrid.SelectedObject = _plugin.GetModuleSpecificConfig();
		}

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _layoutManager.RestoreSizeAndPosition(this, nameof(EditModuleSettings));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _layoutManager.SaveSizeAndPosition(this, nameof(EditModuleSettings));
        }
        #endregion



        #region ------------- Implementation ------------------------------------------------------

		private void Window_Closed(object sender, EventArgs e)
		{
            _plugin?.CleanupModuleSpecificConfig();
        }

		private void Button_Test_Click(object sender, RoutedEventArgs e)
        {
            // we need to call the modules in an async context, to enable them to do async tasks
            // some Nuget packages like Abraham.PrtgClient require this
            _dispatcher.BeginInvoke( async () => await TestModuleAsync() );
        }

        private async Task TestModuleAsync()
        {
            this.Cursor = System.Windows.Input.Cursors.Wait;
            (bool success, string messages) = await _plugin.Validate();
            this.Cursor = System.Windows.Input.Cursors.Arrow;

            if (!success)
            {
                ShowMessageBox("Problem", messages);
            }
            else
            {
                (success, messages) = await _plugin.Test();

                if (!string.IsNullOrWhiteSpace(messages))
                    ShowMessageBox("Test", messages);
            }
        }

        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            _dispatcher.BeginInvoke( async () => await SaveModuleAsync() );
        }

        private async Task SaveModuleAsync()
        {
            (bool success, string messages) = await _plugin.Validate();
            if (!success)
			{
                ShowMessageBox("Problem", messages);
                return;
			}

            try
            {
                await _plugin?.Save();
            }
            catch (Exception ex)
			{
                ShowMessageBox("Problem", ex.ToString());
                return;
			}

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
            var title = _texts[HelpTexts.ID.MODULE_HELP];
            string messages;

            var moduleHelp = _plugin.GetHelp();
            var cultureInfo = CultureInfo.CurrentCulture;
            if (moduleHelp.ContainsKey(cultureInfo.Name))
                messages = moduleHelp[cultureInfo.Name];
            else if (moduleHelp.ContainsKey("en-EN"))
                messages = moduleHelp["en-EN"];
            else
                messages = "Sorry, the plugin has no help text.";

            ShowMessageBox(title, messages);
		}

        private void ShowMessageBox(string title, string messages)
        {
            var wnd = new MessageBoxWindow(this);
            wnd.ShowDialogEx(this, title, messages, centered:true, showCheckbox:false);
        }
        #endregion
    }
}
