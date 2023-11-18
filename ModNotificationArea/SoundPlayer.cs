using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AllOnOnePage.Plugins
{
    public class SoundPlayer
	{
		private string _command;
		private bool isOpen;
		[DllImport("winmm.dll")]

		private static extern long mciSendString(string strCommand, StringBuilder strReturn, int iReturnLength, IntPtr hwndCallback);

		public void Play(string filename)
		{
			if (!File.Exists(filename))
				throw new Exception($"Soundfile does not exist: '{filename}'");
            Close();
            Open(filename);
            Play(loop:false);
		}

		public void Close()
		{
			if (isOpen)
			{
				_command = "close MediaFile";
				mciSendString(_command, null, 0, IntPtr.Zero);
				isOpen = false;
			}
		}

		public void Open(string sFileName)
		{
			_command = "open \"" + sFileName + "\" type mpegvideo alias MediaFile";
			mciSendString(_command, null, 0, IntPtr.Zero);
			isOpen = true;
		}

		public void Play(bool loop = false)
		{
			if (isOpen)
			{
				_command = "play MediaFile";
				if (loop)
					_command += " REPEAT";
				mciSendString(_command, null, 0, IntPtr.Zero);
			}
		}
	}
}
