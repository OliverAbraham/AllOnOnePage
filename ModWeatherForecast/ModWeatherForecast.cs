using Abraham.OpenWeatherMap;
using HomenetBase;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AllOnOnePage.Plugins
{
    public class ModWeatherForecast : ModBase, INotifyPropertyChanged
	{
		#region ------------- Settings ------------------------------------------------------------
		public class MyConfiguration : ModuleSpecificConfig
		{
			public string ApiKey { get; set; }
			public string Decimals { get; set; }
			public string Unit { get; set; }
            public string Latitude { get; set; }
            public string Longitude { get; set; }
			//public TimeSpan TimeMorning { get; set; } = new TimeSpan(6,0,0);
			//public TimeSpan TimeLunch   { get; set; } = new TimeSpan(12,0,0);
			//public TimeSpan TimeEvening { get; set; } = new TimeSpan(18,0,0);
			//public TimeSpan TimeNight   { get; set; } = new TimeSpan(23,0,0);
            public string UpdateIntervalInMinutes { get; set; }
            public string UpdateIntervalFromServer { get; set; }
            public bool FetchDataFromServer { get; set; }
            public string ServerDataObjectName { get; set; }
		}
		#endregion



        #region ------------- WPF Properties ------------------------------------------------------
        public Visibility   Visibility              { get; set; } = Visibility.Hidden;
		public Thickness    Margin                  { get; set; }
        public string       H1                      { get; set; } = "-";
        public string       H2                      { get; set; } = "-";
        public string       H3                      { get; set; } = "-";
        public string       H4                      { get; set; } = "-";
        public string       W1                      { get; set; } = "-";
        public string       W2                      { get; set; } = "-";
        public string       W3                      { get; set; } = "-";
        public string       W4                      { get; set; } = "-";
        public string       D1                      { get; set; } = "-";
        public string       D2                      { get; set; } = "-";
        public string       D3                      { get; set; } = "-";
        public string       D4                      { get; set; } = "-";
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private MyConfiguration                _myConfiguration;
        private static OpenWeatherMapConnector _connector;
		private WeatherInfo                    _forecast;
		private Stopwatch                      _stopwatch;
		private const int                      ONE_MINUTE = 60 * 1000;
		private int                            _updateIntervalInMinutes = 60;
        private Grid                           _grid;
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public override void Init(ModuleConfig config, Grid parent, System.Windows.Threading.Dispatcher dispatcher)
		{
			base.Init(config, parent, dispatcher);
			LoadAssembly("Newtonsoft.Json.dll");
			LoadAssembly("RestSharp.dll");
            CreateGrid();
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

        public override void UpdateLayout()
        {
            Delete();
            CreateBackground();
            SetPositionAndSize();
            CreateGrid();
        }

        public override void UpdateContent(Abraham.HomenetBase.Models.DataObject? dataObject)
		{
			ReadNewForecastEveryHour();
			UpdateForecastValues();
		}

        public override bool HitTest(object module, Point mousePosition)
        {
            return PointIsInsideRectangle(mousePosition, GetPositionAndCorrectSize());
        }

		public override (bool,string) Validate()
		{
            CreateGrid();
			UpdateForecastValues();
            return (true, "");
		}

		public override (bool success, string messages) Test()
		{
            ReadForecast();
            return (false, "");
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
        
        public override void SetPosition(double left, double top)
        {
            base.SetPosition(left,top);
            SetMargin();
        }
		
        public override void SetSize(double width, double height)
        { 
            base.SetSize(width, height);
            SetMargin();
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
                _forecast = ReadCurrentForecastFromHomeAutomationServer();
			else
			    _forecast = _connector.ReadCurrentTemperatureAndForecast();
		}

        private WeatherInfo ReadCurrentForecastFromHomeAutomationServer()
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

				return new WeatherInfo(0, "?", forecast, new WeatherModel(), new WeatherModel());
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

        private void CreateGrid()
        {
            _ValueControl.Height = 0;
            SetMargin();

            _grid                     = new Grid();
            _grid.HorizontalAlignment = HorizontalAlignment.Left;
            _grid.VerticalAlignment   = VerticalAlignment.Top;
            _grid.Background          = Brushes.Transparent;
            CreatePropertyBinding(nameof(Margin)         , _grid, Grid.MarginProperty);
            CreatePropertyBinding(nameof(Width)          , _grid, Grid.WidthProperty);
            CreatePropertyBinding(nameof(Height)         , _grid, Grid.HeightProperty);
            CreatePropertyBinding(nameof(ValueVisibility), _grid, Grid.VisibilityProperty);
            
            int fontSize0 = (int)(_config.FontSize*0.4);   
            int fontSize1 = (int)(_config.FontSize*1.0);   
            int fontSize2 = (int)(_config.FontSize*1.0);   

            int height0   = (int)(_config.H*0.17);       
            int height1   = (int)(_config.H*0.25);       
            int height2   = (int)(_config.FontSize*1.2);

            CreateRow(0, fontSize0, height0, "H");
            CreateRow(1, fontSize1, height1, "W");
            CreateRow(2, fontSize2, height2, "D");
 
            _canvas.Children.Add(_grid);
            Panel.SetZIndex(_ValueControl, -2);
            Panel.SetZIndex(_NameControl, -1);
        }

        private void CreateRow(int row, int fontSize, int height, string propertyNamePrefix)
        {
            var row1 = new RowDefinition() { Height = new GridLength(height) };
            _grid.RowDefinitions.Add(row1);

            var col1 = new ColumnDefinition();
            var col2 = new ColumnDefinition();
            var col3 = new ColumnDefinition();
            var col4 = new ColumnDefinition();
            var columnGrid = new Grid();
            Grid.SetRow(columnGrid, row);
            columnGrid.ColumnDefinitions.Add(col1);
            columnGrid.ColumnDefinitions.Add(col2);
            columnGrid.ColumnDefinitions.Add(col3);
            columnGrid.ColumnDefinitions.Add(col4);
            _grid.Children.Add(columnGrid);

            CreateText(columnGrid, row, 0, fontSize, height, propertyNamePrefix + "1");
            CreateText(columnGrid, row, 1, fontSize, height, propertyNamePrefix + "2");
            CreateText(columnGrid, row, 2, fontSize, height, propertyNamePrefix + "3");
            CreateText(columnGrid, row, 3, fontSize, height, propertyNamePrefix + "4");
        }

        private void CreateText(Grid columnGrid, int row, int column, int fontSize, int height, string propertyName)
        {
            var background = Brushes.Transparent;
            switch (row)
            {
                case 0: background = Brushes.LightBlue; break;
                case 1: background = Brushes.LightCoral; break;
                case 2: background = Brushes.LightGreen; break;
            }
            var textBlock = CreateGridTextBlock(_textColor, fontSize, height, propertyName, background);
            Grid.SetRow(textBlock, row);
            Grid.SetColumn(textBlock, column);
            columnGrid.Children.Add(textBlock);
            Panel.SetZIndex(textBlock, 2);
        }

        protected TextBlock CreateGridTextBlock(Brush foreground, int fontSize, int height, string propertyName, SolidColorBrush background)
        {
            var control                 = new TextBlock();
            control.Foreground          = foreground;
            //control.Background          = background;
            control.FontSize            = fontSize;
            control.FontFamily          = new System.Windows.Media.FontFamily("Yu Gothic UI Light");
            control.FontStretch         = FontStretch.FromOpenTypeStretch(3);
            control.HorizontalAlignment = HorizontalAlignment.Center;
            control.VerticalAlignment   = VerticalAlignment.Top;
            control.Margin              = new Thickness(0, 0, 0, 0);
            control.Height              = height;
            CreatePropertyBinding(propertyName, control , TextBlock.TextProperty);
            return control;
        }

        protected void SetMargin()
        {
            Margin = new Thickness(_config.X, _config.Y+20, 0, 0);
            NotifyPropertyChanged(nameof(Margin));
            Width = _config.W;
            NotifyPropertyChanged(nameof(Width));
            Height = _config.H;
            NotifyPropertyChanged(nameof(Height));
        }

        private void UpdateForecastValues()
        {
            H1 = "morgens";                                      
            H2 = "mittags";                                      
            H3 = "abends" ;                                      
            H4 = "nachts" ;                           

            if (_forecast is null)
                return;
            
            var forecast1 = _forecast.SmallForecast[0];
            var forecast2 = _forecast.SmallForecast[1];
            var forecast3 = _forecast.SmallForecast[2];
			var forecast4 = _forecast.SmallForecast[3];
            W1 = new OpenWeatherMapConnector().GetUniCodeSymbolForWeatherIcon(forecast1.Icon);
            W2 = new OpenWeatherMapConnector().GetUniCodeSymbolForWeatherIcon(forecast2.Icon);
            W3 = new OpenWeatherMapConnector().GetUniCodeSymbolForWeatherIcon(forecast3.Icon);
            W4 = new OpenWeatherMapConnector().GetUniCodeSymbolForWeatherIcon(forecast4.Icon);
            D1 = $"{forecast1.Temp}°";                       
            D2 = $"{forecast2.Temp}°";                       
            D3 = $"{forecast3.Temp}°";                       
            D4 = $"{forecast4.Temp}°";                       

            NotifyPropertyChanged(nameof(H1)); 
            NotifyPropertyChanged(nameof(H2)); 
            NotifyPropertyChanged(nameof(H3)); 
            NotifyPropertyChanged(nameof(H4)); 
            NotifyPropertyChanged(nameof(W1)); 
            NotifyPropertyChanged(nameof(W2)); 
            NotifyPropertyChanged(nameof(W3)); 
            NotifyPropertyChanged(nameof(W4)); 
            NotifyPropertyChanged(nameof(D1)); 
            NotifyPropertyChanged(nameof(D2)); 
            NotifyPropertyChanged(nameof(D3)); 
            NotifyPropertyChanged(nameof(D4)); 
        }
		#endregion
	}
}
