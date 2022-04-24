using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace AllOnOnePage.Plugins
{
    class ModTime : ModBase, INotifyPropertyChanged
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
			_myConfiguration = new MyConfiguration();
            _myConfiguration.Format = @"hh\:mm";
		}

		public override void Save()
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
@"Dieses Modul zeigt die aktuelle Uhrzeit an.
In der Einstellung 'Format' kann die Darstellung eingegeben werden:
HH\:mm\:ss Stunde,Minute,Sekunde im 24-Stunden-Format
hh\:mm Stunde,Minute,Sekunde im 12-Stunden-Format
d kurzes Datumsformat 
D langes Datumsformat 
t kurzes Zeitformat   
T langes Zeitformat   
f komplett kurz       
F komplett lang       
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
				CreateSeedData();
		}
        #endregion
    }
}
