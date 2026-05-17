using Abraham.MQTTClient;
using AllOnOnePage.Plugins;
using Newtonsoft.Json;
using PluginBase;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace AllOnOnePage.Connectors
{
    internal class MqttConnector : IServerGetter, IConnector
    {
        #region ------------- Fields --------------------------------------------------------------
        private string     _serverUrl;
        private string     _serverUser;
        private string     _serverPassword;
        private int        _serverTimeout;
        private MQTTClient _mqttClient;
        private DateTime   _lastReaction;

        private class TopicTimestamp
        {
            public string         Topic     { get; set; }
            public string         Value     { get; set; }
            public DateTimeOffset Timestamp { get; set; }
        }
        private Dictionary<string, TopicTimestamp> _topicCache = new();
        private string _topicCacheFilename = "AllOnOnePage_MQTT_timestamps.json";
        private DateTimeOffset _topicCacheLastSaveAction = new DateTimeOffset();
        private bool _topicCacheIsDirty = false;

        private record Telegram(string topic, string value);
        private Dictionary<string,string> _topicsToIgnore = new();

        public class MqttEntity
		{
			public string value;
			public string timestamp;

			public MqttEntity(string topic, string timestamp)
			{
				this.value = topic;
				this.timestamp = timestamp;
			}
		}
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public MqttConnector()
        {
            _lastReaction = DateTime.Now;
            ReadValueCacheFromDisk();
        }
        #endregion



        #region ------------- IServerGetter -------------------------------------------------------
        public ServerDataObject TryGet(string topic)
        {
            var value = _mqttClient.TryGet(topic);
            if (value != null)
            {
                return GetCreateOrUpdateEntityFromCache(topic, value, DateTimeOffset.Now);
            }
            else
            {
                return GetCreateOrUpdateEntityFromCache(topic, "???", DateTimeOffset.Now);
            }
        }
        #endregion



        #region ------------- IConnector ----------------------------------------------------------
        public string Name => "MQTT";

        public bool ConnectionIsInProgress { get; private set; }

        public bool IsConnected { get; set; }

        public string ConnectionStatus => IsConnected ? "connected" : "disconnected";

        public IServerGetter Getter => this;

        public ServerDataObjectChange_Handler OnDataobjectChange { get; set; }
        
        public DateTime LastReaction => _lastReaction;

        public bool IsConfigured(Configuration config)
        {
            return !string.IsNullOrEmpty(config.MqttBrokerUrl);
        }

        public async Task Connect(Configuration config)
        {
            _serverUrl      = config.MqttBrokerUrl;
            _serverUser     = config.MqttBrokerUser;
            _serverPassword = config.MqttBrokerPassword;
            _serverTimeout  = config.MqttBrokerTimeout;
            Connect();
        }

        public async Task Connect()
        {
            try
            {
                ConnectionIsInProgress = true;

                if (_mqttClient is not null)
                {
                    _mqttClient.StopAllSubscriptions();
                    _mqttClient = null;
                }

                _mqttClient = new MQTTClient()
                    .UseUrl(_serverUrl)
                    .UseUsername(_serverUser)
                    .UsePassword(_serverPassword)
                    .UseTimeout(_serverTimeout)
                    .Build();

                await _mqttClient.SubscribeToAllTopicsAsync();
                _mqttClient.OnEvent = OnDataobjectChangeLocal;
                IsConnected = true;
                _lastReaction = DateTime.Now;
            }
            catch (Exception ex)
            {
                IsConnected = false;
            }
            finally
            {
                ConnectionIsInProgress = false;
            }
        }

        public async Task Reconnect()
        {
            Connect();
        }

        public void Stop()
        {
            _mqttClient?.StopAllSubscriptions();
        }
        #endregion



        #region ------------- Implementation ------------------------------------------------------
        private void OnDataobjectChangeLocal(string topic, string value)
        {
            _lastReaction = DateTime.Now;
            var timestamp = new DateTimeOffset();

            // if we receive a topic that ends with "/timestamp", this is a json structure holding value AND timestamp
            if (topic.EndsWith("/timestamp"))
            {
                var dto = JsonConvert.DeserializeObject<MqttEntity>(value);
                if (DateTimeOffset.TryParse(dto.timestamp, out var parsedTimestamp))
                    timestamp = parsedTimestamp;
                value = dto.value;

                // we keep in mind that we can ignore all further Telegrams with the same topic, but ending with /state or no suffix,
                // because we already have the timestamp from the /timestamp topic.
                var baseTopic = topic.Replace("/timestamp", "");
                if (!_topicsToIgnore.ContainsKey(baseTopic))
                    _topicsToIgnore.TryAdd(baseTopic, baseTopic);

                var entity = GetCreateOrUpdateEntityFromCache(baseTopic, value, timestamp);
                var eventData = new ServerDataObjectChange("MQTT", topic, entity.Value, entity.Timestamp);
                OnDataobjectChange(eventData);
            }
            else
            {
                // we ignore all MQTT telegrams where we already received a timestamp topic for the same base topic
                var baseTopic = topic.Replace("/state", "");
                if (_topicsToIgnore.ContainsKey(baseTopic))
                    return;

                // for all other telegrams we generate an artifical timestamp
                var entity = GetCreateOrUpdateEntityFromCache(topic, value, DateTimeOffset.Now);
                var eventData = new ServerDataObjectChange("MQTT", topic, entity.Value, entity.Timestamp);
                OnDataobjectChange(eventData);
            }

            SaveValueCacheToDiskPeriodically();
        }
        #endregion



        #region ------------- Value Cache ---------------------------------------------------------
        private ServerDataObject GetCreateOrUpdateEntityFromCache(string topic, string newValue, DateTimeOffset newTimestamp)
        {
            if (!_topicCache.ContainsKey(topic))
            {
                _topicCache.Add(topic, new TopicTimestamp { Topic = topic, Value = "???", Timestamp = newTimestamp });
                _topicCacheIsDirty = true;
            }

            if (_topicCache[topic].Value != newValue)
            {
                _topicCache[topic].Value = newValue;
                _topicCache[topic].Timestamp = newTimestamp;
                _topicCacheIsDirty = true;
            }

            return new ServerDataObject(topic, _topicCache[topic].Value, _topicCache[topic].Timestamp);
        }

        private void SaveValueCacheToDiskPeriodically()
        {
            try
            {
                var ageInSeconds = (DateTimeOffset.Now - _topicCacheLastSaveAction).TotalSeconds;
                if (_topicCacheIsDirty && ageInSeconds > 10)
                {
                    SaveValueCacheToDisk();
                    _topicCacheLastSaveAction = DateTimeOffset.Now;
                    _topicCacheIsDirty = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving MQTT value cache to disk: {ex}");
            }
        }

        private void ReadValueCacheFromDisk()
        {
            var filename = Path.Combine(Path.GetTempPath(), _topicCacheFilename);
            if (File.Exists(filename))
            {
                var json = File.ReadAllText(filename);
                var tempDictionary = JsonConvert.DeserializeObject<Dictionary<string, TopicTimestamp>>(json) ?? null;
                if (tempDictionary is not null)
                    _topicCache = tempDictionary;
            }
        }

        private void SaveValueCacheToDisk()
        {
            var filename = Path.Combine(Path.GetTempPath(), _topicCacheFilename);
            var json = JsonConvert.SerializeObject(_topicCache);
            File.WriteAllText(filename, json);
        }
        #endregion
    }
}
