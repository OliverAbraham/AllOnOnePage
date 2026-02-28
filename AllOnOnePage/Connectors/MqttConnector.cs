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

        private class TopicTimestamp
        {
            public string         Topic     { get; set; }
            public string         Value     { get; set; }
            public DateTimeOffset Timestamp { get; set; }
        }
        private Dictionary<string, TopicTimestamp> _topicTimestamps = new();
        private string _valueCacheFilename = "AllOnOnePage_MQTT_timestamps.json";
        private DateTimeOffset _lastValueCacheSave = new DateTimeOffset();
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public MqttConnector()
        {
            ReadValueCacheFromDisk();
        }
        #endregion



        #region ------------- IServerGetter -------------------------------------------------------
        public ServerDataObject TryGet(string topic)
        {
            var value = _mqttClient.TryGet(topic);
            if (value != null)
            {
                var timestamp = GetTimestamp(topic, value);
                return new ServerDataObject(topic, value, timestamp);
            }
            else
            {
                return new ServerDataObject(topic, null, DateTimeOffset.Now);
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
                _mqttClient = new MQTTClient()
                    .UseUrl(_serverUrl)
                    .UseUsername(_serverUser)
                    .UsePassword(_serverPassword)
                    .UseTimeout(_serverTimeout)
                    .Build();

                await _mqttClient.SubscribeToAllTopicsAsync();
                _mqttClient.OnEvent = OnDataobjectChangeLocal;
                IsConnected = true;
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
            var timestamp = GetTimestamp(topic, value);
            var eventData = new ServerDataObjectChange("MQTT", topic, value, timestamp);
            OnDataobjectChange(eventData);

            SaveValueCacheToDiskPeriodically();
        }
        #endregion



        #region ------------- Value Cache ---------------------------------------------------------
        private DateTimeOffset GetTimestamp(string topic, string value)
        {
            if (_topicTimestamps.ContainsKey(topic))
            {
                if (_topicTimestamps[topic].Value != value)
                {
                    _topicTimestamps[topic].Value = value;
                    _topicTimestamps[topic].Timestamp = DateTimeOffset.Now;
                }
            }
            else
            {
                _topicTimestamps[topic] = new TopicTimestamp { Topic = topic, Value = value, Timestamp = DateTimeOffset.Now };
            }

            return _topicTimestamps[topic].Timestamp;
        }

        private void SaveValueCacheToDiskPeriodically()
        {
            try
            {
                var age = DateTimeOffset.Now - _lastValueCacheSave;
                if (age > TimeSpan.FromMinutes(10))
                {
                    SaveValueCacheToDisk();
                    _lastValueCacheSave = DateTimeOffset.Now;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving MQTT value cache to disk: {ex}");
            }
        }

        private void ReadValueCacheFromDisk()
        {
            var filename = Path.Combine(Path.GetTempPath(), _valueCacheFilename);
            if (File.Exists(filename))
            {
                var json = File.ReadAllText(filename);
                var tempDictionary = JsonConvert.DeserializeObject<Dictionary<string, TopicTimestamp>>(json) ?? null;
                if (tempDictionary is not null)
                    _topicTimestamps = tempDictionary;
            }
        }

        private void SaveValueCacheToDisk()
        {
            var filename = Path.Combine(Path.GetTempPath(), _valueCacheFilename);
            var json = JsonConvert.SerializeObject(_topicTimestamps);
            File.WriteAllText(filename, json);
        }
        #endregion
    }
}
