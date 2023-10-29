using Abraham.MQTTClient;
using AllOnOnePage.Libs;
using AllOnOnePage.Plugins;
using System;

namespace AllOnOnePage
{
    internal class MqttBrokerConnection : IServerGetter
    {
        #region ------------- Properties ----------------------------------------------------------
        public bool Connected { get; private set; }
        public string ConnectionStatus { get; private set; }
        public SignalrClient.OnDataObjectChange_Handler OnDataobjectChange { get; set; }
        #endregion



        #region ------------- Fields --------------------------------------------------------------
        private string _serverUrl;
        private string _serverUser;
        private string _serverPassword;
        private int _serverTimeout;
        private MQTTClient _mqttClient;
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public MqttBrokerConnection(string serverUrl, string serverUser, string serverPassword, int serverTimeout)
        {
            _serverUrl      = serverUrl;
            _serverUser     = serverUser;
            _serverPassword = serverPassword;
            _serverTimeout  = serverTimeout;
        }
        #endregion



        #region ------------- Methods -------------------------------------------------------------
        public void Connect()
        {
            try
            {
                _mqttClient = new MQTTClient()
                    .UseUrl(_serverUrl)
                    .UseUsername(_serverUser)
                    .UsePassword(_serverPassword)
                    .UseTimeout(_serverTimeout)
                    .Build();
                Connected = true;
                ConnectionStatus = "connected";
                Start_Subscriber();
            }
            catch (Exception ex)
            {
                Connected = false;
                ConnectionStatus = "disconnected";
            }
        }

        public void Stop()
        {
             _mqttClient?.StopAllSubscriptions();
        }
        #endregion



        #region ------------- IServerGetter -------------------------------------------------------
        public ServerDataObject TryGet(string dataObjectName)
        {

            return new ServerDataObject("name", "not implemented");
        }
        #endregion



        #region ------------- Implementation ------------------------------------------------------
        private void Start_Subscriber()
        {
            _mqttClient.Subscribe("",
                delegate(string message)
                {
                    OnDataobjectChangeLocal(message);
                });
        }

        private void OnDataobjectChangeLocal(string message)
        {
            var Do = new Abraham.HomenetBase.Models.DataObject();
            Do.Name = "topic";
            Do.Value = message;
            OnDataobjectChange(Do);
        }
        #endregion
    }
}
