using Abraham.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Abraham.AutoUpdater
{
	/// <summary>
	/// Monitor a source directory and update the program automatically.
	/// </summary>
	/// 
	/// <remarks>
	/// This class installs a thread that monitors a given URL permanently.
	/// If so, it generates and calls a separate updater process to update all program files.
	/// 
	/// 
	/// UPDATE ALGORITHM:
	/// 1) new files are copied into bin bin subfolder of the source directory.
	/// 2) a new file "Force_application_close.dat" is created in source directory.
	/// 3) The updater checks periodically for the existence of "Force_application_close.dat"
	/// 4) The updater gives the signal to end the application.
	/// 5) The application can ask the user if he wants to update
	/// 6) If so, application calls the the updater method "StartUpdate"
	/// 7) Updater creates and starts a new batch file. 
	/// 8) Several parameters are passed to the batch file, also the command line
	///    parameters the application was originally started with.
	/// 9) Application ends
	/// 10) Batch file waits until the application has ended. (after 10 seconds, he kills the app)
	/// 11) Batch file copies the new files from "bin" to the destination directory.
	/// 12) Batch file deletes "Force_application_close.dat".
	/// 13) Batch file restarts the application.
	/// 
	/// </remarks>
	public class Updater
    {
        #region ------------- Properties ----------------------------------------------------------

        public string  CurrentVersion { get; set; }

        /// <summary>
        /// The directory to pull updates from
        /// </summary>
        public string RepositoryURL { get; set; }

        /// <summary>
        /// The beginning, fixed part of the download link on the web page
        /// </summary
		public string DownloadLinkStart { get; set; }

        /// <summary>
        /// The ending part of the download link on the web page
        /// </summary
		public string DownloadLinkEnd { get; set; }

		/// <summary>
		/// The directory to copy updates to
		/// </summary>
		public string DestinationDirectory { get; set; } = ".";

        /// <summary>
        /// Our own process name. The updater batch must know this name 
        /// because it waits for us ending.
        /// </summary>
        public string OwnProcessName { get; set; }

        /// <summary>
        /// Defines the apperance of the updater batch window.
        /// This can be set to ProcessWindowStyle.Minimized so do the update in background.
        /// </summary>
        public ProcessWindowStyle UpdaterWindowStyle { get; set; } = ProcessWindowStyle.Normal;

        /// <summary>
        /// Seconds after which the polling is repeated.
        /// If this value is 0, update only makes one check
        /// </summary>
        public int PollingIntervalInSeconds { get; set; }

        /// <summary>
        /// When Update calls this method, the main program has to end itself
        /// </summary>
        public delegate void OnEndProgram_Handler();

        /// <summary>
        /// When Update calls this method, the main program has to end itself
        /// </summary>
        public OnEndProgram_Handler OnUpdateAvailable {  get; set; } = delegate() { };

        /// <summary>
        /// Delegate to inform the main program about errors
        /// </summary>
        public delegate void OnError_Handler(string errorInfo);

        /// <summary>
        /// Delegate to inform the main program about errors
        /// </summary>
        public OnError_Handler OnError {  get; set; } = delegate(string errorInfo) { };
		
        /// <summary>
        /// Arguments that are passed to the restarted main program after update
        /// </summary>
        public string[] CommandLineArguments { get; set; }

        public DateTime LastWriteTimeOfPreviousUpdate { get; private set; }
		
        /// <summary>
        /// Contains the newest version that could be found on the update homepage
        /// </summary>
        public string NewVersion { get; set; }

        /// <summary>
        /// Filename of the logfile, that can be useful to find problems on a customers' machine
        /// If it is not null or empty, updater logs his activity to this file
        /// </summary>
		public string Logfile { get; set; }

        /// <summary>
        /// Must contain the regular expression used to extract the version number from the download link
        /// </summary>
		public string DownloadLinkRegex { get; set; } = "[0-9]+-[0-9]+-[0-9]+";

		#endregion



		#region ------------- Fields --------------------------------------------------------------
		private int _httpTimeoutInSeconds = 60;
		private Abraham.Threading.ThreadExtensions _Thread;
        private DateTime _LastWriteTimeOfAll;
        private string _VersionInfoFilename;
		private string _DownloadUrl;
		#endregion



		#region ------------- Init ----------------------------------------------------------------
		public Updater()
        {
            PollingIntervalInSeconds = 60 * 60; // every hour
            OwnProcessName = Process.GetCurrentProcess().ProcessName + ".exe";
        }
        #endregion



        #region ------------- Methods -------------------------------------------------------------
        public void Start()
        {
            Log($"");
            Log($"");
            Log($"");
            Log($"Updater started");
            _VersionInfoFilename = DestinationDirectory + Path.DirectorySeparatorChar + "version";

            _Thread = new ThreadExtensions(UpdaterThreadProc, "UpdaterThread");
            _Thread.Timeout_Seconds = 2;
            _Thread.thread.Start();
        }

        public void Stop()
        {
            Log($"Updater stopped");
            if (_Thread != null && _Thread.Run)
                _Thread.SendStopSignalAndWait();
        }

        public bool StartUpdate()
        {
            if (!SearchForDownloadUrlOnHomepage())
                return false;
            if (!DownloadInstallPackageFromHomepage())
                return false;
            if (!CreateUpdaterBatch())
                return false;
            return StartUpdater();
        }

        public string SearchForNewerVersionOnHomepage()
        {
            Log($"SearchForNewerVersionOnHomepage: manual request");
            if (SearchForDownloadUrlOnHomepage())
			{
                var newVersion = ExtractVersionNumberOutOfLink(_DownloadUrl);
                if (newVersion.CompareTo(CurrentVersion) > 0)
				{
                    NewVersion = newVersion;
                    return NewVersion;
				}
			}
            return null;
        }
		#endregion



		#region ------------- Implementation ------------------------------------------------------
		private void UpdaterThreadProc()
        {
            Log("UpdaterThreadProc: Starting update check loop");

			while (_Thread.Run)
			{
				try
				{
					if (UpdateNotificationExists())
						OnUpdateAvailable();
				}
				catch (Exception ex)
				{
					Log("UpdaterThreadProc: " + ex.ToString());
				}

                if (PollingIntervalInSeconds == 0)
				{
                    Log("UpdaterThreadProc: Exiting update check loop after the first check because PollingInterval is not set");
                    break;
				}

				for (int second = 0; second < PollingIntervalInSeconds; second++)
				{
					_Thread.Sleep(1000);
					if (!_Thread.Run)
						break;
				}
			}
            Log("UpdaterThreadProc: Exiting update check loop");
		}

        private bool UpdateNotificationExists()
        {
            try
            {
                if (SearchForDownloadUrlOnHomepage())
				{
                    var newVersion = ExtractVersionNumberOutOfLink(_DownloadUrl);
                    if (newVersion.CompareTo(CurrentVersion) > 0)
				    {
                        NewVersion = newVersion;
                        return true;
				    }
				}
            }
            catch (Exception ex)
            {
                var message = $"Exception! {ex.ToString()}";
                Log(message);
    		    OnError(message);
            }
            return false;
        }   

		private bool SearchForDownloadUrlOnHomepage()
		{
            Log($"SearchForDownloadUrlOnHomepage - reading '{RepositoryURL}' with timeout {_httpTimeoutInSeconds} seconds");

            string html = "";
            Task<string> task = null;
            try
			{
                var client = new HttpClient();
			    task = client.GetStringAsync(RepositoryURL);
                var success = task.Wait(_httpTimeoutInSeconds * 1000);
			    if (!success && task == null)
			    {
                    Log($"SearchForDownloadUrlOnHomepage: reading failed");
				    return false;
			    }
                html = task.Result;
			}
            catch (Exception ex)
			{
                if (ex.ToString().Contains("404"))
                {
                    Log($"SearchForDownloadUrlOnHomepage: reading failed, page not found (404)");
				    return false;
			    }
				else
				{
                    Log($"SearchForDownloadUrlOnHomepage: server error: " + ex.ToString());
				    return false;
				}
			}

            try
			{
                var placeholder = "[0-9a-zA-Z_\\-\\/]*\\.";
                var regex = $"<a href=\\{"\""}{DownloadLinkStart}{placeholder}{DownloadLinkEnd}";
                var match = Regex.Match(html, regex);
                if (match == null || !match.Success)
			    {
                    Log($"SearchForDownloadUrlOnHomepage: no download link found on page");
                    return false;
			    }
                _DownloadUrl = match.Value.Substring( "<a href=".Length+1 );

                Log($"SearchForDownloadUrlOnHomepage: download link found on page: '{_DownloadUrl}'");
            }
            catch (Exception ex)
			{
                Log($"SearchForDownloadUrlOnHomepage: parsing error: " + ex.ToString());
				return false;
			}
            return true;
		}

        private string ExtractVersionNumberOutOfLink(string html)
        {
            Log($"ExtractVersionNumberOutOfLink: '{RepositoryURL}'");

            var match = Regex.Match(html, DownloadLinkRegex);
            if (match == null || !match.Success)
			{
                Log($"SearchForDownloadUrlOnHomepage: no download link found on page");
                return "";
			}
            var version = match.Value;
            return version;


            ////var parts = DownloadLinkRegex.Split(new char[] {'*'}, StringSplitOptions.RemoveEmptyEntries);
            //
            //
            //int start = html.IndexOf("<a href=");
            //if (start >= 0)
			//{
            //    int end = html.IndexOf("</a>", start);
            //    if (end <= start)
            //        end = html.IndexOf("<", start);
            //    if (end > start)
			//	{
            //        start += "<a href=".Length;
            //
            //        int start2 = html.IndexOf("LongTermArchive_");
            //        if (start2 >= 0)
			//		{
            //            start2 += "LongTermArchive_".Length;
            //            int end2   = html.IndexOf(".zip", start2);
            //            if (end2 > start2)
			//		    {
            //                var version = html.Substring(start2, end2-start2).Trim();
            //                Log($"ExtractVersionNumberOutOfLink: Found '{version}'");
            //                return version;
            //            }
			//		}
			//	}
			//}

            Log($"ExtractVersionNumberOutOfLink: no version information found");
            return "";
        }

		private bool DownloadInstallPackageFromHomepage()
		{
            Log($"DownloadInstallPackageFromHomepage");

            var client = new HttpClient();
			var task = client.GetByteArrayAsync(_DownloadUrl);
            var success = task.Wait(_httpTimeoutInSeconds * 1000);
			if (!success && task == null)
			{
                Log($"Cannot download '{_DownloadUrl}'");
                return false;
			}
            
            var bytes = task.Result;
            if (bytes == null || bytes.Length == 0)
			{
                Log($"Cannot download, zero length '{_DownloadUrl}'");
                return false;
			}

            Directory.CreateDirectory("Update");
            File.WriteAllBytes(@"Update\updatepackage.zip", bytes);

            Log($"File successfully downloaded: '{_DownloadUrl}'");
            return true;
		}

        private bool CreateUpdaterBatch()
        {
            try
            {
                string CommandLineArgumentsAsString = (CommandLineArguments != null) 
                    ? string.Join(" ", CommandLineArguments)
                    : "";

                string Updater = $@"
@echo off
@ECHO -----------------------------------------------------------------------------------
@ECHO                       D E P L O Y     S C R I P T
@ECHO -----------------------------------------------------------------------------------
cd ""{Directory.GetCurrentDirectory()}""



@set SOURCE=update\updatepackage.zip
@set DESTINATION="       + DestinationDirectory         + @"
@set PROCESS_NAME="      + OwnProcessName               + @"
@set COMMANDLINEARGS="   + CommandLineArgumentsAsString + @"


@ECHO -----------------------------------------------------------------------------------
@ECHO Script to install a deploy of a new version on the client
@ECHO Source dir       : %SOURCE% 
@ECHO Destination dir  : %DESTINATION%
@ECHO Own processname  : %PROCESS_NAME%
@ECHO CmdLine Arguments: %COMMANDLINEARGS%
@ECHO -----------------------------------------------------------------------------------


@ECHO .
@ECHO .
@ECHO -----------------------------------------------------------------------------------
@ECHO unziping update package...
@ECHO -----------------------------------------------------------------------------------
pushd update
powershell  Expand-Archive  -Force  updatepackage.zip  -DestinationPath . 
popd


@ECHO .
@ECHO .
@ECHO -----------------------------------------------------------------------------------
@ECHO killing the process, if it still runs
@ECHO -----------------------------------------------------------------------------------
FOR /L %%i IN (1,1,10) DO (
    tasklist /FI " + "\"IMAGENAME eq " + OwnProcessName + "\"" + @" 2>NUL | find /I /N " + "\"" + OwnProcessName + @"\"">NUL
    if " + "\"%ERRORLEVEL%\" == \"0\" " + @" goto Process_has_ended
    @CHOICE /C:jc /N /CS /T 2 /D j /M ""Waiting 10 seconds for process end, press c to continue""
)
@ECHO ""Process didn't end within the time, now killing the process...""
taskkill /IM %PROCESS_NAME%

:Process_has_ended


@ECHO .
@ECHO .
@ECHO -----------------------------------------------------------------------------------
@ECHO copying files...
@ECHO -----------------------------------------------------------------------------------
xcopy   ""update\bin\publish\*""   ""%DESTINATION%""  /Y /s /D     >NUL


@ECHO .
@ECHO .
@ECHO -----------------------------------------------------------------------------------
@ECHO restarting the process
@ECHO -----------------------------------------------------------------------------------
cd ""%DESTINATION%""
start %PROCESS_NAME%   %COMMANDLINEARGS%


@ECHO .
@ECHO .
@ECHO -----------------------------------------------------------------------------------
@ECHO deleting temporary files...
@ECHO -----------------------------------------------------------------------------------
del   update\bin\publish\*  /F /Q /S    >NUL
rd    update\bin\publish                >NUL

exit

:end
";
                if (!Directory.Exists("Update"))
                    Directory.CreateDirectory("Update");

                File.WriteAllText(@"Update\updater.cmd", Updater);
                return true;
            }
            catch (Exception ex)
            {
    		    OnError($"Exception! {ex.ToString()}");
                return false;
            }
        }

        private bool StartUpdater()
        {
            LastWriteTimeOfPreviousUpdate = _LastWriteTimeOfAll;
            SaveDataToFile();
            return StartProcess("cmd.exe", $@"/k ""{Directory.GetCurrentDirectory()}\\update\\updater.cmd"" ");
        }

        private bool StartProcess(string program, string arguments)
        {
            try
            {
                Log($"Starting updater process '{program} {arguments}'");

                var ProcStartInfo = new ProcessStartInfo(program, arguments);
                ProcStartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
                ProcStartInfo.WindowStyle = UpdaterWindowStyle;
                ProcStartInfo.Verb = "runas";
                ProcStartInfo.UseShellExecute = true;
                Process.Start(ProcStartInfo);
                Log($"Successful");
                return true;
            }
            catch (Exception ex)
            {
                var message = $"Exception! {ex.ToString()}";
                Log(message);
    		    OnError(message);
                return false;
            }
        }

        private string Read_version_from_HTML_page()
        {
            Log($"Read_version_from_HTML_page: '{RepositoryURL}'");

            var client = new HttpClient();
			var task = client.GetStringAsync(RepositoryURL);
            var success = task.Wait(_httpTimeoutInSeconds * 1000);
			if (!success && task == null)
				return "";
            
            var html = task.Result;
            if (!html.Contains("<a href="))
			{
                Log($"Read_version_from_HTML_page: no link found");
                return "";
			}

            int start = html.IndexOf("<a href=");
            if (start >= 0)
			{
                int end = html.IndexOf("</a>", start);
                if (end <= start)
                    end = html.IndexOf("<", start);
                if (end > start)
				{
                    start += "<a href=".Length;

                    int start2 = html.IndexOf("LongTermArchive_");
                    if (start2 >= 0)
					{
                        start2 += "LongTermArchive_".Length;
                        int end2   = html.IndexOf(".zip", start2);
                        if (end2 > start2)
					    {
                            var version = html.Substring(start2, end2-start2).Trim();
                            Log($"Read_version_from_HTML_page: Found '{version}'");
                            return version;
                        }
					}
				}
			}

            Log($"Read_version_from_HTML_page: no link found");
            return "";
        }

        private void SaveDataToFile()
        {
            File.WriteAllText("updater.config", LastWriteTimeOfPreviousUpdate.Ticks.ToString());
        }
        #endregion



		#region ------------- Logging -----------------------------------------
        private void Log(string message)
        {
            try
			{
                File.AppendAllText(Logfile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "   " + message + "\n");
			}
            catch (Exception)
			{
			}
        }
        #endregion
    }
}
