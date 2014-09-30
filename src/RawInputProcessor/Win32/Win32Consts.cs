namespace RawInputProcessor.Win32
{
    internal static class Win32Consts
    {
        // ReSharper disable InconsistentNaming
        internal const int KEYBOARD_OVERRUN_MAKE_CODE = 255;
        internal const int WM_APPCOMMAND = 793;
        internal const int FAPPCOMMANDMASK = 61440;
        internal const int FAPPCOMMANDMOUSE = 32768;
        internal const int FAPPCOMMANDOEM = 4096;
        internal const int WM_KEYDOWN = 256;
        internal const int WM_KEYUP = 257;
        internal const int WM_SYSKEYDOWN = 260;
        internal const int WM_INPUT = 255;
        internal const int WM_USB_DEVICECHANGE = 537;
        internal const int WM_INPUT_DEVICE_CHANGE = 254;
        internal const int PM_REMOVE = 1;
        internal const int VK_SHIFT = 16;
        internal const int RI_KEY_MAKE = 0;
        internal const int RI_KEY_BREAK = 1;
        internal const int RI_KEY_E0 = 2;
        internal const int RI_KEY_E1 = 4;
        internal const int VK_CONTROL = 17;
        internal const int VK_MENU = 18;
        internal const int VK_ZOOM = 251;
        internal const int VK_LSHIFT = 160;
        internal const int VK_RSHIFT = 161;
        internal const int VK_LCONTROL = 162;
        internal const int VK_RCONTROL = 163;
        internal const int VK_LMENU = 164;
        internal const int VK_RMENU = 165;
        internal const int SC_SHIFT_R = 54;
        internal const int SC_SHIFT_L = 42;
        internal const int RIM_INPUT = 0;
        // ReSharper restore InconsistentNaming
    }
}