using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Interop;

namespace RawInputProcessor.Win32
{
    internal static class Win32Methods
    {
        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax,
            uint wRemoveMsg);

        [DllImport("user32", SetLastError = true)]
        public static extern int GetRawInputData(IntPtr hRawInput, DataCommand command, out InputData buffer,
            [In] [Out] ref int size, int cbSizeHeader);

        [DllImport("user32", SetLastError = true)]
        public static extern int GetRawInputData(IntPtr hRawInput, DataCommand command, [Out] IntPtr pData,
            [In] [Out] ref int size, int sizeHeader);

        [DllImport("user32", SetLastError = true)]
        public static extern uint GetRawInputDeviceInfo(IntPtr hDevice, RawInputDeviceInfo command, IntPtr pData,
            ref uint size);

        [DllImport("user32")]
        public static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint command, ref DeviceInfo data,
            ref uint dataSize);

        [DllImport("user32", SetLastError = true)]
        public static extern uint GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint numberDevices, uint size);

        [DllImport("user32", SetLastError = true)]
        public static extern bool RegisterRawInputDevices(RawInputDevice[] pRawInputDevice, uint numberDevices,
            uint size);

        [DllImport("user32", SetLastError = true)]
        public static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr notificationFilter,
            DeviceNotification flags);

        [DllImport("user32", SetLastError = true)]
        public static extern bool UnregisterDeviceNotification(IntPtr handle);

        public static int LoWord(int dwValue)
        {
            return (dwValue & 0xFFFF);
        }

        public static int HiWord(Int64 dwValue)
        {
            return (int)(dwValue >> 16) & ~Win32Consts.FAPPCOMMANDMASK;
        }

        public static ushort LowWord(uint val)
        {
            return (ushort)val;
        }

        public static ushort HighWord(uint val)
        {
            return (ushort)(val >> 16);
        }

        public static uint BuildWParam(ushort low, ushort high)
        {
            return ((uint)high << 16) | low;
        }

        public static string GetDeviceDiagnostics()
        {
            var stringBuilder = new StringBuilder();
            uint devices = 0u;
            int listSize = Marshal.SizeOf(typeof(RawInputDeviceList));
            if (GetRawInputDeviceList(IntPtr.Zero, ref devices, (uint)listSize) != 0u)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            var deviceListPtr = Marshal.AllocHGlobal((int)(listSize * devices));
            try
            {
                GetRawInputDeviceList(deviceListPtr, ref devices, (uint)listSize);
                int index = 0;
                while (index < devices)
                {
                    uint pcbSize = 0u;
                    var rawInputDeviceList = (RawInputDeviceList)Marshal.PtrToStructure(new IntPtr(deviceListPtr.ToInt64() + listSize * index), typeof(RawInputDeviceList));
                    GetRawInputDeviceInfo(rawInputDeviceList.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);
                    if (pcbSize <= 0u)
                    {
                        stringBuilder.AppendLine("pcbSize: " + pcbSize);
                        stringBuilder.AppendLine(Marshal.GetLastWin32Error().ToString());
                        string result = stringBuilder.ToString();
                        return result;
                    }
                    var deviceInfoSize = (uint)Marshal.SizeOf(typeof(DeviceInfo));
                    var deviceInfo = new DeviceInfo
                    {
                        Size = Marshal.SizeOf(typeof(DeviceInfo))
                    };
                    if (GetRawInputDeviceInfo(rawInputDeviceList.hDevice, 536870923u, ref deviceInfo, ref deviceInfoSize) <= 0u)
                    {
                        stringBuilder.AppendLine(Marshal.GetLastWin32Error().ToString());
                        string result = stringBuilder.ToString();
                        return result;
                    }
                    var deviceInfoPtr = Marshal.AllocHGlobal((int)pcbSize);
                    try
                    {
                        GetRawInputDeviceInfo(rawInputDeviceList.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, deviceInfoPtr,
                            ref pcbSize);
                        string device = Marshal.PtrToStringAnsi(deviceInfoPtr);
                        if (rawInputDeviceList.dwType == DeviceType.RimTypekeyboard ||
                            rawInputDeviceList.dwType == DeviceType.RimTypeHid)
                        {
                            string deviceDescription = GetDeviceDescription(device);
                            var rawKeyboardDevice = new RawKeyboardDevice(Marshal.PtrToStringAnsi(deviceInfoPtr),
                                (RawDeviceType)rawInputDeviceList.dwType, rawInputDeviceList.hDevice, deviceDescription);
                            stringBuilder.AppendLine(rawKeyboardDevice.ToString());
                            stringBuilder.AppendLine(deviceInfo.ToString());
                            stringBuilder.AppendLine(deviceInfo.KeyboardInfo.ToString());
                            stringBuilder.AppendLine(deviceInfo.HIDInfo.ToString());
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(deviceInfoPtr);
                    }
                    index++;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(deviceListPtr);
            }
            return stringBuilder.ToString();
        }

        public static string GetDeviceDescription(string device)
        {
            var deviceKey = RegistryAccess.GetDeviceKey(device);
            if (deviceKey == null) return string.Empty;

            string text = deviceKey.GetValue("DeviceDesc").ToString();
            return text.Substring(text.IndexOf(';') + 1);
        }

        public static bool InputInForeground(IntPtr wparam)
        {
            return wparam.ToInt32() == 0;
        }
    }
}