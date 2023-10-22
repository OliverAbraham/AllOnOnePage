using Abraham.HomenetBase.Connectors;
using AllOnOnePage.Libs;
using HomenetBase;
using System;

namespace AllOnOnePage
{
    internal class HomeAutomationServerConnection
    {
        #region ------------- Properties ----------------------------------------------------------
        public DataObjectsConnector DataObjectsConnector;
        public bool Connected { get; private set; }
        public string ConnectionStatus { get; private set; }
        public SignalrClient.OnDataObjectChange_Handler OnDataobjectChange { get; set; }
        #endregion



        #region ------------- Fields --------------------------------------------------------------
        private string _serverUrl;
        private string _serverUser;
        private string _serverPassword;
        private int _serverTimeout;
        private SignalrClient _signalrClient;
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public HomeAutomationServerConnection(string serverUrl, string serverUser, string serverPassword, int serverTimeout)
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
                DataObjectsConnector  = new DataObjectsConnector(_serverUrl, _serverUser, _serverPassword, _serverTimeout);
                Connected = true;
                ConnectionStatus = "connected";
                Start_SignalR_client();
            }
            catch (Exception ex)
            {
                Connected = false;
                ConnectionStatus = "disconnected";
            }
        }

        internal void Stop()
        {
             Stop_SignalR_client();
        }
        #endregion



        #region ------------- Implementation ------------------------------------------------------
        #region ------------- SignalR client ----------------------------------
        private void Start_SignalR_client()
        {
            _signalrClient = new SignalrClient();
            _signalrClient.OnDataObjectChange = OnDataobjectChangeLocal;
            _signalrClient.OnLogMessage += Handle_SignalR_log_message;
            _signalrClient.OnConnectionStateChange += Handle_connection_state_changes;
            _signalrClient.OnForceShutdown = Handle_SignalR_shutdown_request;
            _signalrClient.Start(_serverUrl, _serverUser, _serverPassword);
        }

        private void OnDataobjectChangeLocal(DataObject Do)
        {
            OnDataobjectChange(Do);
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
            _signalrClient.Stop();
        }
        #endregion
        #endregion
    }
}
