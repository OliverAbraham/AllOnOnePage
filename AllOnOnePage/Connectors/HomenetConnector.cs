using Abraham.HomenetBase.Connectors;
using AllOnOnePage.Libs;
using AllOnOnePage.Plugins;
using PluginBase;
using System;
using System.Threading.Tasks;

namespace AllOnOnePage.Connectors
{
    internal class HomenetConnector : IServerGetter, IConnector
    {
        #region ------------- Fields --------------------------------------------------------------
        private string               _serverUrl;
        private string               _serverUser;
        private string               _serverPassword;
        private int                  _serverTimeout;
        private DataObjectsConnector _dataObjectsConnector;
        private SignalrClient        _signalrClient;
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public HomenetConnector()
        {
        }
        #endregion



        #region ------------- IServerGetter -------------------------------------------------------
        public ServerDataObject TryGet(string dataObjectName)
        {
            var Do = _dataObjectsConnector.TryGet(dataObjectName);
            return new ServerDataObject(Do.Name, Do.Value, Do.Timestamp);
        }
        #endregion



        #region ------------- IConnector ----------------------------------------------------------
        public string Name => "Homenet";

        public bool ConnectionIsInProgress { get; private set; }

        public bool IsConnected { get; set; }
        
        public string ConnectionStatus => IsConnected ? "connected" : "disconnected";

        public IServerGetter Getter => this;

        public ServerDataObjectChange_Handler OnDataobjectChange { get; set; }

        public bool IsConfigured(Configuration config)
        {
            return !string.IsNullOrEmpty(config.HomeAutomationServerUrl);
        }

        public async Task Connect(Configuration config)
        {
            _serverUrl      = config.HomeAutomationServerUrl;
            _serverUser     = config.HomeAutomationServerUser;
            _serverPassword = config.HomeAutomationServerPassword;
            _serverTimeout  = config.HomeAutomationServerTimeout;
            Connect();
        }

        public async Task Connect()
        {
            try
            {
                ConnectionIsInProgress = true;
                _dataObjectsConnector = new DataObjectsConnector(_serverUrl, _serverUser, _serverPassword, _serverTimeout);
                IsConnected = true;
                Start_SignalR_client();
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
            Stop_SignalR_client();
        }
        #endregion



        #region ------------- Implementation ------------------------------------------------------
        private void Start_SignalR_client()
        {
            _signalrClient = new SignalrClient();
            _signalrClient.OnDataObjectChange = OnDataobjectChangeLocal;
            _signalrClient.OnLogMessage += Handle_SignalR_log_message;
            _signalrClient.OnConnectionStateChange += Handle_connection_state_changes;
            _signalrClient.OnForceShutdown = Handle_SignalR_shutdown_request;
            _signalrClient.Start(_serverUrl, _serverUser, _serverPassword);
        }

        private void OnDataobjectChangeLocal(Abraham.HomenetBase.Models.DataObject Do)
        {
            var eventData = new ServerDataObjectChange("HOMENET", Do.Name, Do.Value, Do.Timestamp);
            OnDataobjectChange(eventData);
        }

        private void Handle_SignalR_shutdown_request()
        {
            System.Diagnostics.Debug.WriteLine($"Handle_SignalR_shutdown_request");
        }

        private void Handle_SignalR_log_message(string message)
        {
            System.Diagnostics.Debug.WriteLine($"Handle_SignalR_log_message: {message}");
        }

        private void Handle_connection_state_changes(string newState)
        {
            System.Diagnostics.Debug.WriteLine($"Handle_connection_state_change: {newState}");
            //Dispatcher.Invoke(() =>
            //{
            //    //SSLog($"OnConnectionStateChange: {newState}");
            //    //_VM.CurrentConnectionState.Value = newState;
            //    //_VM.Update(_VM.CurrentConnectionState);
            //    //Update_hidden_modules_the_first_time_connected(newState);
            //});
        }

        private void Stop_SignalR_client()
        {
            _signalrClient?.Stop();
        }
        #endregion
    }
}
