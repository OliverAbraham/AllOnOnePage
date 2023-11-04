using Abraham.OpenWeatherMap;
using Newtonsoft.Json;
using PluginBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
        }
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private MyConfiguration                _myConfiguration;
        private static OpenWeatherMapConnector _connector;
        private string                         _connectorMessages;
		private WeatherInfo                    _forecast;
		private Stopwatch                      _stopwatch;
        private const int                      ONE_MINUTE = 60 * 1000;
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
			_myConfiguration                          = new MyConfiguration();
            _myConfiguration.ApiKey                   = "ENTER-YOUR-API-KEY-HERE you get one free at www.openweathermap.org/api";
            _myConfiguration.Decimals                 = "0";
            _myConfiguration.Unit                     = "°C";
            _myConfiguration.Latitude                 = "53.8667";
            _myConfiguration.Longitude                = "9.8833";
            _myConfiguration.UpdateIntervalInMinutes  = "60";
		}

		public override async Task Save()
		{
			_config.ModulePrivateData = System.Text.Json.JsonSerializer.Serialize(_myConfiguration);
		}

        public override void Recreate()
        {
        }

        public override void UpdateContent(ServerDataObjectChange? dataObject)
		{
			ReadNewForecastEveryHour();
			UpdateUI();
		}

		public override async Task<(bool,string)> Validate()
		{
			try
			{
				_connectorMessages = "";
				_connector
					.UseLogger(ValidationLogger)
					.UseApiKey(_myConfiguration.ApiKey)
					.UseLocation(_myConfiguration.Latitude, _myConfiguration.Longitude);
				ReadForecast();
				UpdateUI();
				var success = (_forecast is not null);
				if (!string.IsNullOrWhiteSpace(_connectorMessages))
                    return (success, _connectorMessages);
				return (true, "");
            }
            catch (Exception ex)
			{
                return (false, $"There is a problem. Please check your settings:\n {_connectorMessages}");
            }
		}

        private void ValidationLogger(string message)
        {
            _connectorMessages += message + Environment.NewLine;
        }

        public override async Task<(bool success, string messages)> Test()
		{
            return (false, "");
		}

		public override Dictionary<string,string> GetHelp()
		{
            var texts = new Dictionary<string,string>();
            texts.Add("de-DE", 
@"This module displays the current temperature, fetched from openweathermap.org.
You need an API key from there.
To do this, go to www.openweathermap.org/api and register for free.
Then copy the API Key into the settings here.
Also enter the coordinates of your location (longitude and latitude).
You'll find them on www.google.com/maps. Please refer to my Readme.md on github.
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
			if (_myConfiguration.ApiKey.StartsWith("ENTER-YOUR-API-KEY-HERE") ||
				string.IsNullOrWhiteSpace(_myConfiguration.ApiKey))
			{
				_connectorMessages += "Please enter your API key" + Environment.NewLine;
                return;
			}

			try
			{
				_forecast = _connector.ReadCurrentTemperatureAndForecast();
			}
			catch (Exception ex)
			{
				Value = $"Check your settings. Problem: {ex}";
			}
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
				var intervalMilliseconds = Convert.ToInt32(_myConfiguration.UpdateIntervalInMinutes) * ONE_MINUTE;
				if (_stopwatch.ElapsedMilliseconds > intervalMilliseconds)
				{
					ReadForecast();
					_stopwatch.Restart();
				}
			}
		}

		private void UpdateUI()
		{
			if (_myConfiguration.ApiKey.StartsWith("ENTER-YOUR-API-KEY-HERE"))
			{
				Value = "Enter your API key and check your settings";
				NotifyPropertyChanged(nameof(Value));
                return;
			}

			if (_forecast is null)
			{
				Value = "check your settings";
				NotifyPropertyChanged(nameof(Value));
                return;
			}

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
