using System;
using System.Windows.Forms;

namespace RawInputProcessor
{
    public class RawFormsInput : RawInput
    {
        // ReSharper disable once NotAccessedField.Local
        private RawInputNativeWindow _window;
        private PreMessageFilter _filter;

        public override void AddMessageFilter()
        {
            if (_filter != null)
            {
                return;
            }
            _filter = new PreMessageFilter(this);
            Application.AddMessageFilter(_filter);
        }

        public override void RemoveMessageFilter()
        {
            if (_filter == null)
            {
                return;
            }
            Application.RemoveMessageFilter(_filter);
        }

        public RawFormsInput(IntPtr parentHandle, RawInputCaptureMode captureMode)
            : base(parentHandle, captureMode)
        {
            _window = new RawInputNativeWindow(this, parentHandle);
        }

        public RawFormsInput(IWin32Window window, RawInputCaptureMode captureMode)
            : this(window.Handle, captureMode)
        {
        }

        private class PreMessageFilter : IMessageFilter
        {
            private readonly RawFormsInput _rawFormsInput;

            public PreMessageFilter(RawFormsInput rawFormsInput)
            {
                _rawFormsInput = rawFormsInput;
            }

            public bool PreFilterMessage(ref Message m)
            {
                return _rawFormsInput.KeyboardDriver.HandleMessage(m.Msg, m.WParam, m.LParam);
            }
        }

        private class RawInputNativeWindow : NativeWindow
        {
            private readonly RawFormsInput _rawFormsInput;

            public RawInputNativeWindow(RawFormsInput rawFormsInput, IntPtr parentHandle)
            {
                _rawFormsInput = rawFormsInput;
                AssignHandle(parentHandle);
            }

            protected override void WndProc(ref Message message)
            {
                _rawFormsInput.KeyboardDriver.HandleMessage(message.Msg, message.WParam, message.LParam);
                base.WndProc(ref message);
            }
        }
    }
}