using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GL_EditorFramework.EditorDrawables
{
    public abstract class AbstractDrawableCollection : AbstractGlDrawable
    {
        protected abstract IEnumerable<AbstractGlDrawable> GetDrawables();

        public override uint MouseDown(MouseEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                var |= obj.MouseDown(e, control);
            }
            return var;
        }

        public override uint MouseMove(MouseEventArgs e, Point lastMousePos, GL_ControlBase control)
        {
            uint var = 0;
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                var |= obj.MouseMove(e, lastMousePos, control);
            }
            return var;
        }

        public override uint MouseWheel(MouseEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                var |= obj.MouseWheel(e, control);
            }

            return var;
        }

        public override uint MouseUp(MouseEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                var |= obj.MouseUp(e, control);
            }

            return var;
        }

        public override uint MouseClick(MouseEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                var |= obj.MouseClick(e, control);
            }

            return var;
        }

        public override uint MouseEnter(int inObjectIndex, GL_ControlBase control)
        {
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                int span = obj.GetPickableSpan();
                if (inObjectIndex >= 0 && inObjectIndex < span)
                {
                    return obj.MouseEnter(inObjectIndex, control);
                }
                inObjectIndex -= span;
            }
            return 0;
        }

        public override uint MouseLeave(int inObjectIndex, GL_ControlBase control)
        {
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                int span = obj.GetPickableSpan();
                if (inObjectIndex >= 0 && inObjectIndex < span)
                {
                    return obj.MouseLeave(inObjectIndex, control);
                }
                inObjectIndex -= span;
            }
            return 0;
        }

        public override uint MouseLeaveEntirely(GL_ControlBase control)
        {
            uint var = 0;
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                var |= obj.MouseLeaveEntirely(control);
            }
            return var;
        }

        public override uint KeyDown(KeyEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                var |= obj.KeyDown(e, control);
            }
            return var;
        }

        public override uint KeyUp(KeyEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                var |= obj.KeyUp(e, control);
            }
            return var;
        }

        public override void Prepare(GL_ControlModern control)
        {
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                obj.Prepare(control);
            }
        }

        public override void Prepare(GL_ControlLegacy control)
        {
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                obj.Prepare(control);
            }
        }

        public override void Draw(GL_ControlModern control, Pass pass)
        {
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                obj.Draw(control, pass);
            }
        }

        public override void Draw(GL_ControlLegacy control, Pass pass)
        {
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                obj.Draw(control, pass);
            }
        }

        public override int GetPickableSpan()
        {
            int span = 0;
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                span += obj.GetPickableSpan();
            }
            return span;
        }

        public override int GetRandomNumberSpan()
        {
            int span = 0;
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                span += obj.GetRandomNumberSpan();
            }
            return span;
        }
        
        public override void Connect(GL_ControlBase control)
        {
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                obj.Connect(control);
            }
        }

        public override void Disconnect(GL_ControlBase control)
        {
            foreach (AbstractGlDrawable obj in GetDrawables())
            {
                obj.Disconnect(control);
            }
        }
    }
}
