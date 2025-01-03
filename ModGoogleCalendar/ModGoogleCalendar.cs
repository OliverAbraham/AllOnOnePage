using Abraham.GoogleCalendar;
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
using System.IO;

namespace AllOnOnePage.Plugins
{
    public class ModGoogleCalendar : ModBase, INotifyPropertyChanged
	{
		#region ------------- Settings ------------------------------------------------------------
		public class MyConfiguration : ModuleSpecificConfig
		{
            public string HeadingColor            { get; set; }
            public bool   LoadDataFromHomenet     { get; set; }
            public string HomenetDataObjectName   { get; set; }

            public bool   LoadDataFromGoogle      { get; set; }
            public string GoogleCredentials       { get; set; }
            public string UpdateIntervalInMinutes { get; set; }
            public int    DaysToReadInAdvance     { get; set; }
            public string DateFormatting          { get; set; }

            public string Entry1                  { get; set; }
            public string Entry2                  { get; set; }
            public string Entry3                  { get; set; }
            public string Entry4                  { get; set; }
            public string WeekdayNames            { get; set; }
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
            public string Keyword;

            public Entry()
            {
            }

            public Entry(DateTime? date, string heading, string keyword, string weekday, string formattedDate)
            {
                ConvertedDate = date ?? new DateTime(1, 1, 1);
                Heading = heading;
                Keyword = keyword;
                Weekday = weekday;
                Date = formattedDate;
            }
        }

        private class Filter
        {
            public string Keyword { get; set; }
		    public string Heading  { get; set; }
		    public int    ResultsCount { get; set; }
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
		private MyConfiguration                  _myConfiguration;
        private Grid                             _grid;
        private List<Entry>                      _entries = new();
		private Stopwatch                        _stopwatch;
		private const int                        _oneMinute = 60 * 1000;
		private int                              _updateIntervalInMinutes = 60;
        private string                           _connectorMessages;
        private SolidColorBrush                  _headingColor;
        private List<Filter>                     _filters;
        private List<GoogleCalendarReader.Event> _calendarEvents;
        private string[]                         _weekdayNames;
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public override void Init(ModuleConfig config, Grid parent, System.Windows.Threading.Dispatcher dispatcher)
		{
			base.Init(config, parent, dispatcher);
			base.LoadAssembly("Abraham.GoogleCalendar.dll");
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
			_myConfiguration                          = new MyConfiguration();
            _myConfiguration.HeadingColor             = "#FF90EE90"; //Brushes.LightGreen;
            _myConfiguration.LoadDataFromHomenet      = false;
            _myConfiguration.LoadDataFromGoogle       = true;
			_myConfiguration.GoogleCredentials        = "(Enter your Google credentials)";
            _myConfiguration.UpdateIntervalInMinutes  = "60";
            _myConfiguration.DaysToReadInAdvance      = 14;
            _myConfiguration.DateFormatting           = "dd.MM";
            _myConfiguration.Entry1                   = "ABHOLUNG_BIOTONNE";
            _myConfiguration.Entry2                   = "ABHOLUNG_GELBERSACK";
            _myConfiguration.Entry3                   = "ABHOLUNG_PAPIERTONNE";
            _myConfiguration.Entry4                   = "ABHOLUNG_RESTMUELL";
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
            _headingColor = (SolidColorBrush)(new BrushConverter().ConvertFrom(_myConfiguration.HeadingColor));
            Delete();
            CreateBackground();
            SetPositionAndSize();
            CreateGrid();
        }

        public override void UpdateContent(ServerDataObjectChange? dataObject)
        {
			// we're not interested in MQTT or Home Automation messages
			if (dataObject is not null)
				return;
            System.Diagnostics.Debug.WriteLine($"UpdateContent:");
			ReadCalendarEveryHour();
		}

		public override async Task<(bool,string)> Validate()
		{
			try
			{
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
            CopyFilterEntriesToFilterList();
            ReadCalendar();
            FilterEntries();
            UpdateUI();
            
            var formattedResult = 
                "Events that were read from Google Calendar:\n\n" +
                string.Join('\n', _calendarEvents) + 
                "\n\n" + 
                "Found these events by given filters:\n" +
                string.Join('\n', _filters.Select(x => $"FilterKeyword: {x.Keyword} Results: {x.ResultsCount} events"));

            return (false, formattedResult);
		}

		public override Dictionary<string,string> GetHelp()
		{
            var texts = new Dictionary<string,string>();
            texts.Add("de-DE", 
@"This module displays up to four events from your Google calendar, sorted by date
You need to give a filter for each entry. The filter is a keyword that is contained in the event's title.
This module was planned to list recurring events like garbage collection dates.
In the four filter settings, specify the filter word, a separator | and the heading for the event.
For example: 'Garbage can|Garbage:'
The module will read the events from your Google calendar and search for the given filter 'Garbage can' in the subjects.
The Word 'Garbage:' will be the heading.
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
		#region ------------- General -------------------------------------------------------------
		private void InitConfiguration()
        {
            DeserializeConfigurationSet();
            ConvertColors();
            CopyFilterEntriesToFilterList();
            DeserializeWeekdayNames();
        }

        private void DeserializeConfigurationSet()
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

        private void ConvertColors()
        {
            _headingColor = (SolidColorBrush)(new BrushConverter().ConvertFrom(_myConfiguration.HeadingColor));
        }

        private void CopyFilterEntriesToFilterList()
        {
            _filters = new();
            CopyEntry(ref _filters, _myConfiguration.Entry1);
            CopyEntry(ref _filters, _myConfiguration.Entry2);
            CopyEntry(ref _filters, _myConfiguration.Entry3);
            CopyEntry(ref _filters, _myConfiguration.Entry4);
        }

        private void CopyEntry(ref List<Filter> filters, string entry)
        {
            Filter filter = Deserialize(entry);
            if (filter is not null) 
                _filters.Add(filter);
        }

        private Filter Deserialize(string data)
        {
			if (string.IsNullOrWhiteSpace(data))
				return null;

            var parts = data.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (parts is null || parts.Length < 1)
                return null;

			var result = new Filter();
			result.Keyword = parts[0];
            result.Heading =(parts.Length >= 2) ? parts[1] : parts[0];
			return result;
        }

        private void DeserializeWeekdayNames()
        {
            if (!string.IsNullOrEmpty(_myConfiguration.WeekdayNames))
                _weekdayNames = _myConfiguration.WeekdayNames.Split('|', StringSplitOptions.RemoveEmptyEntries);
            
            if (_weekdayNames is null || _weekdayNames.GetLength(0) < (3+7))
            {
                _weekdayNames = new string[3] { "Yesterday", "Today", "Tomorrow" };
                _myConfiguration.WeekdayNames = "Yesterday|Today|Tomorrow|Sun|Mon|Tue|Wed|Thu|Fri|Sat";
            }
        }
        #endregion
		#region ------------- Create Grid ---------------------------------------------------------
        public override void Delete()
		{
            if (_grid is not null)
            {
                _Parent.Children.Remove(_grid);
                _grid.Children.Clear();
                _grid.RowDefinitions.Clear();
                _grid.ColumnDefinitions.Clear();
                _grid = null;
            }
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

        protected void CreateTwoRowGroup(int rowIndex, string row1TextPropertyName, string row2Col1TextPropertyName, string row2Col2TextPropertyName)
        {
            var row1Height   = _config.FontSize * 25 / 45;
            var row2Height   = _config.FontSize;
            var row1FontSize = _config.FontSize * 20 / 45;
            var row2FontSize = _config.FontSize * 30 / 45;

            CreateRow1(rowIndex  , row1Height, row1FontSize, _headingColor, row1TextPropertyName);
            CreateRow2(rowIndex+1, row2Height, row2FontSize, _textColor   , row2Col1TextPropertyName, row2Col2TextPropertyName);
        }

        private void CreateRow1(int rowIndex, int height, int fontSize, SolidColorBrush textColor, string textPropertyName)
        {
            var rowDef = new RowDefinition() { Height = new GridLength(height) };
            _grid.RowDefinitions.Add(rowDef);

            var row = new Grid();
            Grid.SetRow(row, rowIndex);

            CreateCell(row, 0, textPropertyName, fontSize, textColor);

            _grid.Children.Add(row);
        }

        private void CreateRow2(int rowIndex, int height, int fontsize, SolidColorBrush textColor, string col1PropertyName, string col2PropertyName)
        {
            var rowDef = new RowDefinition() { Height = new GridLength(height) };
            _grid.RowDefinitions.Add(rowDef);

            var row = new Grid();
            Grid.SetRow(row, rowIndex);

            CreateCell(row, 0, col1PropertyName, fontsize, textColor);
            CreateCell(row, 1, col2PropertyName, fontsize, textColor);

            _grid.Children.Add(row);
        }

        private void CreateCell(Grid row, int columnIndex, string textPropertyName, int fontSize, SolidColorBrush textColor)
        {
            var col2 = new ColumnDefinition();
            row.ColumnDefinitions.Add(col2);
            var textBlock = CreateTextBlock(textColor, fontSize, textPropertyName);
            Panel.SetZIndex(textBlock, 2);
            Grid.SetColumn(textBlock, columnIndex);
            row.Children.Add(textBlock);
        }

        protected TextBlock CreateTextBlock(Brush foregroundColor, int fontSize, string propertyName)
        {
            var control                 = new TextBlock();
            control.Foreground          = foregroundColor;
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
		#region ------------- Load data -----------------------------------------------------------
		private void ReadCalendarEveryHour()
        {
            if (_stopwatch == null)
            {
				_stopwatch = Stopwatch.StartNew();
                ReadCalendar();
                FilterEntries();
			    UpdateUI();
            }
            else
            {
				if (_stopwatch.ElapsedMilliseconds > _updateIntervalInMinutes * _oneMinute)
				{
					ReadCalendar();
                    FilterEntries();
			        UpdateUI();
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
        #endregion
		#region ------------- Load data from Google -----------------------------------------------
        private void LoadCalendarEntriesFromGoogle()
        {
            try
            {
                if (_myConfiguration.DaysToReadInAdvance < 1)
                    _myConfiguration.DaysToReadInAdvance = 1;

                // If we cannot find the credentials file, try to find it in the user directory (our subdirectory)
                if (!File.Exists(_myConfiguration.GoogleCredentials))
                {
                    var tryInUserDirectory = Path.Combine(_config.ApplicationData.DataDirectory, _myConfiguration.GoogleCredentials);
                    if (File.Exists(tryInUserDirectory))
                        _myConfiguration.GoogleCredentials = tryInUserDirectory;
                }

                var reader = new GoogleCalendarReader()
                    .UseCredentialsFile(_myConfiguration.GoogleCredentials)
                    .UseApplicationName("AllOnOnePage");

                _calendarEvents = reader.ReadEventsByStartTime(
                    DateTime.Now, 
                    DateTime.Now.AddDays(_myConfiguration.DaysToReadInAdvance));
            }
            catch (Exception ex)
            {
            }
        }

        private void FilterEntries()
        {
            if (_calendarEvents is null)
                return;
			try
			{
                _entries.Clear();
                foreach(var filter in _filters)
                {
                    var @event = _calendarEvents.FirstOrDefault(e => e.Summary.Contains(filter.Keyword));
                    if (@event is not null)
                    {
                        filter.ResultsCount = _calendarEvents.Where(e => e.Summary.Contains(filter.Keyword)).Count();
                        var weekday = @event.When?.DayOfWeek.ToString() ?? "???";
                        var formattedDate = @event.When?.ToString(_myConfiguration.DateFormatting) ?? "???";
                        _entries.Add(new Entry(@event.When, filter.Heading, filter.Keyword, weekday, formattedDate));
                    }
                }
            }
			catch (Exception ex)
			{
                _connectorMessages += ex.Message;
			}
        }
        #endregion
		#region ------------- Load data from Homenet ----------------------------------------------
        private void LoadCalendarEntriesFromHomenet()
        {
			try
			{
                _entries.Clear();
                foreach (var filter in _filters)
                {
                    var entry = LoadEntry(filter);
                    _entries.Add(entry);
                }
                FormatEventDates();
            }
			catch (Exception)
			{
			}
        }

        private Entry LoadEntry(Filter filter)
        {
            var entry = new Entry();
            entry.Heading = filter.Heading;
            try
            {
                entry.DataObject = _config.ApplicationData._homenetGetter.TryGet(filter.Keyword);
            }
            catch (Exception)
            {
                entry.DataObject = new ServerDataObject(filter.Keyword, "???", new DateTimeOffset());
            }
            return entry;
        }

        private void FormatEventDates()
        {
            foreach (var entry in _entries)
            {
                FormatEventDate(entry);
            }
        }

        /// <summary>
        /// Replace near dates to yesterday/today/tomorrow
        /// </summary>
        private void FormatEventDate(Entry entry)
        {
            if (entry.DataObject == null)
            {
                entry.DataObject = new ServerDataObject("", "", new DateTimeOffset());
                entry.ConvertedDate = new DateTime(1, 1, 1);
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
                    entry.ConvertedDate = DateTime.ParseExact(dateString, "d.M.yyyy", null);
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
                    entry.ConvertedDate = new DateTime(1, 1, 1);
                    entry.Weekday = "??";
                }
            }
        }

        private string GetWeekdayNameFromDate(DateTime convertedDate)
        {
            switch (convertedDate.DayOfWeek)
            {
                case DayOfWeek.Sunday   : return _weekdayNames[3]; // "Sun";
                case DayOfWeek.Monday   : return _weekdayNames[4]; // "Mon";
                case DayOfWeek.Tuesday  : return _weekdayNames[5]; // "Tue";
                case DayOfWeek.Wednesday: return _weekdayNames[6]; // "Wed";
                case DayOfWeek.Thursday : return _weekdayNames[7]; // "Thu";
                case DayOfWeek.Friday   : return _weekdayNames[8]; // "Fri";
                case DayOfWeek.Saturday : return _weekdayNames[9]; // "Sat";
                default                 : return "???";
            }
        }
        #endregion
		#region ------------- Update UI -----------------------------------------------------------
        private void UpdateUI()
        {
            SortByDate();
            ReplaceNearEventByTodayTomorrow(_entries);
            CopyToWpfProperties(_entries);
        }

        private void ReplaceNearEventByTodayTomorrow(List<Entry> entries)
        {
            foreach (var entry in entries)
            {
                if (entry.ConvertedDate == DateTime.Today.AddDays(-1))
                {
                    entry.Weekday = _weekdayNames[0]; // "Yesterday";
                    entry.Date    = "";
                }
                else if (entry.ConvertedDate == DateTime.Today)
                {
                    entry.Weekday = _weekdayNames[1]; // "Today";
                    entry.Date    = "";
                }
                else if (entry.ConvertedDate == DateTime.Today.AddDays(1))
                {
                    entry.Weekday = _weekdayNames[2]; // "Tomorrow";
                    entry.Date    = "";
                }
                else
                {
                    var daysAhead = entry.ConvertedDate - DateTime.Today;
                    if (daysAhead.TotalDays < 6)
                        entry.Date    = "";
                    entry.Weekday = GetWeekdayNameFromDate(entry.ConvertedDate);
                }
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
