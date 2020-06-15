using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;

namespace GL_EditorFramework.EditorDrawables
{
    public abstract partial class EditorSceneBase : AbstractGlDrawable
    {
        protected float renderDistanceSquared = 1000000;
        protected float renderDistance = 1000;

        public float RenderDistance
        {
            get => renderDistanceSquared;
            set
            {
                if (value < 1f)
                {
                    renderDistanceSquared = 1f;
                    renderDistance = 1f;
                }
                else
                {
                    renderDistanceSquared = value * value;
                    renderDistance = value;
                }
            }
        }

        public override void Draw(GL_ControlModern control, Pass pass)
        {
            ObjectRenderState.ShouldBeDrawn = ShouldBeDrawn;

            foreach (IEditableObject obj in GetObjects())
                obj.Draw(control, pass, this);

            foreach (AbstractGlDrawable obj in StaticObjects)
                obj.Draw(control, pass);


            ObjectRenderState.ShouldBeDrawn = ObjectRenderState.ShouldBeDrawn_Default;

            if (pass == Pass.OPAQUE)
            {
                SelectionTransformAction.Draw(control);
                CurrentAction?.Draw(control);
            }
        }

        public override void Draw(GL_ControlLegacy control, Pass pass)
        {
            ObjectRenderState.ShouldBeDrawn = ShouldBeDrawn;

            foreach (IEditableObject obj in GetObjects())
                obj.Draw(control, pass, this);

            foreach (AbstractGlDrawable obj in StaticObjects)
                obj.Draw(control, pass);


            ObjectRenderState.ShouldBeDrawn = ObjectRenderState.ShouldBeDrawn_Default;

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

        public bool ShouldBeDrawn(IEditableObject obj)
        {
            if (!(obj.Visible && obj.IsInRange(renderDistance, renderDistanceSquared, control.CameraPosition)))
                return false;

            return true;
        }
    }
}