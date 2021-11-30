using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Gehtsoft.Xce.Conio
{
    public interface IConsoleInputListener
    {
        void OnKeyPressed(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt);
        void OnKeyReleased(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt);
        void OnMouseMove(int row, int column, bool shift, bool ctrl, bool alt, bool lb, bool rb);
        void OnMouseLButtonDown(int row, int column, bool shift, bool ctrl, bool alt);
        void OnMouseLButtonUp(int row, int column, bool shift, bool ctrl, bool alt);
        void OnMouseRButtonDown(int row, int column, bool shift, bool ctrl, bool alt);
        void OnMouseRButtonUp(int row, int column, bool shift, bool ctrl, bool alt);
        void OnMouseWheelUp(int row, int column, bool shift, bool ctrl, bool alt);
        void OnMouseWheelDown(int row, int column, bool shift, bool ctrl, bool alt);
        void OnGetFocus(bool shift, bool ctrl, bool alt);
        void OnScreenBufferChanged(int rows, int columns);

        void OnIdle();
    }
}
