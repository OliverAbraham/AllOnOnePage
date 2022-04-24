using System;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using AllOnOnePage.Plugins;
using System.Collections.Generic;
using Abraham.PluginManager;

namespace AllOnOnePage.DialogWindows
{
	public partial class NewModule : Window, INotifyPropertyChanged
    {
        #region ------------- Properties ----------------------------------------------------------
        public RuntimeModule Module { get; set; }
		public List<Processor> Processors { get; internal set; }
		#endregion



		#region ------------- Control Properties --------------------------------------------------
		public string ModuleName { get { return Module.Config.ModuleName; } set { Module.Config.ModuleName = value; } }
        public string ModuleType { get { return Module.Config.TileType;   } set { Module.Config.TileType   = value; } }
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		[NonSerialized]
        private PropertyChangedEventHandler _PropertyChanged;
		private ApplicationDirectories _applicationDirectories;
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public NewModule(ApplicationDirectories applicationDirectories)
		{
            var config = new ModuleConfig()
			{
                FontSize = 30,
                ApplicationDirectories = applicationDirectories,
			};
            Module = new RuntimeModule(config);
			InitializeComponent();
            DataContext = this;
		}
        #endregion



        #region ------------- Implementation ------------------------------------------------------
        private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			InitControls();
		}

		private void InitControls()
		{
			InitName();
			InitType();
		}

		private void InitName()
		{
			ModuleName = "neues Modul 1";
			NotifyPropertyChanged(nameof(ModuleName));
		}

		private void InitType()
		{
			var types = Processors.Select(x=> x.Type.Name).ToArray();
			comboboxType.ItemsSource = types;
			comboboxType.SelectedItem = types[0];
			NotifyPropertyChanged(nameof(ModuleType));
		}

		private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        #endregion



        #region ------------- INotifyPropertyChanged ----------------------------------------------
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                _PropertyChanged += value;
            }
            remove
            {
                _PropertyChanged -= value;
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler Handler = _PropertyChanged; // avoid race condition
            if (Handler != null)
                Handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}

