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
    public class ModGoogleCalendar2 : ModBase, INotifyPropertyChanged
	{
		#region ------------- Settings ------------------------------------------------------------
		public class MyConfiguration : ModuleSpecificConfig
		{
            public string HeadingColor            { get; set; }
            public string GoogleCredentials       { get; set; }
            public string UpdateIntervalInMinutes { get; set; }
            public int    DaysToReadInAdvance     { get; set; }
            public string DateFormatting          { get; set; }
            public int    NumberOfEntries         { get; set; }

            public string WeekdayNames            { get; set; }
            public string SubjectBlacklist        { get; set; }
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
        private string[]                         _subjectBlacklist;
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
			_myConfiguration.GoogleCredentials        = "(Enter your Google credentials)";
            _myConfiguration.UpdateIntervalInMinutes  = "60";
            _myConfiguration.NumberOfEntries          = 4;
            _myConfiguration.DateFormatting           = "dd.MM";
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
            ReadCalendar();
            FilterEntries();
            UpdateUI();
            
            var selectedEvents = _calendarEvents.Take(_myConfiguration.NumberOfEntries).ToList();
            var formattedResult = 
                "Events that were read from Google Calendar:\n\n" +
                string.Join('\n', selectedEvents) + "\n\n";

            return (false, formattedResult);
		}

		public override Dictionary<string,string> GetHelp()
		{
            var texts = new Dictionary<string,string>();
            texts.Add("de-DE", @"This module displays up to four events from your Google calendar, sorted by date and time");
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
            DeserializeWeekdayNames();
            DeserializeBlacklist();
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

        private void DeserializeWeekdayNames()
        {
            if (!string.IsNullOrEmpty(_myConfiguration.WeekdayNames))
                _subjectBlacklist = _myConfiguration.WeekdayNames.Split('|', StringSplitOptions.RemoveEmptyEntries);
            
            if (_subjectBlacklist is null || _subjectBlacklist.GetLength(0) < (3+7))
            {
                _subjectBlacklist = new string[3] { "Yesterday", "Today", "Tomorrow" };
                _myConfiguration.WeekdayNames = "Yesterday|Today|Tomorrow|Sun|Mon|Tue|Wed|Thu|Fri|Sat";
            }
        }

        private void DeserializeBlacklist()
        {
            if (!string.IsNullOrEmpty(_myConfiguration.SubjectBlacklist))
                _subjectBlacklist = _myConfiguration.SubjectBlacklist.Split('|', StringSplitOptions.RemoveEmptyEntries);
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
            
            CreateRow(0, nameof(W1), nameof(D1));
            CreateRow(1, nameof(W2), nameof(D2));
            CreateRow(2, nameof(W3), nameof(D3));
            CreateRow(3, nameof(W4), nameof(D4));
 
            _Parent.Children.Add(_grid);
            Panel.SetZIndex(_ValueControl, -2);
            Panel.SetZIndex(_NameControl, -1);
        }

        protected void CreateRow(int rowIndex, string col1PropertyName, string col2PropertyName)
        {
            var rowHeight = _config.FontSize;
            var fontSize = _config.FontSize * 30 / 45;

            var rowDef = new RowDefinition() { Height = new GridLength(rowHeight) };
            _grid.RowDefinitions.Add(rowDef);

            var row = new Grid();
            Grid.SetRow(row, rowIndex);

            CreateCell(row, 0, col1PropertyName, fontSize);
            CreateCell(row, 1, col2PropertyName, fontSize);

            _grid.Children.Add(row);
        }

        private void CreateCell(Grid row, int columnIndex, string textPropertyName, int fontSize)
        {
            var col2 = new ColumnDefinition();
            row.ColumnDefinitions.Add(col2);
            var textBlock = CreateTextBlock(_textColor, fontSize, textPropertyName);
            Panel.SetZIndex(textBlock, 2);
            Grid.SetColumn(textBlock, columnIndex);
            row.Children.Add(textBlock);
        }

        protected TextBlock CreateTextBlock(Brush foreground, int fontSize, string propertyName)
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
            LoadCalendarEntriesFromGoogle();
        }
        #endregion
		#region ------------- Load data from Google -----------------------------------------------
        private void LoadCalendarEntriesFromGoogle()
        {
            try
            {
                if (_myConfiguration.DaysToReadInAdvance < 1)
                    _myConfiguration.DaysToReadInAdvance = 5;

                // If we cannot find the credentials file, try to find it in the user directory (our subdirectory)
                if (!File.Exists(_myConfiguration.GoogleCredentials))
                {
                    var tryInUserDirectory = Path.Combine(_config.ApplicationData.DataDirectory, _myConfiguration.GoogleCredentials);
                    if (File.Exists(tryInUserDirectory))
                        _myConfiguration.GoogleCredentials = tryInUserDirectory;
                }

                if (string.IsNullOrEmpty(_myConfiguration.GoogleCredentials))
                {
                    _myConfiguration.GoogleCredentials = "GoogleCredentialsOlli.json";
                    var currentdir = Directory.GetCurrentDirectory();
                    if (!File.Exists(_myConfiguration.GoogleCredentials))
                    {
                        var json = "{ \"installed\": { \"client_id\": \"\", \"project_id\": \"\", \"auth_uri\": \"https://accounts.google.com/o/oauth2/auth\", \"token_uri\": \"https://www.googleapis.com/oauth2/v3/token\", \"auth_provider_x509_cert_url\": \"https://www.googleapis.com/oauth2/v1/certs\", \"client_secret\": \"\", \"redirect_uris\": [ \"urn:ietf:wg:oauth:2.0:oob\", \"http://localhost\" ] } }";
                        File.WriteAllText(_myConfiguration.GoogleCredentials, json);
                    }
                }

                var reader = new GoogleCalendarReader()
                    .UseCredentialsFile(_myConfiguration.GoogleCredentials)
                    .UseApplicationName("AllOnOnePage");

                _calendarEvents = reader.ReadEventsByStartTime(
                    DateTime.Now, 
                    DateTime.Now.AddDays(_myConfiguration.DaysToReadInAdvance));
            }
            catch (Exception)
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
                foreach(var @event in _calendarEvents.Where(e => e.Summary is not null).Take(_myConfiguration.NumberOfEntries).ToList())
                {
                    var eventIsOnBlacklist = _subjectBlacklist.Any(b => @event.Summary.Contains(b));

                    if (!eventIsOnBlacklist)
                    {
                        var weekday = @event.When?.DayOfWeek.ToString() ?? "???";
                        var formattedDate = @event.When?.ToString(_myConfiguration.DateFormatting) ?? "???";
                        _entries.Add(new Entry(@event.When, @event.Summary, "???", weekday, formattedDate));
                    }
                }
            }
			catch (Exception ex)
			{
                _connectorMessages += ex.Message;
			}
        }
        #endregion
		#region ------------- Date formatting -----------------------------------------------------
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
                case DayOfWeek.Sunday   : return _subjectBlacklist[3]; // "Sun";
                case DayOfWeek.Monday   : return _subjectBlacklist[4]; // "Mon";
                case DayOfWeek.Tuesday  : return _subjectBlacklist[5]; // "Tue";
                case DayOfWeek.Wednesday: return _subjectBlacklist[6]; // "Wed";
                case DayOfWeek.Thursday : return _subjectBlacklist[7]; // "Thu";
                case DayOfWeek.Friday   : return _subjectBlacklist[8]; // "Fri";
                case DayOfWeek.Saturday : return _subjectBlacklist[9]; // "Sat";
                default                 : return "???";
            }
        }
        #endregion
		#region ------------- Update UI -----------------------------------------------------------
        private void UpdateUI()
        {
            SortByDate();
            //ReplaceNearEventByTodayTomorrow(_entries);
            CopyToWpfProperties(_entries);
        }

        private void ReplaceNearEventByTodayTomorrow(List<Entry> entries)
        {
            foreach (var entry in entries)
            {
                if (entry.ConvertedDate == DateTime.Today.AddDays(-1))
                {
                    entry.Weekday = _subjectBlacklist[0]; // "Yesterday";
                    entry.Date    = "";
                }
                else if (entry.ConvertedDate == DateTime.Today)
                {
                    entry.Weekday = _subjectBlacklist[1]; // "Today";
                    entry.Date    = "";
                }
                else if (entry.ConvertedDate == DateTime.Today.AddDays(1))
                {
                    entry.Weekday = _subjectBlacklist[2]; // "Tomorrow";
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
                W1 = entries[0].Heading; NotifyPropertyChanged(nameof(W1));
                D1 = entries[0].Weekday; NotifyPropertyChanged(nameof(D1));
            }

            if (entries.Count >= 2)
            {
                W2 = entries[1].Heading; NotifyPropertyChanged(nameof(W2));
                D2 = entries[1].Weekday; NotifyPropertyChanged(nameof(D2));
            }

            if (entries.Count >= 3)
            {
                W3 = entries[2].Heading; NotifyPropertyChanged(nameof(W3));
                D3 = entries[2].Weekday; NotifyPropertyChanged(nameof(D3));
            }

            if (entries.Count >= 4)
            {
                W4 = entries[3].Heading; NotifyPropertyChanged(nameof(W4));
                D4 = entries[3].Weekday; NotifyPropertyChanged(nameof(D4));
            }
        }
        #endregion
        #endregion
    }
}
