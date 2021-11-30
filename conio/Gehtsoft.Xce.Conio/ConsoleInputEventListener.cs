namespace Gehtsoft.Xce.Conio
{
    public class ConsoleInputEventListener : IConsoleInputListener
    {
        public delegate void KeyDelegate(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt);
        public delegate void MouseDelegate(int row, int column, bool shift, bool ctrl, bool alt, bool lb, bool rb);
        public delegate void MouseButtonDelegate(int row, int column, bool shift, bool ctrl, bool alt);
        public delegate void GetFocusDelegate(bool shift, bool ctrl, bool alt);
        public delegate void ScreenBufferChangedDelegate(int rows, int columns);
        public delegate void IdleDelegate();

        public event KeyDelegate KeyPressed;
        public event KeyDelegate KeyReleased;
        public event MouseDelegate MouseMoved;
        public event MouseButtonDelegate MouseLButtonDown;
        public event MouseButtonDelegate MouseLButtonUp;
        public event MouseButtonDelegate MouseRButtonDown;
        public event MouseButtonDelegate MouseRButtonUp;
        public event MouseButtonDelegate MouseWheelDown;
        public event MouseButtonDelegate MouseWheelUp;
        public event GetFocusDelegate Focus;
        public event ScreenBufferChangedDelegate ScreenBufferChanged;
        public event IdleDelegate Idle;

        void IConsoleInputListener.OnKeyPressed(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            KeyPressed?.Invoke(scanCode, character, shift, ctrl, alt);
        }

        void IConsoleInputListener.OnKeyReleased(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            KeyReleased?.Invoke(scanCode, character, shift, ctrl, alt);
        }

        void IConsoleInputListener.OnMouseMove(int row, int column, bool shift, bool ctrl, bool alt, bool lb, bool rb)
        {
            MouseMoved?.Invoke(row, column, shift, ctrl, alt, lb, rb);
        }

        void IConsoleInputListener.OnMouseLButtonDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
            MouseLButtonDown?.Invoke(row, column, shift, ctrl, alt);
        }

        void IConsoleInputListener.OnMouseLButtonUp(int row, int column, bool shift, bool ctrl, bool alt)
        {
            MouseLButtonUp?.Invoke(row, column, shift, ctrl, alt);
        }

        void IConsoleInputListener.OnMouseRButtonDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
            MouseRButtonDown?.Invoke(row, column, shift, ctrl, alt);
        }

        void IConsoleInputListener.OnMouseRButtonUp(int row, int column, bool shift, bool ctrl, bool alt)
        {
            MouseRButtonUp?.Invoke(row, column, shift, ctrl, alt);
        }

        void IConsoleInputListener.OnMouseWheelUp(int row, int column, bool shift, bool ctrl, bool alt)
        {
            MouseWheelUp?.Invoke(row, column, shift, ctrl, alt);
        }

        void IConsoleInputListener.OnMouseWheelDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
            MouseWheelDown?.Invoke(row, column, shift, ctrl, alt);
        }

        void IConsoleInputListener.OnGetFocus(bool shift, bool ctrl, bool alt)
        {
            Focus?.Invoke(shift, ctrl, alt);
        }

        void IConsoleInputListener.OnScreenBufferChanged(int rows, int columns)
        {
            ScreenBufferChanged.Invoke(rows, columns);
        }

        void IConsoleInputListener.OnIdle()
        {
            Idle?.Invoke();
        }
    }
}
