using Abraham.OpenWeatherMap;
using HomenetBase;
using Newtonsoft.Json;
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
            public string UpdateIntervalInMinutes { get; set; }
            public string UpdateIntervalFromServer { get; set; }
            public bool FetchDataFromServer { get; set; }
            public string ServerDataObjectName { get; set; }
        }
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private MyConfiguration _myConfiguration;
        private static OpenWeatherMapConnector _connector;
		private WeatherInfo _forecast;
		private Stopwatch _stopwatch;
		private const int ONE_MINUTE = 60 * 1000;
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
            _myConfiguration.UpdateIntervalInMinutes = "60";
			_myConfiguration.UpdateIntervalFromServer = "1";
			_myConfiguration.FetchDataFromServer = false;
			_myConfiguration.ServerDataObjectName = "WEATHER_FORECAST";
		}

		public override void Save()
		{
			_config.ModulePrivateData = System.Text.Json.JsonSerializer.Serialize(_myConfiguration);
		}

        public override void Recreate()
        {
            //_stopwatch = null;
        }

        public override void UpdateContent(Abraham.HomenetBase.Models.DataObject? dataObject)
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
			if (_myConfiguration.FetchDataFromServer)
			{
			}
			else
			{
				_connector = new OpenWeatherMapConnector()
					.UseApiKey(_myConfiguration.ApiKey)
					.UseLocation(_myConfiguration.Latitude, _myConfiguration.Longitude);
			}
		}

		private void ReadForecast()
        {
            if (_myConfiguration.FetchDataFromServer)
                _forecast = ReadCurrentTemperatureFromHomeAutomationServer();
			else
                _forecast = _connector.ReadCurrentTemperatureAndForecast();
        }

        private WeatherInfo ReadCurrentTemperatureFromHomeAutomationServer()
        {
			try
			{
				var dataObject = _config.ApplicationData._homenetGetter.TryGet(_myConfiguration.ServerDataObjectName);
				if (dataObject is null || dataObject.Value is null)
					return DefaultWeatherInfo();

				var json = TextEncoder.UnescapeJsonCharacters(dataObject.Value);
				var forecast = JsonConvert.DeserializeObject<List<Forecast>>(json);
				if (forecast is null)
					return DefaultWeatherInfo();

				dataObject = _config.ApplicationData._homenetGetter.TryGet("AUSSENTEMPERATUR");
				var value = dataObject.Value;
				if (value.Contains(' '))
					value = value.Substring(0, value.IndexOf(' '));
				if (value.Contains('°'))
					value = value.Substring(0, value.IndexOf('°'));
				if (value.Contains(','))
					value = value.Replace(',', '.');
				
				// convert value into double with invariant culture
				var temperature = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);

				return new WeatherInfo(temperature, "°", forecast, new WeatherModel(), new WeatherModel());
			}
			catch (Exception)
			{
				return DefaultWeatherInfo();
			}
        }

        private static WeatherInfo DefaultWeatherInfo()
        {
            return new WeatherInfo(0, "?", new List<Forecast>(), new WeatherModel(), new WeatherModel());
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
				var intervalMilliseconds = (_myConfiguration.FetchDataFromServer) 
					? Convert.ToInt32(_myConfiguration.UpdateIntervalFromServer) * ONE_MINUTE
					: Convert.ToInt32(_myConfiguration.UpdateIntervalInMinutes) * ONE_MINUTE;

				if (_stopwatch.ElapsedMilliseconds > intervalMilliseconds)
				{
					ReadForecast();
					_stopwatch.Restart();
				}
			}
		}

		private void UpdateForecastValues()
		{
			if (_forecast is null)
                return;
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
