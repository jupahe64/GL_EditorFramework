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

        protected int xrayPickingIndex;

        public bool XRaySelection = false;

        protected bool drawSelection = false;
        protected bool drawOthers = false;

        public override void Draw(GL_ControlModern control, Pass pass)
        {
            if (XRaySelection)
            {
                #region xray picking
                if (pass == Pass.OPAQUE)
                {
                    //draw all GetObjects() except Selection
                    drawOthers = true;
                    drawSelection = false;

                    foreach (IEditableObject obj in GetObjects())
                        obj.Draw(control, Pass.OPAQUE, this);

                    foreach (AbstractGlDrawable obj in StaticObjects)
                        obj.Draw(control, Pass.OPAQUE);


                    //draw selection XRay
                    drawOthers = false;
                    drawSelection = true;

                    control.NextPickingColorHijack = SelectColorHijack;

                    GL.DepthMask(false);
                    GL.DepthFunc(DepthFunction.Greater);

                    foreach (IEditableObject obj in GetObjects())
                        obj.Draw(control, Pass.PICKING, this);


                    //draw visible selection
                    GL.DepthMask(true);
                    GL.DepthFunc(DepthFunction.Lequal);

                    control.NextPickingColorHijack = null;
                    control.RNG = new Random(0);

                    foreach (IEditableObject obj in GetObjects())
                        obj.Draw(control, Pass.OPAQUE, this);

                }
                else if (pass == Pass.PICKING)
                {
                    //draw all GetObjects() except Selection
                    drawOthers = true;
                    drawSelection = false;

                    control.NextPickingColorHijack = ExtraPickingHijack;
                    xrayPickingIndex = control.PickingIndexOffset;

                    foreach (IEditableObject obj in GetObjects())
                        obj.Draw(control, Pass.PICKING, this);

                    foreach (AbstractGlDrawable obj in StaticObjects)
                        obj.Draw(control, Pass.PICKING);


                    //clear depth where selected GetObjects() are
                    drawOthers = false;
                    drawSelection = true;

                    GL.ColorMask(false, false, false, false);
                    GL.DepthFunc(DepthFunction.Always);
                    GL.DepthRange(1, 1);

                    control.NextPickingColorHijack = SelectColorHijack;

                    foreach (IEditableObject obj in GetObjects())
                        obj.Draw(control, Pass.PICKING, this);


                    //draw "emulated x-ray"
                    control.NextPickingColorHijack = ExtraPickingHijack;
                    xrayPickingIndex = control.PickingIndexOffset;

                    GL.ColorMask(true, true, true, true);
                    GL.DepthFunc(DepthFunction.Lequal);
                    GL.DepthRange(0, 1);

                    foreach (IEditableObject obj in GetObjects())
                        obj.Draw(control, Pass.PICKING, this);



                    control.NextPickingColorHijack = null;
                }
                #endregion
                else
                {
                    drawOthers = true;
                    drawSelection = true;

                    foreach (IEditableObject obj in GetObjects())
                        obj.Draw(control, Pass.TRANSPARENT, this);

                    foreach (AbstractGlDrawable obj in StaticObjects)
                        obj.Draw(control, Pass.TRANSPARENT);
                }
            }
            else
            {
                drawOthers = true;
                drawSelection = true;

                foreach (IEditableObject obj in GetObjects())
                    obj.Draw(control, pass, this);

                foreach (AbstractGlDrawable obj in StaticObjects)
                    obj.Draw(control, pass);

            }


            if (pass == Pass.OPAQUE)
            {
                CurrentAction.Draw(control);
                ExclusiveAction.Draw(control);
            }
        }

        public override void Draw(GL_ControlLegacy control, Pass pass)
        {
            if (XRaySelection)
            {
                #region xray picking
                if (pass == Pass.OPAQUE)
                {
                    //draw all GetObjects() except Selection
                    drawOthers = true;
                    drawSelection = false;

                    foreach (IEditableObject obj in GetObjects())
                        obj.Draw(control, Pass.OPAQUE, this);

                    foreach (AbstractGlDrawable obj in StaticObjects)
                        obj.Draw(control, Pass.OPAQUE);


                    //draw selection XRay
                    drawOthers = false;
                    drawSelection = true;

                    control.NextPickingColorHijack = SelectColorHijack;

                    GL.DepthMask(false);
                    GL.DepthFunc(DepthFunction.Greater);

                    foreach (IEditableObject obj in GetObjects())
                        obj.Draw(control, Pass.PICKING, this);


                    //draw visible selection
                    GL.DepthMask(true);
                    GL.DepthFunc(DepthFunction.Lequal);

                    control.NextPickingColorHijack = null;
                    control.RNG = new Random(0);

                    foreach (IEditableObject obj in GetObjects())
                        obj.Draw(control, Pass.OPAQUE, this);

                }
                else if (pass == Pass.PICKING)
                {
                    //draw all GetObjects() except Selection
                    drawOthers = true;
                    drawSelection = false;

                    control.NextPickingColorHijack = ExtraPickingHijack;
                    xrayPickingIndex = control.PickingIndexOffset;

                    foreach (IEditableObject obj in GetObjects())
                        obj.Draw(control, Pass.PICKING, this);

                    foreach (AbstractGlDrawable obj in StaticObjects)
                        obj.Draw(control, Pass.PICKING);


                    //clear depth where selected GetObjects() are
                    drawOthers = false;
                    drawSelection = true;

                    GL.ColorMask(false, false, false, false);
                    GL.DepthFunc(DepthFunction.Always);
                    GL.DepthRange(1, 1);

                    control.NextPickingColorHijack = SelectColorHijack;

                    foreach (IEditableObject obj in GetObjects())
                        obj.Draw(control, Pass.PICKING, this);


                    //draw "emulated x-ray"
                    control.NextPickingColorHijack = ExtraPickingHijack;
                    xrayPickingIndex = control.PickingIndexOffset;

                    GL.ColorMask(true, true, true, true);
                    GL.DepthFunc(DepthFunction.Lequal);
                    GL.DepthRange(0, 1);

                    foreach (IEditableObject obj in GetObjects())
                        obj.Draw(control, Pass.PICKING, this);



                    control.NextPickingColorHijack = null;
                }
                #endregion
                else
                {
                    drawOthers = true;
                    drawSelection = true;

                    foreach (IEditableObject obj in GetObjects())
                        obj.Draw(control, Pass.TRANSPARENT, this);

                    foreach (AbstractGlDrawable obj in StaticObjects)
                        obj.Draw(control, Pass.TRANSPARENT);
                }
            }
            else
            {
                drawOthers = true;
                drawSelection = true;

                foreach (IEditableObject obj in GetObjects())
                    obj.Draw(control, pass, this);

                foreach (AbstractGlDrawable obj in StaticObjects)
                    obj.Draw(control, pass);

            }


            if (pass == Pass.OPAQUE)
            {
                CurrentAction.Draw(control);
                ExclusiveAction.Draw(control);
            }
        }

        public override void Prepare(GL_ControlModern control)
        {
            this.control = control;
            foreach (IEditableObject obj in GetObjects())
                obj.Prepare(control);
            foreach (AbstractGlDrawable obj in StaticObjects)
                obj.Prepare(control);
        }

        public override void Prepare(GL_ControlLegacy control)
        {
            this.control = control;
            foreach (IEditableObject obj in GetObjects())
                obj.Prepare(control);
            foreach (AbstractGlDrawable obj in StaticObjects)
                obj.Prepare(control);
        }

        public override int GetPickableSpan()
        {
            int var = 0;
            foreach (IEditableObject obj in GetObjects())
                if (obj.Visible && obj.IsInRange(renderDistance, renderDistanceSquared, control.CameraPosition))
                    var += obj.GetPickableSpan();
            foreach (AbstractGlDrawable obj in StaticObjects)
                var += obj.GetPickableSpan();
            return var;
        }

        public bool ShouldBeDrawn(IEditableObject obj)
        {
            if (!(obj.Visible && obj.IsInRange(renderDistance, renderDistanceSquared, control.CameraPosition)))
                return false;

            if ((drawSelection && SelectedObjects.Contains(obj)) || (drawOthers && !SelectedObjects.Contains(obj)))
                return true;

            else
            {
                xrayPickingIndex += obj.GetPickableSpan();
                for (int i = 0; i < obj.GetRandomNumberSpan(); i++)
                    control.RNG?.Next();

                return false;
            }
        }

        protected Vector4 SelectColorHijack() => new Vector4(1, 1f, 0.25f, 1);

        protected Vector4 ExtraPickingHijack()
        {
            return new Vector4(
                ((xrayPickingIndex >> 16) & 0xFF) / 255f,
                ((xrayPickingIndex >> 8) & 0xFF) / 255f,
                (xrayPickingIndex & 0xFF) / 255f,
                ((xrayPickingIndex++ >> 24) & 0xFF) / 255f
            );
        }
    }
}