using Abraham.Scheduler;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AllOnOnePage.Libs
{
    public class SignalrClient
    {
        #region ------------- Properties ----------------------------------------------------------
        public HubConnection Connection { get; private set; }
        public string ConnectionState
        {
            get
            {
                return Connection == null ? "Disconnected" : Connection?.State.ToString();
            }
        }


        public delegate void OnLogMessage_Handler(string message);
        public OnLogMessage_Handler OnLogMessage
        {
            get
            {
                return _OnLogMessage;
            }
            set
            {
                _OnLogMessage = value != null ? value : delegate (string message) { };  // Null object pattern
            }
        }
        private OnLogMessage_Handler _OnLogMessage = delegate (string message) { };


        public delegate void OnForceShutdown_Handler();
        public OnForceShutdown_Handler OnForceShutdown
        {
            get
            {
                return _OnForceShutdown;
            }
            set
            {
                _OnForceShutdown = value != null ? value : delegate () { };  // Null object pattern
            }
        }
        private OnForceShutdown_Handler _OnForceShutdown = delegate () { };


        public delegate void OnMessage_Handler(Abraham.HomenetBase.Models.LogItem logItem);
        public OnMessage_Handler OnMessage
        {
            get
            {
                return _OnMessage;
            }
            set
            {
                _OnMessage = value != null ? value : delegate (Abraham.HomenetBase.Models.LogItem logItem) { };  // Null object pattern
            }
        }
        private OnMessage_Handler _OnMessage = delegate (Abraham.HomenetBase.Models.LogItem logItem) { };


        public delegate void OnDataObjectChange_Handler(Abraham.HomenetBase.Models.DataObject Do);
        public OnDataObjectChange_Handler OnDataObjectChange
        {
            get
            {
                return _OnDataObjectChange;
            }
            set
            {
                _OnDataObjectChange = value != null ? value : delegate (Abraham.HomenetBase.Models.DataObject Do) { };  // Null object pattern
            }
        }
        private OnDataObjectChange_Handler _OnDataObjectChange = delegate (Abraham.HomenetBase.Models.DataObject Do) { };


        public delegate void OnDeviceChange_Handler(Abraham.HomenetBase.Models.Device Do);
        public OnDeviceChange_Handler OnDeviceChange
        {
            get
            {
                return _OnDeviceChange;
            }
            set
            {
                _OnDeviceChange = value != null ? value : delegate (Abraham.HomenetBase.Models.Device Do) { };  // Null object pattern
            }
        }
        private OnDeviceChange_Handler _OnDeviceChange = delegate (Abraham.HomenetBase.Models.Device Do) { };


        public delegate void OnConnectionStateChange_Handler(string state);
        public OnConnectionStateChange_Handler OnConnectionStateChange
        {
            get
            {
                return _OnConnectionStateChange;
            }
            set
            {
                _OnConnectionStateChange = value != null ? value : delegate (string message) { };  // Null object pattern
            }
        }
        private OnConnectionStateChange_Handler _OnConnectionStateChange = delegate (string message) { };
        #endregion



        #region ------------- Fields --------------------------------------------------------------
        private string _CurrentConnectionState;
        private DateTime _CurrentStateTimestamp = new DateTime();
        private ThreadExtensions _SupervisorThread;
        private ThreadExtensions _ConnectionManagerThread;
        private CancellationTokenSource _CancellationTokenSource;
        private DateTime _LastConnected = DateTime.Now;
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public SignalrClient()
        {
            _CancellationTokenSource = new CancellationTokenSource();
        }
        #endregion



        #region ------------- Methods -------------------------------------------------------------
        public void Start(string server, string userName, string password)
        {
            if (Connection == null)
            {
                var authenticationHeader = HomenetBase.Authentication.BuildAuthenticationHeader(userName, password);

                Connection = new HubConnectionBuilder()
                    .WithUrl($"{server}/HomenetHub", options =>
                    {
                        options.Headers.Add("Authorization", "Basic " + authenticationHeader);
                    })
                    .WithAutomaticReconnect()
                    .Build();

                Connection.ServerTimeout = new TimeSpan(0, 0, 10);

                Connection.Closed += async (error) =>
                {
                    _OnLogMessage($"SignalR server connection is closed");
                    await Task.Delay(5 * 1000);
                    await Connection.StartAsync();
                };

                Connection.On<Abraham.HomenetBase.Models.LogItem>("OnMessageLogged", (logitem) => { OnMessage(logitem); });
                Connection.On<Abraham.HomenetBase.Models.DataObject>("OnDataObjectChanged", (dataobject) => { OnDataObjectChange(dataobject); });
                Connection.On<Abraham.HomenetBase.Models.Device>("OnDeviceChanged", (@event) => { OnDeviceChange(@event); });
            }
            Connection.StartAsync();
            StartSupervisorThread();
            StartConnectionManagerThread();
        }

        private void Restart_SignalR_client()
        {
            _OnLogMessage($"SignalR trying to restart: now stopping async...");
            Connection.StopAsync(_CancellationTokenSource.Token).Wait();
            _OnLogMessage($"SignalR trying to restart: waiting 10 seconds...");
            Thread.Sleep(10 * 1000);
            _OnLogMessage($"SignalR trying to restart: now starting again...");
            Connection.StartAsync(_CancellationTokenSource.Token).Wait();
            _OnLogMessage($"finished");
        }

        public void Stop()
        {
            _CancellationTokenSource.Cancel();
            Connection.StopAsync();
            Connection = null;
            StopSupervisorThread();
            StopConnectionManagerThread();
        }
        #endregion



        #region ------------- Implementation ------------------------------------------------------
        #region ------------- Connection manager thread -----------------------

        private void StartConnectionManagerThread()
        {
            _ConnectionManagerThread = new ThreadExtensions(ConnectionManagerThreadProc);
            _ConnectionManagerThread.Thread.Start();
        }

        private void StopConnectionManagerThread()
        {
            if (_ConnectionManagerThread != null)
                _ConnectionManagerThread.SendStopSignalAndWait();
        }

        private void ConnectionManagerThreadProc()
        {
            _OnLogMessage("ConnectionManagerThread starting...");
            do
            {
                try
                {
                    //if (ConnectionState.ToLower().Contains("reconnect"))
                    //	_ReconnectCounter++;

                    if (ConnectionState != _CurrentConnectionState)
                    {
                        _CurrentConnectionState = ConnectionState;
                        _CurrentStateTimestamp = DateTime.Now;
                        _OnConnectionStateChange(_CurrentConnectionState);
                    }

                    if (_CurrentConnectionState.ToLower() == "connected")
                        _LastConnected = DateTime.Now;

                    _ConnectionManagerThread.Sleep(1 * 1000);
                }
                catch (Exception ex)
                {
                    _OnLogMessage($"ConnectionManagerThread Exception: {ex.ToString()}");
                    _ConnectionManagerThread.Sleep(10 * 1000);
                }
            }
            while (_ConnectionManagerThread.Run);
            _OnLogMessage("ConnectionManagerThread has ended");
        }
        #endregion
        #region ------------- Supervisor thread -------------------------------

        private void StartSupervisorThread()
        {
            _SupervisorThread = new ThreadExtensions(SupervisorThreadProc);
            _SupervisorThread.Thread.Start();
        }

        private void StopSupervisorThread()
        {
            if (_SupervisorThread != null)
                _SupervisorThread.SendStopSignalAndWait();
        }

        private void SupervisorThreadProc()
        {
            _OnLogMessage("SupervisorThread starting...");
            do
            {
                try
                {
                    TimeSpan since = DateTime.Now - _CurrentStateTimestamp;
                    TimeSpan disconnectedSince = DateTime.Now - _LastConnected;
                    _OnLogMessage($"SignalR connection: Current state ({_CurrentConnectionState}) since {since.ToString(@"dd\.hh\:mm\:ss")} last connected: {_LastConnected.ToString(@"dd\.hh\:mm\:ss")}  not connected since {disconnectedSince.ToString(@"dd\.hh\:mm\:ss")}");

                    //if (_CurrentConnectionState == "Disconnected")
                    //{
                    //    _OnLogMessage($"SignalR is disconnected, trying to repair...");
                    //	try
                    //	{
                    //		Restart_SignalR_client();
                    //	}
                    //	catch (Exception ex)
                    //	{
                    //		_OnLogMessage($"SignalR error in repairing: {ex.ToString().Replace("\n", " ")}");
                    //	}
                    //}
                    //
                    //if (_ReconnectCounter > 10)
                    //{
                    //    _OnLogMessage($"SignalR is reconnecting more than 10 times, trying to repair...");
                    //    Restart_SignalR_client();
                    //}
                    //
                    //if (_ReconnectCounter > 20)
                    //{
                    //    _OnLogMessage($"SignalR is reconnecting more than 20 times, forcing app shutdown...");
                    //    _OnForceShutdown();
                    //}

                    if (disconnectedSince.TotalMinutes > 5)
                    {
                        _OnLogMessage($"SignalR disconnected for more than 5 minutes, forcing app shutdown...");
                        try
                        {
                            _OnForceShutdown();
                        }
                        catch (Exception ex)
                        {
                            _OnLogMessage($"SignalR problem in OnForceShutdown: {ex.ToString().Replace("\n", " ")}");
                        }
                    }

                    _SupervisorThread.Sleep(60 * 1000);
                }
                catch (Exception ex)
                {
                    _OnLogMessage($"SupervisorThread Exception: {ex.ToString().Replace("\n", " ")}");
                    _SupervisorThread.Sleep(10 * 1000);
                }
            }
            while (_SupervisorThread.Run);
            _OnLogMessage("SupervisorThread has ended");
        }
        #endregion
        #endregion
    }
}
