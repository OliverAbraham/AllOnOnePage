using Abraham.MQTTClient;
using AllOnOnePage.Plugins;
using PluginBase;
using System;
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
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public MqttConnector()
        {
        }
        #endregion



        #region ------------- IServerGetter -------------------------------------------------------
        public ServerDataObject TryGet(string topic)
        {
            var value = _mqttClient.TryGet(topic);
            return new ServerDataObject(topic, value);
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
            var eventData = new ServerDataObjectChange("MQTT", topic, value);
            OnDataobjectChange(eventData);
        }
        #endregion
    }
}
