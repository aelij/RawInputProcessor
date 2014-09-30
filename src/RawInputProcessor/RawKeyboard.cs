using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using RawInputProcessor.Win32;

namespace RawInputProcessor
{
    public sealed class RawKeyboard : IDisposable
    {
        private static readonly Guid DeviceInterfaceHid = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");
        
        private readonly Dictionary<IntPtr, RawKeyboardDevice> _deviceList = new Dictionary<IntPtr, RawKeyboardDevice>();
        private readonly object _lock = new object();
        
        private IntPtr _devNotifyHandle;

        public int NumberOfKeyboards { get; private set; }
        
        public event EventHandler<RawInputEventArgs> KeyPressed;

        public RawKeyboard(IntPtr hwnd, bool captureOnlyInForeground)
        {
            RawInputDevice[] array =
            {
                new RawInputDevice
                {
                    UsagePage = HidUsagePage.GENERIC,
                    Usage = HidUsage.Keyboard,
                    Flags = (captureOnlyInForeground ? RawInputDeviceFlags.NONE : RawInputDeviceFlags.INPUTSINK) | RawInputDeviceFlags.DEVNOTIFY,
                    Target = hwnd
                }
            };
            if (!Win32Methods.RegisterRawInputDevices(array, (uint)array.Length, (uint)Marshal.SizeOf(array[0])))
            {
                throw new ApplicationException("Failed to register raw input device(s).", new Win32Exception());
            }
            EnumerateDevices();
            _devNotifyHandle = RegisterForDeviceNotifications(hwnd);
        }

        ~RawKeyboard()
        {
            Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (_devNotifyHandle != IntPtr.Zero)
            {
                Win32Methods.UnregisterDeviceNotification(_devNotifyHandle);
                _devNotifyHandle = IntPtr.Zero;
            }
        }

        private static IntPtr RegisterForDeviceNotifications(IntPtr parent)
        {
            IntPtr notifyHandle = IntPtr.Zero;
            BroadcastDeviceInterface broadcastDeviceInterface = default(BroadcastDeviceInterface);
            broadcastDeviceInterface.dbcc_size = Marshal.SizeOf(broadcastDeviceInterface);
            broadcastDeviceInterface.BroadcastDeviceType = BroadcastDeviceType.DBT_DEVTYP_DEVICEINTERFACE;
            broadcastDeviceInterface.dbcc_classguid = DeviceInterfaceHid;
            IntPtr interfacePtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(BroadcastDeviceInterface)));
            try
            {
                Marshal.StructureToPtr(broadcastDeviceInterface, interfacePtr, false);
                notifyHandle = Win32Methods.RegisterDeviceNotification(parent, interfacePtr,
                    DeviceNotification.DEVICE_NOTIFY_WINDOW_HANDLE);
            }
            catch (Exception ex)
            {
                Debug.Print("Registration for device notifications Failed. Error: {0}", Marshal.GetLastWin32Error());
                Debug.Print(ex.StackTrace);
            }
            finally
            {
                Marshal.FreeHGlobal(interfacePtr);
            }

            if (notifyHandle == IntPtr.Zero)
            {
                Debug.Print("Registration for device notifications Failed. Error: {0}", Marshal.GetLastWin32Error());
            }
            return notifyHandle;
        }

        public void EnumerateDevices()
        {
            lock (_lock)
            {
                _deviceList.Clear();
                var rawKeyboardDevice = new RawKeyboardDevice("Global Keyboard", RawDeviceType.Keyboard, IntPtr.Zero,
                    "Fake Keyboard. Some keys (ZOOM, MUTE, VOLUMEUP, VOLUMEDOWN) are sent to rawinput with a handle of zero.");
                _deviceList.Add(rawKeyboardDevice.Handle, rawKeyboardDevice);
                uint devices = 0u;
                int size = Marshal.SizeOf(typeof(RawInputDeviceList));
                if (Win32Methods.GetRawInputDeviceList(IntPtr.Zero, ref devices, (uint)size) != 0u)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(size * devices));
                try
                {
                    Win32Methods.GetRawInputDeviceList(pRawInputDeviceList, ref devices, (uint)size);
                    int index = 0;
                    while (index < devices)
                    {
                        RawKeyboardDevice device = GetDevice(pRawInputDeviceList, size, index);
                        if (device != null && !_deviceList.ContainsKey(device.Handle))
                        {
                            _deviceList.Add(device.Handle, device);
                        }
                        index++;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pRawInputDeviceList);
                }
                NumberOfKeyboards = _deviceList.Count;
            }
        }

        private static RawKeyboardDevice GetDevice(IntPtr pRawInputDeviceList, int dwSize, int index)
        {
            uint size = 0u;
            // On Window 8 64bit when compiling against .Net > 3.5 using .ToInt32 you will generate an arithmetic overflow. Leave as it is for 32bit/64bit applications
            var rawInputDeviceList = (RawInputDeviceList)Marshal.PtrToStructure(new IntPtr(pRawInputDeviceList.ToInt64() + dwSize * index), typeof(RawInputDeviceList));
            Win32Methods.GetRawInputDeviceInfo(rawInputDeviceList.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, IntPtr.Zero, ref size);
            if (size <= 0u)
            {
                return null;
            }
            IntPtr intPtr = Marshal.AllocHGlobal((int)size);
            try
            {
                Win32Methods.GetRawInputDeviceInfo(rawInputDeviceList.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, intPtr, ref size);
                string device = Marshal.PtrToStringAnsi(intPtr);
                if (rawInputDeviceList.dwType == DeviceType.RimTypekeyboard ||
                    rawInputDeviceList.dwType == DeviceType.RimTypeHid)
                {
                    string deviceDescription = Win32Methods.GetDeviceDescription(device);
                    return new RawKeyboardDevice(Marshal.PtrToStringAnsi(intPtr),
                        (RawDeviceType)rawInputDeviceList.dwType, rawInputDeviceList.hDevice, deviceDescription);
                }
            }
            finally
            {
                if (intPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(intPtr);
                }
            }
            return null;
        }

        private bool ProcessRawInput(IntPtr hdevice)
        {
            if (_deviceList.Count == 0)
            {
                return false;
            }
            int size = 0;
            Win32Methods.GetRawInputData(hdevice, DataCommand.RID_INPUT, IntPtr.Zero, ref size, Marshal.SizeOf(typeof(RawInputHeader)));
            InputData rawBuffer;
            if (Win32Methods.GetRawInputData(hdevice, DataCommand.RID_INPUT, out rawBuffer, ref size, Marshal.SizeOf(typeof(RawInputHeader))) != size)
            {
                Debug.WriteLine("Error getting the rawinput buffer");
                return false;
            }
            int vKey = rawBuffer.data.keyboard.VKey;
            int makecode = rawBuffer.data.keyboard.Makecode;
            int flags = rawBuffer.data.keyboard.Flags;
            if (vKey == Win32Consts.KEYBOARD_OVERRUN_MAKE_CODE)
            {
                return false;
            }

            RawKeyboardDevice device;
            if (!_deviceList.TryGetValue(rawBuffer.header.hDevice, out device))
            {
                Debug.WriteLine("Handle: {0} was not in the device list.", rawBuffer.header.hDevice);
                return false;
            }

            var isE0BitSet = ((flags & Win32Consts.RI_KEY_E0) != 0);
            bool isBreakBitSet = (flags & Win32Consts.RI_KEY_BREAK) != 0;

            uint message = rawBuffer.data.keyboard.Message;
            Key key = KeyInterop.KeyFromVirtualKey(AdjustVirtualKey(rawBuffer, vKey, isE0BitSet, makecode));
            EventHandler<RawInputEventArgs> keyPressed = KeyPressed;
            if (keyPressed != null)
            {
                var rawInputEventArgs = new RawInputEventArgs(device, isBreakBitSet ? KeyPressState.Up : KeyPressState.Down,
                    message, key, vKey);
                keyPressed(this, rawInputEventArgs);
                if (rawInputEventArgs.Handled)
                {
                    MSG msg;
                    Win32Methods.PeekMessage(out msg, IntPtr.Zero, Win32Consts.WM_KEYDOWN, Win32Consts.WM_KEYUP, Win32Consts.PM_REMOVE);
                }
                return rawInputEventArgs.Handled;
            }
            return false;
        }

        private static int AdjustVirtualKey(InputData rawBuffer, int virtualKey, bool isE0BitSet, int makeCode)
        {
            var adjustedKey = virtualKey;

            if (rawBuffer.header.hDevice == IntPtr.Zero)
            {
                // When hDevice is 0 and the vkey is VK_CONTROL indicates the ZOOM key
                if (rawBuffer.data.keyboard.VKey == Win32Consts.VK_CONTROL)
                {
                    adjustedKey = Win32Consts.VK_ZOOM;
                }
            }
            else
            {
                switch (virtualKey)
                {
                    // Right-hand CTRL and ALT have their e0 bit set 
                    case Win32Consts.VK_CONTROL:
                        adjustedKey = isE0BitSet ? Win32Consts.VK_RCONTROL : Win32Consts.VK_LCONTROL;
                        break;
                    case Win32Consts.VK_MENU:
                        adjustedKey = isE0BitSet ? Win32Consts.VK_RMENU : Win32Consts.VK_LMENU;
                        break;
                    case Win32Consts.VK_SHIFT:
                        adjustedKey = makeCode == Win32Consts.SC_SHIFT_R ? Win32Consts.VK_RSHIFT : Win32Consts.VK_LSHIFT;
                        break;
                    default:
                        adjustedKey = virtualKey;
                        break;
                }
            }

            return adjustedKey;
        }

        public bool HandleMessage(int msg, IntPtr wparam, IntPtr lparam)
        {
            switch (msg)
            {
                case Win32Consts.WM_INPUT_DEVICE_CHANGE:
                    EnumerateDevices();
                    break;
                case Win32Consts.WM_INPUT:
                    return ProcessRawInput(lparam);
            }
            return false;
        }

        public static string GetDeviceDianostics()
        {
            return Win32Methods.GetDeviceDiagnostics();
        }
    }
}