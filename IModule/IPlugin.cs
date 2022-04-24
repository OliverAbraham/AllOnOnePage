using System;

namespace AllOnOnePage.Plugins
{
	public interface IPlugin
	{
		public void OnEvent(object sender, PluginEventArgs e);
	}

	public enum PluginEventType
	{
		Init,
		Start,
		Stop,
		Time,
	}

	public class PluginEventArgs
	{
        #region Incoming data
		public PluginEventType Type { get; set; }
        #endregion

        #region Outgoing data
		#endregion
	}

	public class InitEventArgs : PluginEventArgs
	{
        #region Incoming data
        #endregion

        #region Outgoing data
		#endregion

		public InitEventArgs()
        {
        }

        public InitEventArgs(PluginEventType type)
        {
            Type = type;
        }
	}

	public class StartEventArgs : PluginEventArgs
	{
	}

	public class StopEventArgs : PluginEventArgs
	{
	}

	public class TimeEventArgs : PluginEventArgs
	{
        #region Incoming data
		public DateTime Time { get; set; }
        #endregion

        #region Outgoing data
		#endregion

        public TimeEventArgs(DateTime time)
        {
            Type = PluginEventType.Time;
            Time = time;
        }
	}
}
