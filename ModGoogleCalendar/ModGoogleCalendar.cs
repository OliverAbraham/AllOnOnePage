using PluginBase;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;

namespace AllOnOnePage.Plugins
{
    public class ModGoogleCalendar : ModBase, INotifyPropertyChanged
	{
		#region ------------- Settings ------------------------------------------------------------
		public class MyConfiguration : ModuleSpecificConfig
		{
            public string HeadingColor { get; set; }
            public bool LoadDataFromHomenet { get; set; }
            public string ServerDataObjectName { get; set; }
            public string UpdateIntervalFromServer { get; set; }

            public string UpdateIntervalInMinutes { get; set; }
            public string Entry1 { get; set; }
            public string Entry2 { get; set; }
            public string Entry3 { get; set; }
            public string Entry4 { get; set; }
		}
		#endregion



        #region ------------- Types and constants -------------------------------------------------
        private class Entry
        {
            public ServerDataObject DataObject;
            public DateTime ConvertedDate;
            public string Heading;
            public string Weekday;
            public string Date;
        }

        private class ValueEntriesList
        {
		    public List<ValueEntry> Values { get; set; }

            public ValueEntriesList()
            {
			    Values = new List<ValueEntry>();
            }
        }

        private class ValueEntry
        {
            public string Value { get; set; }
		    public string Text  { get; set; }

            public ValueEntry(string value, string text)
            {
			    Value = value;
                Text  = text;
            }
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
		private MyConfiguration    _myConfiguration;
        private Grid               _grid;
        private List<Entry>        _entries = new();
		private Stopwatch          _stopwatch;
		private const int          ONE_MINUTE = 60 * 1000;
		private int                _updateIntervalInMinutes = 60;
        private string             _connectorMessages;
        private SolidColorBrush    _headingColor;
        private ValueEntriesList   _entry1;
        private ValueEntriesList   _entry2;
        private ValueEntriesList   _entry3;
        private ValueEntriesList   _entry4;
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public override void Init(ModuleConfig config, Grid parent, System.Windows.Threading.Dispatcher dispatcher)
		{
			base.Init(config, parent, dispatcher);
			InitConfiguration();
            CreateGrid();
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
            _myConfiguration.HeadingColor = "#FF90EE90"; //Brushes.LightGreen;
            _myConfiguration.UpdateIntervalInMinutes  = "60";
			_myConfiguration.UpdateIntervalFromServer = "1";
            _myConfiguration.Entry1 = "ABHOLUNG_BIOTONNE";
            _myConfiguration.Entry2 = "ABHOLUNG_GELBERSACK";
            _myConfiguration.Entry3 = "ABHOLUNG_PAPIERTONNE";
            _myConfiguration.Entry4 = "ABHOLUNG_RESTMUELL";
		}

		public override async Task Save()
		{
			_config.ModulePrivateData = System.Text.Json.JsonSerializer.Serialize(_myConfiguration);
		}

        public override void Recreate()
        {
        }

        public override void UpdateLayout()
        {
            Delete();
            CreateBackground();
            SetPositionAndSize();
            CreateGrid();
        }

        public override void UpdateContent(ServerDataObjectChange? dataObject)
        {
            if (dataObject is not null)
                return;
            System.Diagnostics.Debug.WriteLine($"UpdateContent:");
			ReadCalendarEveryHour();
			UpdateUI();
		}

		public override async Task<(bool,string)> Validate()
		{
			try
			{
                _headingColor = (SolidColorBrush)(new BrushConverter().ConvertFrom(_myConfiguration.HeadingColor));
				/*
                if (!_myConfiguration.FetchDataFromServer)
                {
				    _connectorMessages = "";
				    _connector
					    .UseLogger(ValidationLogger)
					    .UseApiKey(_myConfiguration.ApiKey)
					    .UseLocation(_myConfiguration.Latitude, _myConfiguration.Longitude)
                        ;//.UseUnits(_myConfiguration.Units);
                }

				ReadForecast();
				UpdateUI();
				var success = (_forecast is not null);
				if (!string.IsNullOrWhiteSpace(_connectorMessages))
                    return (success, _connectorMessages);
                */
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

        public override bool HitTest(object module, Point mousePosition)
        {
            return PointIsInsideRectangle(mousePosition, GetPositionAndCorrectSize());
        }

		public override async Task<(bool success, string messages)> Test()
		{
            ReadCalendar();
            return (false, "");
		}

		public override Dictionary<string,string> GetHelp()
		{
            var texts = new Dictionary<string,string>();
            texts.Add("de-DE", 
@"This module displays the contents of a cell from an Excel file.
Must be stated:
- the full file name with path.
- the cell (e.g. A1, B13 etc).
A different format can be specified in the 'Format' field.
An example: You have an Excel file in which you record your body weight.
A cell contains the current weight as a number.
To display the weight with the addition of 'kg', enter the following in Format:
{0} kg
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
		#region ------------- Create Grid ---------------------------------------------------------
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

            _headingColor = (SolidColorBrush)(new BrushConverter().ConvertFrom(_myConfiguration.HeadingColor));
			_entry1 = Deserialize(_myConfiguration.Entry1);
			_entry2 = Deserialize(_myConfiguration.Entry2);
			_entry3 = Deserialize(_myConfiguration.Entry3);
			_entry4 = Deserialize(_myConfiguration.Entry4);
		}

        private void CreateGrid()
        {
            _ValueControl.Height = 0;
            SetMargin();

            _grid                     = new Grid();
            _grid.HorizontalAlignment = HorizontalAlignment.Left;
            _grid.VerticalAlignment   = VerticalAlignment.Top;
            _grid.Background          = _backgroundBrush;
            CreatePropertyBinding(nameof(Margin)         , _grid, Grid.MarginProperty);
            CreatePropertyBinding(nameof(Width)          , _grid, Grid.WidthProperty);
            CreatePropertyBinding(nameof(Height)         , _grid, Grid.HeightProperty);
            CreatePropertyBinding(nameof(ValueVisibility), _grid, Grid.VisibilityProperty);
            
            CreateTwoRowGroup(0, nameof(H1), nameof(W1), nameof(D1));
            CreateTwoRowGroup(2, nameof(H2), nameof(W2), nameof(D2));
            CreateTwoRowGroup(4, nameof(H3), nameof(W3), nameof(D3));
            CreateTwoRowGroup(6, nameof(H4), nameof(W4), nameof(D4));
 
            _Parent.Children.Add(_grid);
            Panel.SetZIndex(_ValueControl, -2);
            Panel.SetZIndex(_NameControl, -1);
        }

        protected void CreateTwoRowGroup(int group, string heading, string weekday, string date)
        {
            CreateRow1(group, heading);
            CreateRow2(group, weekday, date);
        }

        private TextBlock CreateRow1(int row, string heading)
        {
            var row1 = new RowDefinition() { Height = new GridLength(25) };
            _grid.RowDefinitions.Add(row1);

            var textBlock = CreateGridTextBlock(_headingColor, 20, heading);
            Grid.SetRow(textBlock, row);
            _grid.Children.Add(textBlock);
            Panel.SetZIndex(textBlock, 2);

            var row2 = new RowDefinition() { Height = new GridLength(45) };
            _grid.RowDefinitions.Add(row2);
            Panel.SetZIndex(textBlock, 2);
            return textBlock;
        }

        private void CreateRow2(int row, string weekday, string date)
        {
            var columnGrid = new Grid();
            Grid.SetRow(columnGrid, row+1);

            var col1 = new ColumnDefinition();
            var col2 = new ColumnDefinition();
            columnGrid.ColumnDefinitions.Add(col1);
            columnGrid.ColumnDefinitions.Add(col2);
            var textBlock1 = CreateGridTextBlock(_textColor, 30, weekday);
            var textBlock2 = CreateGridTextBlock(_textColor, 30, date);
            Panel.SetZIndex(textBlock1, 2);
            Panel.SetZIndex(textBlock2, 2);
            Grid.SetColumn(textBlock2, 0);
            Grid.SetColumn(textBlock2, 1);
            columnGrid.Children.Add(textBlock1);
            columnGrid.Children.Add(textBlock2);

            _grid.Children.Add(columnGrid);
        }

        protected TextBlock CreateGridTextBlock(Brush foreground, int fontSize, string propertyName)
        {
            var control                 = new TextBlock();
            control.Foreground          = foreground;
            control.FontSize            = fontSize;
            control.FontFamily          = new System.Windows.Media.FontFamily("Yu Gothic UI Light");
            control.FontStretch         = FontStretch.FromOpenTypeStretch(3);
            control.HorizontalAlignment = HorizontalAlignment.Left;
            control.VerticalAlignment   = VerticalAlignment.Top;
            control.Margin              = new Thickness(0, 0, 0, 0);
            CreatePropertyBinding(propertyName, control , TextBlock.TextProperty);
            return control;
        }

        protected void SetMargin()
        {
            Margin = new Thickness(_config.X, _config.Y, 0, 0);
            NotifyPropertyChanged(nameof(Margin));
            Width = _config.W;
            NotifyPropertyChanged(nameof(Width));
            Height = _config.H;
            NotifyPropertyChanged(nameof(Height));
        }
        #endregion
		#region ------------- Load data from Google -----------------------------------------------
        private ValueEntriesList Deserialize(string data)
        {
			if (string.IsNullOrWhiteSpace(data))
				return null;

            var parts = data.Split('|', StringSplitOptions.RemoveEmptyEntries);
			var result = new ValueEntriesList();
            foreach (var part in parts)
			{
				var pair = part.Split('=', StringSplitOptions.RemoveEmptyEntries);
				result.Values.Add(new ValueEntry(pair[0], pair[1]));
			}
			return result;
        }

		private void ReadCalendarEveryHour()
        {
            if (_stopwatch == null)
            {
				_stopwatch = Stopwatch.StartNew();
                ReadCalendar();
            }
            else
            {
				if (_stopwatch.ElapsedMilliseconds > _updateIntervalInMinutes * ONE_MINUTE)
				{
					ReadCalendar();
					_stopwatch.Restart();
				}
			}
		}

        private void ReadCalendar()
        {
            if (_myConfiguration.LoadDataFromHomenet)
                LoadCalendarEntriesFromHomenet();
            else
                LoadCalendarEntriesFromGoogle();
        }

        private void LoadCalendarEntriesFromHomenet()
        {
			try
			{
                var basketNames = new List<string>();
                basketNames.Add("ABHOLUNG_BIOTONNE");
                basketNames.Add("ABHOLUNG_GELBERSACK");
                basketNames.Add("ABHOLUNG_PAPIERTONNE");
                basketNames.Add("ABHOLUNG_RESTMUELL");

                _entries.Clear();
                foreach (var name in basketNames)
                {
                    var entry = new Entry();
                    entry.Heading = GetHeadingFromDataobjectName(name) + ":";
                    entry.DataObject = _config.ApplicationData._homenetGetter.TryGet(name);
                    _entries.Add(entry);
                }
			}
			catch (Exception)
			{
			}
        }

        private void LoadCalendarEntriesFromGoogle()
        {
        }
        #endregion
		#region ------------- Update Values -------------------------------------------------------
        //private bool FindAndCopyNewValueToListElement(DataObject dataObject)
        //{
        //    foreach (var day in _WastebasketDays)
        //    {
        //        if (day.DataObject.DOID == dataObject.DOID)
        //        {
        //            day.DataObject.CopyPropertiesFrom(dataObject);
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        private void UpdateUI()
        {
            FormatData();
            SortByDate();
            CopyToWpfProperties(_entries);
        }

        private void FormatData()
        {
            foreach (var entry in _entries)
            {
                if (entry.DataObject == null)
                {
                    entry.DataObject = new ServerDataObject("", "");
                    entry.ConvertedDate = new DateTime(1,1,1);
                    entry.Weekday = "??";
                }
                else
                {
                    entry.Date = entry.DataObject.Value;
                    string dateString = entry.DataObject.Value;
                    if (dateString == "gestern")
                        dateString = DateTime.Today.AddDays(-1).ToString("dd.MM.yyyy");
                    else if (dateString == "heute")
                        dateString = DateTime.Today.ToString("dd.MM.yyyy");
                    else if (dateString == "morgen")
                        dateString = DateTime.Today.AddDays(1).ToString("dd.MM.yyyy");
                    else if (dateString.Length < 10) 
                        dateString = entry.DataObject.Value + DateTime.Today.Year;
                    try
                    {
                        entry.ConvertedDate = DateTime.ParseExact(dateString,"d.M.yyyy",null);
                        if (entry.DataObject.Value == "gestern" ||
                            entry.DataObject.Value == "heute" ||
                            entry.DataObject.Value == "morgen")
                        {
                            entry.Weekday = entry.DataObject.Value;
                            entry.Date = "";
                        }
                        else
                            entry.Weekday = GetWeekdayNameFromDate(entry.ConvertedDate);
                    }
                    catch (Exception)
                    {
                        entry.ConvertedDate = new DateTime(1,1,1);
                        entry.Weekday = "??";
                    }
                }
            }
        }

        private string GetWeekdayNameFromDate(DateTime convertedDate)
        {
            switch (convertedDate.DayOfWeek)
            {
                case DayOfWeek.Sunday   : return "Sun";
                case DayOfWeek.Monday   : return "Mon";
                case DayOfWeek.Tuesday  : return "Tue";
                case DayOfWeek.Wednesday: return "Wed";
                case DayOfWeek.Thursday : return "Thu";
                case DayOfWeek.Friday   : return "Fri";
                case DayOfWeek.Saturday : return "Sat";
                default                 : return "???";
            }
        }

        private string GetHeadingFromDataobjectName(string name)
        {
            switch (name)
            {
                case "ABHOLUNG_BIOTONNE"   : return "Biotonne";
                case "ABHOLUNG_GELBERSACK" : return "Gelber Sack";
                case "ABHOLUNG_PAPIERTONNE": return "Papier";
                case "ABHOLUNG_RESTMUELL"  : return "Restmüll";
                default: return "????????";
            }
        }

        private void SortByDate()
        {
            _entries = (from d in _entries orderby d.ConvertedDate select d).ToList();
        }

        private void CopyToWpfProperties(List<Entry> entries)
        {
            if (entries is null)
                return;

            if (entries.Count >= 1)
            {
                H1 = entries[0].Heading; NotifyPropertyChanged(nameof(H1));
                W1 = entries[0].Weekday; NotifyPropertyChanged(nameof(W1));
                D1 = entries[0].Date;    NotifyPropertyChanged(nameof(D1));
            }

            if (entries.Count >= 2)
            {
                H2 = entries[1].Heading; NotifyPropertyChanged(nameof(H2));
                W2 = entries[1].Weekday; NotifyPropertyChanged(nameof(W2));
                D2 = entries[1].Date;    NotifyPropertyChanged(nameof(D2));
            }

            if (entries.Count >= 3)
            {
                H3 = entries[2].Heading; NotifyPropertyChanged(nameof(H3));
                W3 = entries[2].Weekday; NotifyPropertyChanged(nameof(W3));
                D3 = entries[2].Date;    NotifyPropertyChanged(nameof(D3));
            }

            if (entries.Count >= 4)
            {
                H4 = entries[3].Heading; NotifyPropertyChanged(nameof(H4));
                W4 = entries[3].Weekday; NotifyPropertyChanged(nameof(W4));
                D4 = entries[3].Date;    NotifyPropertyChanged(nameof(D4));
            }
        }
        #endregion
        #endregion
    }
}
