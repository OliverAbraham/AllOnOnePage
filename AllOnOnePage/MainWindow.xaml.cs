using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Abraham.AutoUpdater;
using Abraham.HomenetBase.Connectors;
using Abraham.OpenWeatherMap;
using AllOnOnePage.DialogWindows;
using AllOnOnePage.Libs;
using AllOnOnePage.Plugins;

namespace AllOnOnePage
{
    public partial class MainWindow : Window
    {
        private const string VERSION = "2023-10-10";

		#region ------------- Fields --------------------------------------------------------------
		#region Configuration
        private ConfigurationManager   _configurationManager;
        private Configuration          _config => _configurationManager.Config;
   		private ApplicationData        _applicationData;
		private HelpTexts              _texts;
        private LayoutManager          _layoutManager;
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
		private Updater _Updater;
        #endregion

		#region Home automation server connection
        private bool _connectingToServerInProgress;
		#endregion
        #endregion



        #region ------------- Init ----------------------------------------------------------------
        public MainWindow()
		{
			try
			{
                OpenWeatherMapConnector connector = new OpenWeatherMapConnector();
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
			Init_Updater();
            WaitAndThenCallMethod(wait_time_seconds:1, action:Startup);
		}

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			CleanupPlugins();
			//StopSupervisorThread();
			Stop_date_time_update_timer();
			//Stop_periodic_UI_update_timer();
            if (_Updater is not null) _Updater.Stop();
			_configurationManager.Save();
		}

		private void Window_Closed(object sender, EventArgs e)
        {
            //StopPowerManagement();
            //_programStateManager.Save_state_to_disk();
            //StopSupervisorThread();
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
            if (!string.IsNullOrWhiteSpace(_config.HomeAutomationServerUrl))
                ConnectToServer_then_call_Startup2();
            else
                Startup2();
		}

        private void Startup2()
        {
            try
            {
                //StartPowerManagement();
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
            _Updater.Stop();
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
        #region ------------- Server connection -------------------------------
        private void ConnectToServer_then_call_Startup2()
        {
            _connectingToServerInProgress = true;
            ServerInfo.Content = "Connecting to server...";
            WaitAndThenCallMethod(wait_time_seconds: 1, action: ConnectToHomeAutomationServer);
        }

        private void ConnectToHomeAutomationServer()
        {
            try
            {
                _applicationData._homenetConnector = new DataObjectsConnector(
                    _config.HomeAutomationServerUrl, 
                    _config.HomeAutomationServerUser, 
                    _config.HomeAutomationServerPassword, 
                    _config.HomeAutomationServerTimeout);
                ServerInfo.Content = "Connected";
                FadeOutServerInfo();
            }
            catch (Exception ex)
            {
                ServerInfo.Content = "Error connecting to homenet server";
                WaitAndThenCallMethod(wait_time_seconds: 30, action: ReconnectToHomeAutomationServer);
            }
            finally
            {
                _connectingToServerInProgress = false;
                Startup2();
            }
        }

        private void ReconnectToHomeAutomationServer()
        {
            ServerInfo.Content = "Reconnecting...";
            WaitAndThenCallMethod(wait_time_seconds: 1, action: ReconnectToHomeAutomationServer2);
        }

        private void ReconnectToHomeAutomationServer2()
        {
            try
            {
                _applicationData._homenetConnector = new DataObjectsConnector(
                    _config.HomeAutomationServerUrl, 
                    _config.HomeAutomationServerUser, 
                    _config.HomeAutomationServerPassword, 
                    _config.HomeAutomationServerTimeout);
                ServerInfo.Content = "Connected";
                FadeOutServerInfo();
            }
            catch (Exception ex)
            {
                ServerInfo.Content = "Error connecting to homenet server";
                WaitAndThenCallMethod(wait_time_seconds: 30, action: ReconnectToHomeAutomationServer);
            }
        }
        #endregion
        #region ------------- Main window layout ------------------------------
		private void Init_LayoutManager()
		{
			_layoutManager = new LayoutManager(window: this, key: "MainWindow");
		}

		private void Stop_LayoutManager()
		{
			_layoutManager.Save();
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
        #region ------------- Periodic time update ----------------------------
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

        private void Update_all_modules()
        {
            if (_nowUpdating)
                return;
            _nowUpdating = true;
            try
            {
                _vm.Update_all_modules();
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
            VersionInfo.Content = $"Version {VERSION}";
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
			WpfAnimations.FadeOutLabel(HelpText6);
			WpfAnimations.FadeOutLabel(HelpText7);
		}

        private void FadeOutVersionInfo()
        {
            WaitAndThenCallMethod(wait_time_seconds:10, action: () => WpfAnimations.FadeOutLabel(VersionInfo));			
        }

        private void FadeOutServerInfo()
        {
            WaitAndThenCallMethod(wait_time_seconds:10, action: () => WpfAnimations.FadeOutLabel(ServerInfo));			
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
			WpfAnimations.FadeIn(Button_Wastebasket, UIElement.OpacityProperty);
			WpfAnimations.FadeIn(Button_EditMode   , UIElement.OpacityProperty);
		}

		private void FadeOutButtons()
        {
			WpfAnimations.FadeOutButLeaveVisible(Button_Close      , UIElement.OpacityProperty);
			WpfAnimations.FadeOutButLeaveVisible(Button_Add        , UIElement.OpacityProperty);
			WpfAnimations.FadeOutButLeaveVisible(Button_Fullscreen , UIElement.OpacityProperty);
			WpfAnimations.FadeOutButLeaveVisible(Button_Info       , UIElement.OpacityProperty);
			WpfAnimations.FadeOutButLeaveVisible(Button_Settings   , UIElement.OpacityProperty);
			WpfAnimations.FadeOutButLeaveVisible(Button_Wastebasket, UIElement.OpacityProperty);
			WpfAnimations.FadeOutButLeaveVisible(Button_EditMode   , UIElement.OpacityProperty);
		}
		#endregion
		#region ------------- Visual editor -----------------------------------
		private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_Edit_Click(object sender, RoutedEventArgs e)
        {
            bool editModeOn = _vm.Button_Edit_Click();
            if (editModeOn)
                WpfAnimations.FadeIn(Button_Add, UIElement.OpacityProperty);
            else
                WpfAnimations.FadeOutButLeaveVisible(Button_Add,  UIElement.OpacityProperty);
        }

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

		private void Button_Wastebasket_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
            _vm.Button_Wastebasket_Click();
		}

		private void Button_Editmode_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
            _vm.Button_Editmode_Click();
		}

		private void Button_Plus_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
            _vm.AddNewModule();
		}
		#endregion
        #region ------------- UI background -----------------------------------
        private void Init_Background_Size_and_Position()
		{
            EditBackground.RestoreSelectedBackground(this, _config);
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
        #region ------------- Fullscreen button -------------------------------
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
		#endregion
        #region ------------- Program info ------------------------------------
		private void Button_Info_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
            var wnd = new ProgramInfo(_texts, VERSION, _Updater);
            wnd.Owner = this;
            wnd.ShowDialog();
		}
		#endregion
        #region ------------- Settings ----------------------------------------
		private void Button_Settings_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
            var wnd = new EditSettings(_config);
            wnd.Owner = this;
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

			_Updater = new Updater();
			_Updater.PollingIntervalInSeconds = 0; // only check once at program start
			_Updater.Logfile                  = "updater.log";
			_Updater.CurrentVersion           = VERSION;
			_Updater.RepositoryURL            = @"https://www.abraham-beratung.de/aoop/version.html";
            _Updater.DownloadLinkStart        = "https:\\/\\/www\\.abraham-beratung\\.de\\/aoop";
            _Updater.DownloadLinkEnd          = "zip";
			_Updater.DestinationDirectory     = _applicationData.ProgramDirectory;
			_Updater.OnUpdateAvailable        = delegate()
			{
				_Updater.Stop();
				Dispatcher.BeginInvoke( new Action(() =>
				{
					Show_Update_notification();
				}));
			};
			
			if (!_config.DisableUpdate)
				_Updater.Start();
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
			var fullText = string.Format(text, VERSION, _Updater.NewVersion);

			var wnd = new MessageBoxWindow(this);
            wnd.Title = _texts.Data[HelpTexts.ID.UPDATETITLE];
			wnd.checkboxDontShowAgain.IsChecked = true;
			wnd.ContentBox.Text = fullText;
			wnd.PanelOk.Visibility = Visibility.Hidden;
			wnd.PanelYesNo.Visibility = Visibility.Visible;
			wnd.ShowDialog();
			if (wnd.MessageBoxResult == MessageBoxResult.Yes)
			{
				bool success = _Updater.StartUpdate();
				_Updater.Stop();
				Close();
			}
			else
			{
				_Updater.Stop();
			}
		}
        #endregion

        #endregion
    }
}
