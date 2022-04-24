using Abraham.Internet;
using Abraham.Weather;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Controls;

namespace AllOnOnePage.Plugins
{
	public class ModWeather : ModBase, INotifyPropertyChanged
	{
		#region ------------- Settings ------------------------------------------------------------
		public class MyConfiguration : ModuleSpecificConfig
		{
			public string URL    { get; set; }
			public string Format { get; set; }
		}
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private MyConfiguration _myConfiguration;
        private static WeatherConverter _logic;
		private List<Forecast> _forecast;
		private Stopwatch _stopwatch;
		private const int ONE_MINUTE = 60 * 1000;
		private int _updateIntervalInMinutes = 60;
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public override void Init(ModuleConfig config, Grid parent, System.Windows.Threading.Dispatcher dispatcher)
		{
			base.Init(config, parent, dispatcher);
			InitConfiguration();
			InitWeatherReader();
		}
		#endregion



		#region ------------- Methods -------------------------------------------------------------
		public override ModuleSpecificConfig GetModuleSpecificConfig()
		{
			return _myConfiguration;
		}

		public override void CreateSeedData()
		{
			_myConfiguration        = new MyConfiguration();
            _myConfiguration.URL    = @"https://www.wetter.de/deutschland/wetter-berlin-18228265.html?q=berlin";
            _myConfiguration.Format = "{0}°";
		}

		public override void Save()
		{
			_config.ModulePrivateData = System.Text.Json.JsonSerializer.Serialize(_myConfiguration);
		}

        public override void Recreate()
        {
            _stopwatch = null;
        }

        public override void UpdateContent()
		{
			ReadNewForecastEveryHour();
			UpdateForecastValues();
		}

		public override Dictionary<string,string> GetHelp()
		{
            var texts = new Dictionary<string,string>();
            texts.Add("de-DE", 
@"Dieses Modul zeigt die aktuelle Temperatur von Wetter.de an.
hier gehen Sie auf die Wetter.de Homepage und suchen nach dem gewünschten Ort.
Kopieren Sie dann die Adresszeile des Browsers komplett in die Einstellung hier.
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

		private void InitWeatherReader()
		{
			LoadAssembly("HtmlAgilityPack.dll");
			_logic = new WeatherConverter();
		}

		private void ReadForecast()
		{
            var client = new HttpClient();
            string pageContent = client.DownloadFromUrl(_myConfiguration.URL);
			_forecast = _logic.ExtractWeatherDataFromPage(pageContent);
		}

		private void ReadNewForecastEveryHour()
		{
			if (_stopwatch == null)
			{
				_stopwatch = Stopwatch.StartNew();
				ReadForecast();
			}
			else
			{
				if (_stopwatch.ElapsedMilliseconds > _updateIntervalInMinutes * ONE_MINUTE)
				{
					ReadForecast();
					_stopwatch.Restart();
				}
			}
		}

		private void UpdateForecastValues()
		{
			double currentTemperature = _logic.FindTemperatureForTime(_forecast, DateTime.Now);
			Value = $"{currentTemperature}°";

			if (!string.IsNullOrWhiteSpace(_myConfiguration.Format) &&
				_myConfiguration.Format.Contains("{0}"))
			{
				Value = _myConfiguration.Format.Replace("{0}", Value);
			}
			NotifyPropertyChanged(nameof(Value));
		}
        #endregion
    }
}
