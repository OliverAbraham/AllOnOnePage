using System.Runtime.InteropServices;

namespace AllOnOnePage.Plugins
{
    internal static class MonitorPower
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint SetThreadExecutionState(uint esFlags);

        private const uint ES_DISPLAY_REQUIRED = 0x00000002;

        public static void SwitchOnDisplayIfOff()
        {
            SetThreadExecutionState(ES_DISPLAY_REQUIRED);
        }
    }
}
