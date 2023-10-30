using Abraham.MQTTClient;
using AllOnOnePage.Libs;
using AllOnOnePage.Plugins;
using PluginBase;
using System;

namespace AllOnOnePage.Connectors
{
    internal class MqttBrokerConnection : IServerGetter, IConnector
    {
        #region ------------- Fields --------------------------------------------------------------
        private string     _serverUrl;
        private string     _serverUser;
        private string     _serverPassword;
        private int        _serverTimeout;
        private MQTTClient _mqttClient;
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public MqttBrokerConnection()
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

        public void Connect(Configuration config)
        {
            _serverUrl      = config.MqttBrokerUrl;
            _serverUser     = config.MqttBrokerUser;
            _serverPassword = config.MqttBrokerPassword;
            _serverTimeout  = config.MqttBrokerTimeout;
            Connect();
        }

        public void Connect()
        {
            try
            {
                ConnectionIsInProgress = true;
                _mqttClient = new MQTTClient()
                    .UseUrl(_serverUrl)
                    .UseUsername(_serverUser)
                    .UsePassword(_serverPassword)
                    .UseTimeout(_serverTimeout)
                    .Build()
                    ;//.SubscribeToAllTopics();
                IsConnected = true;

                //_mqttClient.OnEvent = OnDataobjectChangeLocal;
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

        public void Reconnect()
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
