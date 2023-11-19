using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using PluginBase;
using System.Globalization;
using System.Threading.Tasks;
using System.IO;
using Abraham.Scheduler;

namespace AllOnOnePage.Plugins
{
    public class ModNotificationArea : ModBase, IPlugin
	{
		#region ------------- Settings ------------------------------------------------------------
		public class MyConfiguration : ModuleSpecificConfig
		{
			public bool        UseHomenet { get; set; }
			public bool        UseMqtt    { get; set; }
			public string      EventsJson { get; set; }
		}
		#endregion



        #region ------------- Types and constants -------------------------------------------------

        public class Event
        {
		    public string DataObjectOrTopic      { get; set; }
		    public List<TextRule> Rules          { get; set; }

            public Event()
            {
                Rules = new List<TextRule>();
            }
        }

        public class TextRule
        {
		    public string Operator        { get; set; }
		    public string Value           { get; set; }
		    public string Text            { get; set; }
		    public string SoundFile       { get; set; }
		    public string ForegroundColor { get; set; }
		    public int    DismissAfter    { get; set; }
		    public bool   Ignore          { get; set; }
        }
		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private MyConfiguration    _myConfiguration;
		private List<Event>        _events;
        private int                _dismissCurrentValueAfter;
        private static SoundPlayer _player = new ();
        private Scheduler          _scheduler;
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public override void Init(ModuleConfig config, Grid parent, System.Windows.Threading.Dispatcher dispatcher)
		{
			base.Init(config, parent, dispatcher);
			InitConfiguration();
			base.LoadAssembly("Abraham.Scheduler.dll");

			_scheduler = new Scheduler()
				.UseAction(() => { DismissText(); } );
		}

        public override void Stop()
        {
			_scheduler?.Stop();
            base.Stop();
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
            _config.FrameThickness = 1;
            _config.FrameColor = "#FFFFFFFF";

            var events = new List<Event>();
            var @event = new Event();
            @event.DataObjectOrTopic = "<YOUR MQTT TOPIC>";
            @event.Rules.Add(new TextRule() { Operator = "==", Value = "1", Text = "The door is open", SoundFile = "doorbell.wav", ForegroundColor = "Red", DismissAfter = 10 });
            events.Add(@event);
            _myConfiguration.EventsJson = System.Text.Json.JsonSerializer.Serialize(events);
		}

		public override async Task Save()
		{
			_config.ModulePrivateData = System.Text.Json.JsonSerializer.Serialize(_myConfiguration);
            DeserializeEventConfiguration();
		}

		public override async Task<(bool,string)> Validate()
		{
			UpdateContent(null);
            return (true, "");
		}

		public override async Task<(bool success, string messages)> Test()
		{
            return (false, "");
		}

        public override void UpdateLayout()
        {
            base.UpdateLayout();
            UpdateContent(null);
        }

        public override void UpdateContent(ServerDataObjectChange? dataObject)
        {
            (var forUs, var rule) = FindMatchingEvent(dataObject);
            if (!forUs)
                return;

            if (rule is not null)
            {
                if (rule.Ignore)
                    return;

                Value = rule.Text;
                SetUserDefinedColor(rule.ForegroundColor);

                _dismissCurrentValueAfter = (rule.DismissAfter > 0) ? rule.DismissAfter : 0;
                if (_dismissCurrentValueAfter > 0)
                    _scheduler.UseIntervalSeconds(_dismissCurrentValueAfter).Start();

                var soundFile = FindSoundFile(rule.SoundFile);
                if (soundFile is not null)
                    PlaySound(soundFile);
            }
            else
            {
                Value = "";
                _dismissCurrentValueAfter = 0;
            }

            NotifyPropertyChanged(nameof(Value));
            SetValueControlVisible();
            return;
        }

        public override Dictionary<string,string> GetHelp()
		{
            var texts = new Dictionary<string,string>();
            texts.Add("de-DE", 
@"This module can display events. You can define a set of MQTT topics with values.
When a topic changes to a certain value, that value or a appropriate text is shown in the
notification area.
The notification can disappear after a defined time.
If more topics come in, newer data replaces older.
This can also play a small notification sound, like a bell. Some sounds a shipped with the app.
Or display the text in a separate color.
You can define rules, i.e. display negative values in red, values above a threshold in yellow. 
Play a sound when your cat comes throught the cat flap.");
            return texts;
        }
        #endregion



		#region ------------- Implementation ------------------------------------------------------
		private void InitConfiguration()
        {
            DeserializeConfiguration();

            if (_myConfiguration == null)
                CreateSeedData();

            DeserializeEventConfiguration();
        }

        private void DeserializeConfiguration()
        {
            try
            {
                _myConfiguration = System.Text.Json.JsonSerializer.Deserialize<MyConfiguration>(_config.ModulePrivateData);
            }
            catch (Exception)
            {
            }
        }

        private void DeserializeEventConfiguration()
        {
            try
            {
                _events = System.Text.Json.JsonSerializer.Deserialize<List<Event>>(_myConfiguration.EventsJson);
            }
            catch (Exception)
            {
                _events = new();
            }
            _dismissCurrentValueAfter = 0;
        }

        private void DismissText()
        {
            Value = "";
            NotifyPropertyChanged(nameof(Value));
        }

        private (bool, TextRule) FindMatchingEvent(ServerDataObjectChange dataObject)
        {
            if (dataObject is null) // we only react on Events (not on a timer)
                return (false, null);

            var itsAServerEvent = (TheHomenetServerShouldBeUsed() && WeHaveReceivedAHomenetEvent(dataObject))
                               || (TheMqttBrokerShouldBeUsed()    && WeHaveReceivedAnMqttEvent  (dataObject));

            if (!itsAServerEvent)
                return (false, null);


            var theMessageIsForUs = false;

            foreach(var @event in _events)
            {
                if (@event.DataObjectOrTopic == dataObject.Name)
                {
                    theMessageIsForUs = true;
                    foreach(var rule in @event.Rules)
                    {
                        if (OperatorMatchesTheCurrentValue(dataObject.Value, rule))
                        {
                            return (true, rule);
                        }
                    }
                }
            }

            return (theMessageIsForUs, null);
        }

        private bool OperatorMatchesTheCurrentValue(string value, TextRule rule)
        {
            if (rule.Operator == "==" && rule.Value == value)
                return true;

            if (rule.Operator == "!=" && rule.Value != value)
                return true;

            if (rule.Operator == "contains" && value.Contains(rule.Value))
                return true;

            if (rule.Operator == "notcontains" && !value.Contains(rule.Value))
                return true;

            (var left, var right) = ConvertToDouble(value, rule.Value);

            if (rule.Operator == "<=" && left <= right)
                return true;

            if (rule.Operator == "<" && left < right)
                return true;

            if (rule.Operator == ">=" && left >= right)
                return true;

            if (rule.Operator == ">" && left > right)
                return true;

            return false;
        }

        private (double left, double right) ConvertToDouble(string value1, string value2)
        {
            double left = 0;
            double right = 0;

            if (double.TryParse(value1, NumberStyles.Any, CultureInfo.InvariantCulture, out double numberValue1))
                left = numberValue1;

            if (double.TryParse(value2, NumberStyles.Any, CultureInfo.InvariantCulture, out double numberValue2))
                right = numberValue2;

            return (left, right);            
        }

        private void SetUserDefinedColor(string rgbColor)
        {
            if (string.IsNullOrWhiteSpace(rgbColor))
                return;

            base._ValueControl.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom(rgbColor));
        }

        private string? FindSoundFile(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (File.Exists(path))
                    return path;

                var fullPath = Path.Combine(_config.ApplicationData.DataDirectory, "Sounds", path);
                if (File.Exists(fullPath))
                    return fullPath;

                fullPath = Path.Combine(_config.ApplicationData.ProgramDirectory, "Sounds", path);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }

        private void PlaySound(string soundFile)
        {
            try
            {
                _player.Play(soundFile);
            }
            catch (Exception)
            {
            }
        }
        #endregion



		#region ------------- Homenet -------------------------------------------------------------
		private bool WeHaveReceivedAHomenetEvent(ServerDataObjectChange dataObject)
        {
            return 
                dataObject is not null && 
                dataObject.ConnectorName == "HOMENET";
        }

		private bool TheHomenetServerShouldBeUsed()
        {
            return _myConfiguration.UseHomenet;
        }
        #endregion



		#region ------------- MQTT ----------------------------------------------------------------
		private bool WeHaveReceivedAnMqttEvent(ServerDataObjectChange dataObject)
        {
            return 
                dataObject is not null && 
                dataObject.ConnectorName == "MQTT";
        }

		private bool TheMqttBrokerShouldBeUsed()
        {
            return _myConfiguration.UseMqtt;
        }
        #endregion
    }
}
