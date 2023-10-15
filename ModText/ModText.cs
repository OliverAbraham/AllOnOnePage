using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Controls;

namespace AllOnOnePage.Plugins
{
    public class ModText : ModBase, IPlugin
	{
		#region ------------- Settings ------------------------------------------------------------
		public class MyConfiguration : ModuleSpecificConfig
		{
			public string Text				  { get; set; }
			public string ServerDataObject    { get; set; }
			public string ServerMessages      { get; set; }
			public string ServerFadeOutValues { get; set; }
			public string ServerPlaySound     { get; set; }
			public string FadeOutAfter        { get; set; }
		}
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private MyConfiguration _myConfiguration;
        private Params _serverMessages;
        private Params _serverFadeOutValues;
        private Params _serverPlaySound;
        private Params _fadeOutAfter;
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
					Value = MapTechnicalValueToDisplayValue(dataObject.Value);
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

			_serverMessages      = Deserialize(_myConfiguration.ServerMessages);
			_serverFadeOutValues = Deserialize(_myConfiguration.ServerFadeOutValues);
			_serverPlaySound     = Deserialize(_myConfiguration.ServerPlaySound);
			_fadeOutAfter        = Deserialize(_myConfiguration.FadeOutAfter);
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

        private Params Deserialize(string data)
        {
			if (string.IsNullOrWhiteSpace(data))
				return null;

            var parts = data.Split('|', StringSplitOptions.RemoveEmptyEntries);
			var result = new Params();
            foreach (var part in parts)
			{
				var pair = data.Split('=', StringSplitOptions.RemoveEmptyEntries);
				result.Values.Add(new Param(pair[0], pair[1]));
			}
			return result;
        }

        private string MapTechnicalValueToDisplayValue(string value)
        {
			if (_serverMessages is null)
				return value;

            var message = _serverMessages.Values.Where(x => x.Value == value).FirstOrDefault();
			if (message is not null)
                return message.Text;
			else
				return value;
        }
        #endregion
	}

    internal class Params
    {
		public List<Param> Values { get; set; }

        public Params()
        {
			Values = new List<Param>();
        }
    }

    public class Param
    {
        public string Value { get; set; }
		public string Text  { get; set; }

        public Param(string value, string text)
        {
			Value = value;
            Text  = text;
        }
    }
}
