using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;

namespace GL_EditorFramework.EditorDrawables
{
    public abstract partial class EditorSceneBase : AbstractGlDrawable
    {

        public override void Draw(GL_ControlModern control, Pass pass)
        {
            foreach (IEditableObject obj in GetObjects())
            {
                if (obj.Visible)
                {
                    control.LimitPickingColors(obj.GetPickableSpan());
                    obj.Draw(control, pass, this);
                }
            }

            control.UnlimitPickingColors();

            foreach (AbstractGlDrawable obj in StaticObjects)
            {
                if (obj.Visible)
                    obj.Draw(control, pass);
            }

            if (pass == Pass.OPAQUE)
            {
                SelectionTransformAction.Draw(control);
                CurrentAction?.Draw(control);
            }
        }

        public override void Draw(GL_ControlLegacy control, Pass pass)
        {
            foreach (IEditableObject obj in GetObjects())
            {
                if (obj.Visible)
                {
                    control.LimitPickingColors(obj.GetPickableSpan());
                    obj.Draw(control, pass, this);
                }
            }

            control.UnlimitPickingColors();

            foreach (AbstractGlDrawable obj in StaticObjects)
            {
                if (obj.Visible)
                    obj.Draw(control, pass);
            }

            if (pass == Pass.OPAQUE)
            {
                SelectionTransformAction.Draw(control);
                CurrentAction?.Draw(control);
            }
        }

        public override int GetPickableSpan()
        {
            int var = 0;
            foreach (IEditableObject obj in GetObjects())
                var += obj.GetPickableSpan();

            foreach (AbstractGlDrawable obj in StaticObjects)
                var += obj.GetPickableSpan();
            return var;
        }
    }
}