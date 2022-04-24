using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AllOnOnePage
{
	internal class WindowsPowermanagement
	{
        #region Interop services
        public const uint ES_CONTINUOUS = 0x80000000;
        public const uint ES_SYSTEM_REQUIRED = 0x00000001;
        public const uint ES_DISPLAY_REQUIRED = 0x00000002;

        private const int PBT_APMPOWERSTATUSCHANGE = 10;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SetThreadExecutionState([In] uint esFlags);

        private static bool DisplayRequired = false;
        private HwndSource _WindowSource;

        public static void DisableScreensaverAndHibernation()
        {
            DisplayRequired = true;
            //SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED);
        }

        public static void EnableScreensaverAndHibernation()
        {
            if (DisplayRequired)
            {
                DisplayRequired = false;
                //SetThreadExecutionState(ES_CONTINUOUS);
            }
        }

        public WindowsPowermanagement(Window mainWindow)
        {
            _WindowSource = (HwndSource)PresentationSource.FromVisual(mainWindow);
            var handle = _WindowSource.Handle;
            _WindowSource.AddHook(WndProc);
        }

        public void Close()
        {
            _WindowSource.RemoveHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_POWERBROADCAST)
            {
                if ((int)wParam == PBT_APMPOWERSTATUSCHANGE)
                {
                    var power = GetSystemPowerStatus();
                    File.AppendAllText("power.log", 
                        $"{DateTime.Now} - WM_POWERBROADCAST / PBT_APMPOWERSTATUSCHANGE: " +
                        $"BatteryFlag: {power._BatteryFlag} " +
                        $"BatteryFullLifeTime: {power._BatteryFullLifeTime} " + 
                        $"BatteryLifePercent: {power._BatteryLifePercent} " + 
                        $"BatteryLifeTime: {power._BatteryLifeTime}\n");
                }
                else
                {
                    File.AppendAllText("power.log", $"{DateTime.Now} - WM_POWERBROADCAST: wParam = {wParam} lParam = {lParam}\n");
                }
            }
            handled = false;
            return (IntPtr)0;
        }

        [DllImport("Kernel32")]
        private static extern Boolean GetSystemPowerStatus( SystemPowerStatus sps );

        public static SystemPowerStatus GetSystemPowerStatus()
        {
            SystemPowerStatus sps = new SystemPowerStatus();
            GetSystemPowerStatus( sps );
            return sps;
        }
        
		#endregion
	}





    class NativeMethods
    {
        internal const uint DEVICE_NOTIFY_WINDOW_HANDLE = 0x0;
        internal const uint DEVICE_NOTIFY_SERVICE_HANDLE = 0x1;
        internal const int WM_POWERBROADCAST = 0x0218;
        internal const int PBT_POWERSETTINGCHANGE = 0x8013;

        [DllImport("User32.dll", SetLastError = true)]
        internal static extern IntPtr RegisterPowerSettingNotification(IntPtr hWnd, [In] Guid PowerSettingGuid, uint Flags);

        [DllImport("User32.dll", SetLastError = true)]
        internal static extern bool UnregisterPowerSettingNotification(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct POWERBROADCAST_SETTING
        {
            public Guid PowerSetting;
            public uint DataLength;
            public byte Data;
        }

        // https://docs.microsoft.com/en-us/windows/win32/power/power-setting-guids
        public class PowerSettingGuid
        {
            // 0=Powered by AC, 1=Powered by Battery, 2=Powered by short-term source (UPC)
            public Guid AcdcPowerSource { get; } = new Guid("5d3e9a59-e9D5-4b00-a6bd-ff34ff516548");
            // POWERBROADCAST_SETTING.Data = 1-100
            public Guid BatteryPercentageRemaining { get; } = new Guid("a7ad8041-b45a-4cae-87a3-eecbb468a9e1");
            // Windows 8+: 0=Monitor Off, 1=Monitor On, 2=Monitor Dimmed
            public Guid ConsoleDisplayState { get; } = new Guid("6fe69556-704a-47a0-8f24-c28d936fda47");
            // Windows 8+, Session 0 enabled: 0=User providing Input, 2=User Idle
            public Guid GlobalUserPresence { get; } = new Guid("786E8A1D-B427-4344-9207-09E70BDCBEA9");
            // 0=Monitor Off, 1=Monitor On.
            public Guid MonitorPowerGuid { get; } = new Guid("02731015-4510-4526-99e6-e5a17ebd1aea");
            // 0=Battery Saver Off, 1=Battery Saver On.
            public Guid PowerSavingStatus { get; } = new Guid("E00958C0-C213-4ACE-AC77-FECCED2EEEA5");

            // Windows 8+: 0=Off, 1=On, 2=Dimmed
            public Guid SessionDisplayStatus { get; } = new Guid("2B84C20E-AD23-4ddf-93DB-05FFBD7EFCA5");

            // Windows 8+, no Session 0: 0=User providing Input, 2=User Idle
            public Guid SessionUserPresence { get; } = new Guid("3C0F4548-C03F-4c4d-B9F2-237EDE686376");
            // 0=Exiting away mode 1=Entering away mode
            public Guid SystemAwaymode { get; } = new Guid("98a7f580-01f7-48aa-9c0f-44352c29e5C0");

            /* Windows 8+ */
            // POWERBROADCAST_SETTING.Data not used
            public Guid IdleBackgroundTask { get; } = new Guid(0x515C31D8, 0xF734, 0x163D, 0xA0, 0xFD, 0x11, 0xA0, 0x8C, 0x91, 0xE8, 0xF1);

            public Guid PowerSchemePersonality { get; } = new Guid(0x245D8541, 0x3943, 0x4422, 0xB0, 0x25, 0x13, 0xA7, 0x84, 0xF6, 0x79, 0xB7);

            // The Following 3 Guids are the POWERBROADCAST_SETTING.Data result of PowerSchemePersonality
            public Guid MinPowerSavings { get; } = new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
            public Guid MaxPowerSavings { get; } = new Guid("a1841308-3541-4fab-bc81-f71556f20b4a");
            public Guid TypicalPowerSavings { get; } = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");
        }
    }


    public enum ACLineStatus : byte
    {
        Offline = 0, 
        Online = 1, 
        Unknown = 255
    }

    public enum BatteryFlag : byte
    {
        High = 1,
        Low = 2,
        Critical = 4,
        Charging = 8,
        NoSystemBattery = 128,
        Unknown = 255
    }

    // Fields must mirror their unmanaged counterparts, in order
    [StructLayout( LayoutKind.Sequential )]
    public class SystemPowerStatus
    {
        public ACLineStatus _ACLineStatus;
        public BatteryFlag  _BatteryFlag;
        public Byte     _BatteryLifePercent;
        public Byte     _Reserved1;
        public Int32    _BatteryLifeTime;
        public Int32    _BatteryFullLifeTime;
    }

}
