using Abraham.OpenWeatherMap;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;

namespace AllOnOnePage.Plugins
{
    public class ModWeather : ModBase, INotifyPropertyChanged
	{
		#region ------------- Settings ------------------------------------------------------------
		public class MyConfiguration : ModuleSpecificConfig
		{
			public string ApiKey { get; set; }
			public string Decimals { get; set; }
			public string Unit { get; set; }
            public string Latitude { get; set; }
            public string Longitude { get; set; }
        }
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private MyConfiguration _myConfiguration;
        private static OpenWeatherMapConnector _connector;
		private WeatherInfo _forecast;
		private Stopwatch _stopwatch;
		private const int ONE_MINUTE = 60 * 1000;
		private int _updateIntervalInMinutes = 60;
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public override void Init(ModuleConfig config, Grid parent, System.Windows.Threading.Dispatcher dispatcher)
		{
			base.Init(config, parent, dispatcher);
			LoadAssembly("Newtonsoft.Json.dll");
			LoadAssembly("RestSharp.dll");
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
			_myConfiguration           = new MyConfiguration();
            _myConfiguration.ApiKey    = "ENTER-YOUR-API-KEY-HERE you get one free at www.openweathermap.org/api";
			#if DEBUG
			_myConfiguration.ApiKey    = File.ReadAllText(@"C:\Credentials\OpenWeatherMapApiKey.txt");
			#endif
            _myConfiguration.Decimals  = "0";
            _myConfiguration.Unit      = "°C";
            _myConfiguration.Latitude  = "53.8667";
            _myConfiguration.Longitude = "9.8833";
		}

		public override void Save()
		{
			_config.ModulePrivateData = System.Text.Json.JsonSerializer.Serialize(_myConfiguration);
		}

        public override void Recreate()
        {
            //_stopwatch = null;
        }

        public override void UpdateContent()
		{
			ReadNewForecastEveryHour();
			UpdateForecastValues();
		}

		public override (bool,string) Validate()
		{
			UpdateForecastValues();
            return (true, "");
		}

		public override (bool success, string messages) Test()
		{
            return (false, "");
		}

		public override Dictionary<string,string> GetHelp()
		{
            var texts = new Dictionary<string,string>();
            texts.Add("de-DE", 
@"Dieses Modul zeigt die aktuelle Temperatur an.
Die Daten stammen von openweathermap.org.
Sie brauchen einen API Key von dort. 
Gehen Sie hierzu auf www.openweathermap.org/api und registrieren Sie sich kostenlos.
Kopieren Sie dann denn API Key in die Einstellung hier.
Geben sie auch die Koordinaten Ihres Ortes ein (Längen und Breitengrad).
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
			_connector = new OpenWeatherMapConnector()
				.UseApiKey(_myConfiguration.ApiKey)
				.UseLocation(_myConfiguration.Latitude, _myConfiguration.Longitude);
		}

		private void ReadForecast()
		{
			_forecast = _connector.ReadCurrentTemperatureAndForecast();
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
			Value = $"{_forecast.CurrentTemperature}";

			if (!string.IsNullOrWhiteSpace(_myConfiguration.Decimals))
				Value = _forecast.CurrentTemperature.ToString("N" + _myConfiguration.Decimals);

			if (!string.IsNullOrWhiteSpace(_myConfiguration.Unit))
				Value += _myConfiguration.Unit;

			NotifyPropertyChanged(nameof(Value));
		}
        #endregion
    }
}
