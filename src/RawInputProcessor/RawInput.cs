using System;

namespace RawInputProcessor
{
    public abstract class RawInput : IDisposable
    {
        private readonly RawKeyboard _keyboardDriver;

        public event EventHandler<RawInputEventArgs> KeyPressed
        {
            add { KeyboardDriver.KeyPressed += value; }
            remove { KeyboardDriver.KeyPressed -= value; }
        }

        public int NumberOfKeyboards
        {
            get { return KeyboardDriver.NumberOfKeyboards; }
        }

        protected RawKeyboard KeyboardDriver
        {
            get { return _keyboardDriver; }
        }

        protected RawInput(IntPtr handle, RawInputCaptureMode captureMode)
        {
            _keyboardDriver = new RawKeyboard(handle, captureMode == RawInputCaptureMode.Foreground);
        }

        public abstract void AddMessageFilter();
        public abstract void RemoveMessageFilter();

        public void Dispose()
        {
            KeyboardDriver.Dispose();
        }
    }
}