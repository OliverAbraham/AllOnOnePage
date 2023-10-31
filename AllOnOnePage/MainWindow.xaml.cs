using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using Abraham.AutoUpdater;
using Abraham.OpenWeatherMap;
using Abraham.WPFWindowLayoutManager;
using AllOnOnePage.Connectors;
using AllOnOnePage.DialogWindows;
using AllOnOnePage.Libs;
using AllOnOnePage.Plugins;
using PluginBase;

namespace AllOnOnePage
{
    public partial class MainWindow : Window
    {
		#region ------------- Fields --------------------------------------------------------------
		#region Configuration
        private ConfigurationManager   _configurationManager;
        private Configuration          _config => _configurationManager.Config;
   		private ApplicationData        _applicationData;
		private HelpTexts              _texts;
        private WindowLayoutManager    _windowLayoutManager;
		private Logger                 _logger;
        private PluginLoader           _pluginLoader;
        #endregion
        #region Dynamic data
        private ViewModel              _vm;
        private bool                   _nowUpdating;
        private Timer                  _periodicTimer;
        private Timer                  _dateTimeUpdateTimer;
        #endregion
		#region Power management and Supervisor
		private WindowsPowermanagement _powermanagement;
		private Timer                  _buttonFadeOutTimer;
		#endregion
		#region Updater
		private Updater                _updater;
        #endregion
		#region Connectors for Home automation servers
        private List<IConnector> _connectors = new List<IConnector>()
        {
            new HomenetConnector(),
            new MqttConnector()
        };
        private bool _endTheReconnectorLoop;
        #endregion
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public MainWindow()
		{
			try
            {
                Init_OpenWeatherMap_Connector();
                Init_Configuration();
                Init_Logger();
                Init_LayoutManager();
                Init_Plugins();
                InitializeComponent();
                Init_GlobalExceptionHandler();
                Init_ViewModel();
                Init_Background_Size_and_Position();
                Init_HelpTexts();
            }
            catch (Exception ex)
			{
                MessageBox.Show(ex.ToString());
                Close();
			}
		}

        private static void Init_OpenWeatherMap_Connector()
        {
            var connector = new OpenWeatherMapConnector(); // we need to do this here to load the libs
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
			Init_Updater();
            WaitAndThenCallMethod(wait_time_seconds:1, action:Startup);
		}

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			CleanupPlugins();
            _endTheReconnectorLoop = true;
            StopAllConnectors();
			Stop_date_time_update_timer();
            _updater?.Stop();
			_configurationManager.Save();
		}

        private void Window_Closed(object sender, EventArgs e)
        {
            //StopPowerManagement();
            //_programStateManager.Save_state_to_disk();
        }

        private Timer CreateAndStartTimer(ElapsedEventHandler timerProc, int interval_in_seconds, bool repeatedly = true)
        {
            var timer = new Timer();
            timer.Interval = interval_in_seconds * 1000;
            timer.Elapsed += timerProc;
            timer.AutoReset = repeatedly;
            timer.Start();
            return timer;
        }

        private Timer WaitAndThenCallMethod(int wait_time_seconds, Action action)
        {
			return CreateAndStartTimer(
                delegate(object sender, ElapsedEventArgs e)
                {
                    try
                    {
						Dispatcher.Invoke(() =>
						{
                            try
							{
                                action();
							}
							catch (Exception ex)
							{
                                Debug.WriteLine(ex.ToString());
							}
						});
                    }
                    catch (Exception) { }
                }, wait_time_seconds, repeatedly:false);
        }
        #endregion



        #region ------------- Implementation ------------------------------------------------------
        #region ------------- Startup -----------------------------------------
		private void Init_Configuration()
		{
            _applicationData = new ApplicationData();
            _applicationData.ProgramDirectory = Directory.GetCurrentDirectory();
			_texts = new HelpTexts();
			_configurationManager = new ConfigurationManager(_texts, _applicationData);
			_configurationManager.CreateDataDirectoryIfNotExists();
			_configurationManager.SetCurrentDirectoryToDataDirectory();
			_configurationManager.Load();
            _applicationData.DataDirectory = _configurationManager.DataDirectory;
		}

		private void Init_Logger()
		{
			_logger = new Logger(_config);
		}

        private void Startup()
        {
            Dispatcher.BeginInvoke( async () => InitAndReconnectConnectorsLoop() );
		}

        private void Startup2()
        {
            try
            {
                //Read_saved_state_from_disk();

                Init_all_modules();
                Start_periodic_UI_update_timer();
                Start_date_time_update_timer();
                //StartSupervisorThread();
                Show_Welcome_Screen();
                FadeOutEditControlAndHelpTexts();
                FadeOutVersionInfo();
            }
            catch (Exception ex)
            {
                _vm.DisplayHardError("Startfehler!");
                _logger.Log(ex.ToString());
                //CreateAndStartTimer(ShutdownTimer_Elapsed, 10, repeatedly:false);
            }
        }

        private void Init_ViewModel()
		{
			_vm = new ViewModel(this, _config, _texts, _applicationData);
			_vm.Dispatcher = Dispatcher;
			_vm.SaveConfiguration = _configurationManager.Save;
            _vm.LayoutManager = _windowLayoutManager;
			DataContext = _vm;
		}
        #endregion
        #region ------------- Closing -----------------------------------------
        private void ShutdownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                //File.WriteAllText("restart-request.dat", "Program requested a hard restart");
                _vm.DisplayHardError("restarting...!");
                ShutdownTheApp();
            });
        }

        private void ShutdownTheApp()
		{
			Stop_periodic_UI_update_timer();
			Stop_LayoutManager();
            _updater.Stop();
			Close();
		}
		#endregion
		#region ------------- Global exception handler ------------------------
		private void Init_GlobalExceptionHandler()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
		}

        [HandleProcessCorruptedStateExceptionsAttribute]
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string Msg = "CurrentDomain_UnhandledException!" + "\r\n" + e.ExceptionObject.ToString();
            _logger.Log(Msg);
            MessageBox.Show(Msg, _texts[HelpTexts.ID.ERROR_HEADING]);
        }
        #endregion
        #region ------------- Main window layout ------------------------------
		private void Init_LayoutManager()
		{
			_windowLayoutManager = new WindowLayoutManager(this, nameof(MainWindow));
            _windowLayoutManager.RestoreSizeAndPosition(this, nameof(MainWindow));
		}

		private void Stop_LayoutManager()
		{
			_windowLayoutManager.Save();
		}
        #endregion
        #region ------------- Plugins -----------------------------------------
		private void Init_Plugins()
		{
			_pluginLoader = new PluginLoader();
            _pluginLoader.LoadPlugins(_applicationData.ProgramDirectory);
            _applicationData.PluginDirectory = _pluginLoader.ActualPluginDirectory;

            if (_pluginLoader.Processors.Count == 0)
			{
                MessageBox.Show("no plugins found! keine Plugins gefunden!");
			}
		}

		private void CleanupPlugins()
		{
            if (_pluginLoader != null)
			    _pluginLoader.StopPlugins();
		}
        #endregion
        #region ------------- Periodic date/time update -----------------------
        private void Start_date_time_update_timer()
        {
            _dateTimeUpdateTimer = new Timer();
            _dateTimeUpdateTimer.Interval = 1 * 1000;
            _dateTimeUpdateTimer.Elapsed += 
                delegate(object sender, ElapsedEventArgs e)
                {
                    Dispatcher.Invoke(() => { _vm.Time(); });
                };

            _dateTimeUpdateTimer.Start();
        }

        private void Stop_date_time_update_timer()
        {
            if (_dateTimeUpdateTimer != null)
                _dateTimeUpdateTimer.Stop();
        }
		#endregion
        #region ------------- Periodic tile update ----------------------------
        private void Start_periodic_UI_update_timer()
        {
            int FirstUpdateAfterProgramStartInSeconds = 1;
            _periodicTimer = new Timer();
            _periodicTimer.Interval = FirstUpdateAfterProgramStartInSeconds * 1000;
            _periodicTimer.Elapsed += Periodic_timer_elapsed;
            _periodicTimer.Start();
        }
        
        private void Stop_periodic_UI_update_timer()
        {
            if (_periodicTimer != null)
                _periodicTimer.Stop();
        }
        
        private void Periodic_timer_elapsed(object sender, ElapsedEventArgs e)
        {
            _periodicTimer.Stop();
            
            Dispatcher.Invoke(() =>
            {
                Update_all_modules();
            });

            if (_config.UpdateIntervalInSeconds == 0)
                _config.UpdateIntervalInSeconds = 60;

            _periodicTimer.Interval = _config.UpdateIntervalInSeconds * 1000;
            _periodicTimer.Start();
        }

        private void Update_all_modules(ServerDataObjectChange? Do = null)
        {
            if (Do is not null)
                System.Diagnostics.Debug.WriteLine($"Update_all_modules: {Do.ConnectorName} change event: {Do.Name} = {Do.Value}");

            if (_nowUpdating)
                return;
            _nowUpdating = true;
            try
            {
                _vm.Update_all_modules(Do);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                _logger.Log($"Update_all_modules Exception: {ex.ToString()}");
            }
            finally
            {
                _nowUpdating = false;
            }
        }

        #endregion
        #region ------------- Modules -----------------------------------------
        private void Init_all_modules()
        {
            _vm.Init_all_modules(_pluginLoader, _pluginLoader.Processors, this.MainGrid, this.canvas);
        }
        #endregion
		#region ------------- Power management --------------------------------
        private void StartPowerManagement()
        {
            _powermanagement = new WindowsPowermanagement(this);
        }

        private void StopPowerManagement()
        {
            if (_powermanagement != null)
                _powermanagement.Close();
        }
        #endregion
        #region ------------- Buttons and help texts --------------------------
        private void Init_HelpTexts()
        {
            VersionInfo.Content = $"Version {AppVersion.Version.VERSION}";
            ServerInfo.Content = "";
            HelpText1.Content = _texts[HelpTexts.ID.CLICKHERETOEND];
            HelpText2.Content = _texts[HelpTexts.ID.CLICKHERETOEDIT];
            HelpText3.Content = _texts[HelpTexts.ID.CLICKHERETOFULL];
        }

        private void FadeOutEditControlAndHelpTexts()
        {
            WaitAndThenCallMethod(wait_time_seconds:5, action:FadeOutHelpTexts);
            WaitAndThenCallMethod(wait_time_seconds:10, action:FadeOutButtons);
        }

		private void FadeOutHelpTexts()
		{
			WpfAnimations.FadeOutLabel(HelpText1);
			WpfAnimations.FadeOutLabel(HelpText2);
			WpfAnimations.FadeOutLabel(HelpText3);
			WpfAnimations.FadeOutLabel(HelpText4);
			WpfAnimations.FadeOutLabel(HelpText5);
			//WpfAnimations.FadeOutLabel(HelpText6);
			//WpfAnimations.FadeOutLabel(HelpText7);
		}

        private void FadeOutVersionInfo()
        {
            WaitAndThenCallMethod(wait_time_seconds:10, action: () => WpfAnimations.FadeOutLabel(VersionInfo));			
        }

        private void FadeOutServerInfo()
        {
            WaitAndThenCallMethod(wait_time_seconds:10, action: () => WpfAnimations.FadeOutLabel(ServerInfo));			
        }

        private void FadeInServerInfo()
        {
            WpfAnimations.FadeInLabel(ServerInfo);
        }

		private void ShowButtonsOnMouseHover(Window sender, MouseEventArgs e)
		{
			var pos = e.GetPosition(sender);
            if (pos.X > sender.Width-100 && pos.X < sender.Width-40 && 
                pos.Y > 30 && pos.Y < 400)
			{
                MouseIsInButtonArea();
			}
            else
			{
				MouseIsNotInButtonArea();
			}
		}

		private void MouseIsInButtonArea()
		{
			if (_buttonFadeOutTimer != null)
			{
                _buttonFadeOutTimer.Stop();
                _buttonFadeOutTimer = null;
                FadeInButtons();
			}
		}

		private void MouseIsNotInButtonArea()
		{
			if (_buttonFadeOutTimer != null)
                return;
            _buttonFadeOutTimer = WaitAndThenCallMethod(wait_time_seconds:5, action:FadeOutButtons);
		}

		private void FadeInButtons()
        {
			WpfAnimations.FadeIn(Button_Close      , UIElement.OpacityProperty);
			WpfAnimations.FadeIn(Button_Add        , UIElement.OpacityProperty);
			WpfAnimations.FadeIn(Button_Fullscreen , UIElement.OpacityProperty);
			WpfAnimations.FadeIn(Button_Info       , UIElement.OpacityProperty);
			WpfAnimations.FadeIn(Button_Settings   , UIElement.OpacityProperty);
			//WpfAnimations.FadeIn(Button_Wastebasket, UIElement.OpacityProperty);
			//WpfAnimations.FadeIn(Button_EditMode   , UIElement.OpacityProperty);
		}

		private void FadeOutButtons()
        {
			WpfAnimations.FadeOutButLeaveVisible(Button_Close      , UIElement.OpacityProperty);
			WpfAnimations.FadeOutButLeaveVisible(Button_Add        , UIElement.OpacityProperty);
			WpfAnimations.FadeOutButLeaveVisible(Button_Fullscreen , UIElement.OpacityProperty);
			WpfAnimations.FadeOutButLeaveVisible(Button_Info       , UIElement.OpacityProperty);
			WpfAnimations.FadeOutButLeaveVisible(Button_Settings   , UIElement.OpacityProperty);
			//WpfAnimations.FadeOutButLeaveVisible(Button_Wastebasket, UIElement.OpacityProperty);
			//WpfAnimations.FadeOutButLeaveVisible(Button_EditMode   , UIElement.OpacityProperty);
		}
		#endregion
		#region ------------- Buttons -----------------------------------------
		private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

		private void Button_Fullscreen_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
            if (_config.FullScreenDisplay)
			{
                _config.FullScreenDisplay = false;
                this.WindowStyle = WindowStyle.SingleBorderWindow;
			}
            else
			{
                _config.FullScreenDisplay = true;
                this.WindowState = WindowState.Maximized;
                this.WindowStyle = WindowStyle.None;
			}
		}

        private void Button_Edit_Click(object sender, RoutedEventArgs e)
        {
            bool editModeOn = _vm.Button_Edit_Click();
            if (editModeOn)
                WpfAnimations.FadeIn(Button_Add, UIElement.OpacityProperty);
            else
                WpfAnimations.FadeOutButLeaveVisible(Button_Add,  UIElement.OpacityProperty);
        }

		private void Button_Wastebasket_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
            _vm.Button_Wastebasket_Click();
		}

		private void Button_Editmode_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
            _vm.Button_Editmode_Click();
		}

		private void Button_AddNewModule_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
            _vm.AddNewModule();
		}
        #endregion
		#region ------------- Mouse events ------------------------------------
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _vm.Window_MouseLeftButtonDown(this, e);
        }

        private void Window_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _vm.Window_MouseLeftButtonUp(this, e);
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _vm.Window_MouseMove(this, e);
            ShowButtonsOnMouseHover(this, e);
        }

		private void Window_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _vm.MouseDoubleClick(this, e);
        }

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _vm.MouseRightButtonDown(this, e);
        }
		#endregion
        #region ------------- UI background -----------------------------------
        private void Init_Background_Size_and_Position()
		{
            EditBackground.RestoreSelectedBackground(this, _config, _applicationData.ProgramDirectory);
		}
		#endregion
        #region ------------- Welcome screen ----------------------------------
        private void Show_Welcome_Screen()
		{
            if (!_config.WelcomeScreenDisabled)
			{
                var wnd = new WelcomeScreen(_texts);
                wnd.Owner = this;
                wnd.ShowDialog();
                _config.WelcomeScreenDisabled = true;
			}
		}
		#endregion
        #region ------------- Program info ------------------------------------
		private void Button_Info_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
            var wnd = new ProgramInfo(_texts, AppVersion.Version.VERSION, _updater);
            wnd.Owner = this;
            wnd.ShowDialog();
		}
		#endregion
        #region ------------- Settings ----------------------------------------
		private void Button_Settings_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
            var wnd = new EditSettings(_config);
            wnd.Owner = this;
            wnd.LayoutManager = _windowLayoutManager;
            wnd.ShowDialog();
            if (wnd.DialogResult == true)
			{
                _configurationManager.Save();
			}
		}
		#endregion
		#region ------------- Updater -----------------------------------------
		private void Init_Updater()
		{
			try
			{
				Init_Updater_internal();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void Init_Updater_internal()
		{
			if (!_config.WelcomeScreenDisabled)
				return;
			if (_config.DisableUpdate)
				return;

			_updater = new Updater();
			_updater.PollingIntervalInSeconds = 0; // only check once at program start
			_updater.Logfile                  = "updater.log";
			_updater.CurrentVersion           = AppVersion.Version.VERSION;
			_updater.RepositoryURL            = @"https://www.abraham-beratung.de/aoop/version.html";
            _updater.DownloadLinkStart        = "https:\\/\\/www\\.abraham-beratung\\.de\\/aoop";
            _updater.DownloadLinkEnd          = "zip";
			_updater.DestinationDirectory     = _applicationData.ProgramDirectory;
			_updater.OnUpdateAvailable        = delegate()
			{
				_updater.Stop();
				Dispatcher.BeginInvoke( new Action(() =>
				{
					Show_Update_notification();
				}));
			};
			
			if (!_config.DisableUpdate)
				_updater.Start();
		}

		private void Show_Update_notification()
		{
			try
			{
				Show_Update_notification_internal();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void Show_Update_notification_internal()
		{
			var text = _texts.Data[HelpTexts.ID.UPDATEAVAILABLE];
			var fullText = string.Format(text, AppVersion.Version.VERSION, _updater.NewVersion);

			var wnd = new MessageBoxWindow(this);
            wnd.Title = _texts.Data[HelpTexts.ID.UPDATETITLE];
			wnd.checkboxDontShowAgain.IsChecked = true;
			wnd.ContentBox.Text = fullText;
			wnd.PanelOk.Visibility = Visibility.Hidden;
			wnd.PanelYesNo.Visibility = Visibility.Visible;
			wnd.ShowDialog();
			if (wnd.MessageBoxResult == MessageBoxResult.Yes)
			{
				bool success = _updater.StartUpdate();
				_updater.Stop();
				Close();
			}
			else
			{
				_updater.Stop();
			}
		}
        #endregion
        #region ------------- Home automation server connections --------------
        /// <summary>
        /// This will connect every connector in the list, then call Startup2.
        /// Afterwards it will go into and endless loop, and reconnect every connector that has lost its connection.
        /// </summary>
        private async Task InitAndReconnectConnectorsLoop()
        {
            foreach(var connector in _connectors)
            {
                if (!connector.IsConnected)
                {
                    ServerInfo.Content = $"Connecting to {connector.Name}...";
                    FadeInServerInfo();
                    await connector.Connect(_config);
                    LinkConnector(connector);
                }
            }

            _endTheReconnectorLoop = false;
            WaitAndThenCallMethod(wait_time_seconds: 1, action: Startup2);
            WaitAndThenCallMethod(wait_time_seconds: 10, action: ReconnectLoop);
        }

        private void ReconnectLoop()
        {
            foreach (var connector in _connectors)
            {
                if (!connector.IsConnected && !connector.ConnectionIsInProgress)
                {
                    ServerInfo.Content = $"Reconnecting to {connector.Name}...";
                    FadeInServerInfo();
                    connector.Reconnect();
                }
            }

            if (!_endTheReconnectorLoop)
                WaitAndThenCallMethod(wait_time_seconds: 30, action: ReconnectLoop);
        }

        private void StopAllConnectors()
        {
            foreach(var connector in _connectors)
                connector.Stop();
        }

        private void LinkConnector(IConnector connector)
        {
            try
            {
                if (connector.Name == "MQTT")
                    _applicationData._mqttGetter = connector.Getter;
                else
                    _applicationData._homenetGetter = connector.Getter;

                connector.OnDataobjectChange += 
                    delegate(ServerDataObjectChange Do)
                    {
                        Dispatcher.Invoke(() => { Update_all_modules(Do); });
                    };

                ServerInfo.Content = connector.ConnectionStatus;
                FadeOutServerInfo();
            }
            catch (Exception ex)
            {
                ServerInfo.Content = $"Error connecting to {connector.Name}";
            }
        }
        #endregion
        #endregion
    }
}
