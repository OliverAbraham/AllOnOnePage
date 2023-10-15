using HomenetBase;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Windows.Controls;

namespace AllOnOnePage.Plugins
{
	public class ModText : ModBase, IPlugin
	{
		#region ------------- Settings ------------------------------------------------------------
		public class MyConfiguration : ModuleSpecificConfig
		{
			public string Text				{ get; set; }
			public string ServerDataObject  { get; set; }
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
            _myConfiguration.Text = "Mein Text";
		}

		public override void Save()
		{
			_config.ModulePrivateData = System.Text.Json.JsonSerializer.Serialize(_myConfiguration);
		}

		public override (bool,string) Validate()
		{
			UpdateContent(null);
            return (true, "");
		}

		public override (bool success, string messages) Test()
		{
            return (false, "");
		}

        public override void UpdateContent(HomenetBase.DataObject? dataObject)
        {
			if (string.IsNullOrWhiteSpace(_myConfiguration.ServerDataObject))
			{
	            Value = _myConfiguration.Text;
			}
			else
			{
				if (dataObject is not null)
				{
					if (dataObject.Name != _myConfiguration.ServerDataObject)
						return;
					Value = dataObject.Value;
				}
				else
					Value = ReadValueFromHomeAutomationServer();
			}

            NotifyPropertyChanged(nameof(Value));
            SetValueControlVisible();
        }

		public override Dictionary<string,string> GetHelp()
		{
            var texts = new Dictionary<string,string>();
            texts.Add("de-DE", 
@"Dieses Modul zeigt einfachen Text an.
In den allgemeinen Einstellungen im Feld 'Text' kann der Text eingegeben werden.");
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

        private string ReadValueFromHomeAutomationServer()
        {
			try
			{
				var dataObject = _config.ApplicationData._homenetConnector.TryGet(_myConfiguration.ServerDataObject);
				return dataObject?.Value ?? "???";
			}
			catch (Exception)
			{
				return "???";
			}
        }
        #endregion
	}
}
