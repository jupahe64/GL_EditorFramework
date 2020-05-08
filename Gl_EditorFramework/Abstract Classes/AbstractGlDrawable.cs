using GL_EditorFramework.GL_Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;

namespace GL_EditorFramework.Interfaces
{
    public enum Pass
    {
        OPAQUE,
        TRANSPARENT,
        PICKING
    }

    public class MarginScrollEventArgs : EventArgs
    {
        public Point Location { get; set; }

        public int AmountX { get; set; }
        public int AmountY { get; set; }

        public MarginScrollEventArgs(Point location, int amountX, int amountY)
        {
            Location = location;
            AmountX = amountX;
            AmountY = amountY;
        }
    }

    public abstract class AbstractGlDrawable : AbstractEventHandling3DObj
    {
        [Browsable(false)]
        public bool Visible { get; set; } = true;

        public const uint REDRAW =				0x80000000;
        public const uint REDRAW_PICKING =		0xC0000000;
        public const uint REPICK =				0x40000000;
        public const uint NO_CAMERA_ACTION = 0x20000000;
        public const uint FORCE_REENTER = 0x10000000;

        public abstract void Prepare(GL_ControlModern control);
        public abstract void Prepare(GL_ControlLegacy control);
        public abstract void Draw(GL_ControlModern control, Pass pass);
        public abstract void Draw(GL_ControlLegacy control, Pass pass);
        public virtual int GetPickableSpan() => 1;
        public virtual uint MouseEnter(int index, GL_ControlBase control) { return 0; }
        public virtual uint MouseLeave(int index, GL_ControlBase control) { return 0; }
        public virtual uint MouseLeaveEntirely(GL_ControlBase control) { return 0; }
        public override uint MouseMove(MouseEventArgs e, Point lastMousePos, GL_ControlBase control) { return REPICK; }
        public virtual void MarginScroll(MarginScrollEventArgs e, GL_ControlBase control) { }

        public virtual void Connect(GL_ControlBase control) { }
        public virtual void Disconnect(GL_ControlBase control) { }
    }
}
