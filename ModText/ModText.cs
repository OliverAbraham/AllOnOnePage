using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using System.Windows.Media;
using System.Windows.Markup.Localizer;

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
			public string ServerWarningValues { get; set; }
			public string ServerPlaySound     { get; set; }
			public string FadeOutAfter        { get; set; }
			public string WarningTextColor    { get; set; }
		}
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private MyConfiguration _myConfiguration;
        private Params _serverMessages;
        private Params _serverFadeOutValues;
        private Params _serverWarningValues;
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

        public override void UpdateLayout()
        {
            base.UpdateLayout();
            UpdateContent(null);
        }

        public override void UpdateContent(Abraham.HomenetBase.Models.DataObject? dataObject)
        {
			if (TheHomeAutomationServerShouldBeUsed())
            {
                if (WeHaveReceivedAValueChangeMessage(dataObject))
                {
                    if (ThisChangeMessageIsNotForUs(dataObject))
                        return;
                    Value = dataObject.Value;
                }
                else
                {
                    Value = ReadValueDirectlyFromHomeAutomationServer();
                }
                Value = MapTechnicalValueToDisplayValue(Value);
                NotifyPropertyChanged(nameof(Value));

                SetWarningColorIfNecessary();
                SetValueControlVisible();
                return;
            }

            Value = _myConfiguration.Text;
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
			_serverWarningValues = Deserialize(_myConfiguration.ServerWarningValues);
			_serverPlaySound     = Deserialize(_myConfiguration.ServerPlaySound);
			_fadeOutAfter        = Deserialize(_myConfiguration.FadeOutAfter);
		}
        #endregion



		#region ------------- Home automation server ----------------------------------------------
		private bool TheHomeAutomationServerShouldBeUsed()
        {
            return !string.IsNullOrWhiteSpace(_myConfiguration.ServerDataObject);
        }

        private string ReadValueDirectlyFromHomeAutomationServer()
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

		private bool WeHaveReceivedAValueChangeMessage(Abraham.HomenetBase.Models.DataObject dataObject)
        {
            return dataObject is not null;
        }

		private bool ThisChangeMessageIsNotForUs(Abraham.HomenetBase.Models.DataObject dataObject)
        {
            return dataObject is not null && dataObject.Name != _myConfiguration.ServerDataObject;
        }

        private Params Deserialize(string data)
        {
			if (string.IsNullOrWhiteSpace(data))
				return null;

            var parts = data.Split('|', StringSplitOptions.RemoveEmptyEntries);
			var result = new Params();
            foreach (var part in parts)
			{
				var pair = part.Split('=', StringSplitOptions.RemoveEmptyEntries);
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

        private void SetWarningColorIfNecessary()
        {
            if (WarningTextColorIsSet() && WarningValuesAreSet())
            {
                var weHaveAWarning = _serverWarningValues.Values.Any(x => x.Text == Value);
                var textColor = (weHaveAWarning) ? _myConfiguration.WarningTextColor : _config.TextColor;
                base._ValueControl.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom(textColor));
            }
        }

        private bool WarningTextColorIsSet()
        {
            return _myConfiguration.WarningTextColor is not null && !string.IsNullOrWhiteSpace(_myConfiguration.WarningTextColor);
        }

        private bool WarningValuesAreSet()
        {
            return _serverWarningValues is not null && _serverWarningValues.Values.Any();
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
