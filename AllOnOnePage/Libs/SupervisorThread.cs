using System;
using System.Collections.Generic;
using System.Text;

namespace AllOnOnePage.Libs
{
	class SupervisorThread
	{
		#region ------------- Supervisor thread -------------------------------

        //private ThreadExtensions _SupervisorThread;
        
		//private void StartSupervisorThread()
        //{
        //    _SupervisorThread = new ThreadExtensions(SupervisorThreadProc);
        //    _SupervisorThread.thread.Start();
        //}
        //
        //private void StopSupervisorThread()
        //{
        //    _SupervisorThread?.SendStopSignalAndWait();
        //}
        //
        //private void SupervisorThreadProc()
        //{
        //    SSLog("starting");
        //    do
		//	{
		//		try
		//		{
        //            //SSLog($"Connection state: {_VM.CurrentConnectionState.Value}");
        //            _SupervisorThread.Sleep(60 * 1 * 1000);
		//		}
		//		catch (Exception ex)
		//		{
		//			SSLog($"SupervisorThread Exception: {ex.ToString()}");
        //            _SupervisorThread.Sleep(10 * 1 * 1000);
		//		}
		//	}
		//	while (_SupervisorThread.Run);
        //    SSLog("ended");
        //}
        //
        //private void SSLog(string message)
        //{
        //    if (_Logging)
        //        return;
        //    
        //    _Logging = true;
        //    try
        //    {
        //        File.AppendAllText("supervisorthread.log", $"{DateTime.Now} - {message}\n");
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine(ex.ToString());
        //    }
        //    finally
        //    {
        //        _Logging = false;
        //    }
        //}

        #endregion
	}
}
