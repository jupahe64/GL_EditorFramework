using GL_EditorFramework.GL_Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GL_EditorFramework.Interfaces
{
    public class AbstractEventHandling3DObj
    {
        public virtual uint MouseDown(MouseEventArgs e, GL_ControlBase control) { return 0; }
        public virtual uint MouseMove(MouseEventArgs e, Point lastMousePos, GL_ControlBase control) {return 0; }
        public virtual uint MouseUp(MouseEventArgs e, GL_ControlBase control) { return 0; }
        public virtual uint MouseWheel(MouseEventArgs e, GL_ControlBase control) { return 0; }
        public virtual uint MouseClick(MouseEventArgs e, GL_ControlBase control) { return 0; }
        
        public virtual uint KeyDown(KeyEventArgs e, GL_ControlBase control) { return 0; }
        public virtual uint KeyUp(KeyEventArgs e, GL_ControlBase control) { return 0; }
    }
}
