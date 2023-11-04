using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AllOnOnePage.Plugins
{
	class ModDate : ModBase, INotifyPropertyChanged
    {
		#region ------------- Settings ------------------------------------------------------------
		public class MyConfiguration : ModuleSpecificConfig
		{
			public string Format { get; set; }
		}
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private MyConfiguration _myConfiguration;
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public override void Init(ModuleConfig config, Grid parent, System.Windows.Threading.Dispatcher dispatcher)
		{
			base.Init(config, parent, dispatcher);
			InitConfiguration();
		}
		#endregion



		#region ------------- Methods -------------------------------------------------------------
		public override ModuleSpecificConfig GetModuleSpecificConfig()
		{
			return _myConfiguration;
		}

		public override void CreateSeedData()
		{
            _myConfiguration.Format = "ddd dd. MMMM";
		}

		public override async Task Save()
		{
			_config.ModulePrivateData = System.Text.Json.JsonSerializer.Serialize(_myConfiguration);
		}

        public override void Time()
        {
            Value = DateTime.Now.ToString(_myConfiguration.Format);
            NotifyPropertyChanged(nameof(Value));
            ValueVisibility = Visibility.Visible; 
            NotifyPropertyChanged(nameof(ValueVisibility));
        }

		public override Dictionary<string,string> GetHelp()
		{
            var texts = new Dictionary<string,string>();
            texts.Add("de-DE", 
@"Dieses Modul zeigt das aktuelle Datum.
In der Einstellung 'Format' kann die Darstellung geändert werden:
ddd dd. MMMM - Tag als Name, Tag als Zahl und Monat ausgeschrieben
dd.MM.YYYY - Tag, Monat und Jahr
");
            return texts;
		}
        #endregion



		#region ------------- Implementation ------------------------------------------------------
		private void InitConfiguration()
		{
			try
			{
				_myConfiguration = System.Text.Json.JsonSerializer.Deserialize<MyConfiguration>(_config.ModulePrivateData);
			}
            catch (Exception)
			{
			}

			if (_myConfiguration == null)
			{
				_myConfiguration = new MyConfiguration();
				_myConfiguration.Format = "ddd dd. MMMM";
			}
		}
        #endregion
    }
}
