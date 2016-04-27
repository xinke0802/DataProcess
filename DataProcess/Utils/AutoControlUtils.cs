using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;

namespace DataProcess.Utils
{
    #region Data structures
    public enum MouseEventFlag : uint
    {
        Move = 0x0001,
        LeftDown = 0x0002,
        LeftUp = 0x0004,
        RightDown = 0x0008,
        RightUp = 0x0010,
        MiddleDown = 0x0020,
        MiddleUp = 0x0040,
        XDown = 0x0080,
        XUp = 0x0100,
        Wheel = 0x0800,
        VirtualDesk = 0x4000,
        Absolute = 0x8000,
    }
    #endregion

    public class AutoControlUtils
    {
        [DllImport("user32.dll")]
        static extern void mouse_event(MouseEventFlag flags, int dx, int dy, uint data, UIntPtr ext);
        
        public static void MouseDragMove(int deltaX, int deltaY, bool isLeftKey = true)
        {
            int orgX, orgY;
            GetMousePosition(out orgX, out orgY);

            if (isLeftKey)
                mouse_event(MouseEventFlag.LeftDown, 0, 0, 0, UIntPtr.Zero);
            else
                mouse_event(MouseEventFlag.RightDown, 0, 0, 0, UIntPtr.Zero);

            MoveMousePosition(orgX + deltaX, orgY + deltaY);

            if (isLeftKey)
                mouse_event(MouseEventFlag.LeftUp, 0, 0, 0, UIntPtr.Zero);
            else
                mouse_event(MouseEventFlag.RightUp, 0, 0, 0, UIntPtr.Zero);

        }

        public static void MoveMousePosition(int x, int y)
        {
            Cursor.Position = new Point(x, y);
        }

        public static void GetMousePosition(out int x, out int y)
        {
            x = Cursor.Position.X;
            y = Cursor.Position.Y;
        }


    }
}
