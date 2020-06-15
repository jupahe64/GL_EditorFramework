using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinInput = System.Windows.Input;

namespace GL_EditorFramework.EditorDrawables
{
    public static class TransformRendering
    {
        public static Vector4 colorX = new Vector4(1, 0, 0, 1);
        public static Vector4 colorY = new Vector4(0, 0.5f, 1, 1);
        public static Vector4 colorZ = new Vector4(0, 1, 0, 1);
    }

    public abstract class AbstractTransformAction
    {
        public virtual Vector3 NewPos(Vector3 pos) => pos;

        public Vector3 NewPos(Vector3 pos, out bool posHasChanged)
        {
            Vector3 newPos = NewPos(pos);
            if (newPos == pos)
                posHasChanged = false;
            else
                posHasChanged = true;
            return newPos;
        }

        public virtual Vector3 NewIndividualPos(Vector3 pos) => pos;

        public Vector3 NewIndividualPos(Vector3 pos, out bool posHasChanged)
        {
            Vector3 newPos = NewIndividualPos(pos);
            if (newPos == pos)
                posHasChanged = false;
            else
                posHasChanged = true;
            return newPos;
        }

        public virtual Matrix3 NewRot(Matrix3 rot) => rot;

        public Vector3 NewRot(Vector3 rot, out bool rotHasChanged)
        {
            Matrix3 rotMtx = Framework.Mat3FromEulerAnglesDeg(rot);

            Matrix3 newRot = NewRot(rotMtx);
            if (newRot == rotMtx)
            {
                rotHasChanged = false;
                return rot;
            }
            else
            {
                rotHasChanged = true;

                return newRot.ExtractDegreeEulerAngles() + new Vector3(
                    (float)Math.Round(rot.X / 360f) * 360,
                    (float)Math.Round(rot.Y / 360f) * 360,
                    (float)Math.Round(rot.Z / 360f) * 360
                    );
            }
        }

        public Matrix3 NewRot(Matrix3 rot, out bool rotHasChanged)
        {
            Matrix3 newRot = NewRot(rot);
            if (newRot == rot)
                rotHasChanged = false;
            else
                rotHasChanged = true;
            return newRot;
        }

        public virtual Vector3 NewScale(Vector3 scale, Matrix3 rotation) => scale;

        public Vector3 NewScale(Vector3 scale, Matrix3 rotation, out bool scaleHasChanged)
        {
            Vector3 newScale = NewScale(scale, rotation);
            if (newScale == scale)
                scaleHasChanged = false;
            else
                scaleHasChanged = true;
            return newScale;
        }

        protected GL_ControlBase control;

        public virtual void UpdateMousePos(Point mousePos) { }

        public virtual void ApplyScrolling(Point mousePos, float deltaScroll) { }

        public virtual void ApplyMarginScrolling(Point mousePos, float amountX, float amountY) { }

        public virtual void KeyDown(KeyEventArgs e)
        {

        }

        public virtual bool IsApplyOnRelease() => true;

        public virtual void Draw(GL_ControlModern controlModern)
        {

        }

        public virtual void Draw(GL_ControlLegacy controlLegacy)
        {

        }
    }

    public abstract class TransformingAction<T> : AbstractAction where T : AbstractTransformAction
    {
        protected T transformAction;

        protected abstract void Update();

        public override void UpdateMousePos(Point mousePos)
        {
            transformAction.UpdateMousePos(mousePos);
            Update();
        }

        public override void ApplyScrolling(Point mousePos, float deltaScroll)
        {
            transformAction.ApplyScrolling(mousePos, deltaScroll);
            Update();
        }

        public override void ApplyMarginScrolling(Point mousePos, float amountX, float amountY)
        {
            transformAction.ApplyMarginScrolling(mousePos, amountX, amountY);
            Update();
        }

        public override void KeyDown(KeyEventArgs e)
        {
            transformAction.KeyDown(e);
            Update();
        }

        public override void Draw(GL_ControlLegacy control)
        {
            transformAction.Draw(control);
        }

        public override void Draw(GL_ControlModern control)
        {
            transformAction.Draw(control);
        }
    }

    public class TranslateAction : AbstractTransformAction
    {
        float scrolling = 0;
        readonly float draggingDepth;
        Vector3 planeOrigin;
        Vector3 origin;
        bool absoluteSnapping;

        bool allowScrolling = true;

        enum AxisRestriction
        {
            NONE = 0b000,
            X    = 0b011,
            Y    = 0b101,
            Z    = 0b110,
            YZ   = 0b100,
            XZ   = 0b010,
            XY   = 0b001
        }

        AxisRestriction axisRestriction = AxisRestriction.NONE;

        Vector3 translation = new Vector3();

        public TranslateAction(GL_ControlBase control, Point mousePos, Vector3 center, float draggingDepth)
        {
            this.control = control;
            this.draggingDepth = draggingDepth;
            planeOrigin = control.CoordFor(mousePos.X, mousePos.Y, draggingDepth);
            origin = center;
        }

        public override void UpdateMousePos(Point mousePos)
        {
            Vector3 vec = control.InvertedRotationMatrix.Row2;

            Vector3 UnrestrictedTranslation() =>
                planeOrigin - control.ScreenCoordPlaneIntersection(mousePos, vec, -control.CameraPosition + (draggingDepth - scrolling) * vec);

            switch (axisRestriction)
            {
                case AxisRestriction.NONE:

                    translation = UnrestrictedTranslation();

                    break;

                case AxisRestriction.X:

                    if (Math.Round(vec.X, 7) == 1 || Math.Round(vec.X, 7) == -1)
                    {
                        vec = Vector3.UnitX * (float)Math.Round(vec.X, 7);

                        translation = UnrestrictedTranslation();
                        translation *= Vector3.UnitX;
                        goto Snapping;
                    }
                    else
                        vec.X = 0;

                    translation = planeOrigin - control.ScreenCoordPlaneIntersection(mousePos, vec, planeOrigin);
                    translation *= Vector3.UnitX;
                    break;

                case AxisRestriction.Y:

                    if (Math.Round(vec.Y, 7) == 1 || Math.Round(vec.Y, 7) == -1)
                    {
                        vec = Vector3.UnitY * (float)Math.Round(vec.Y, 7);

                        translation = UnrestrictedTranslation();
                        translation *= Vector3.UnitY;
                        goto Snapping;
                    }
                    else
                        vec.Y = 0;

                    translation = planeOrigin - control.ScreenCoordPlaneIntersection(mousePos, vec, planeOrigin);
                    translation *= Vector3.UnitY;
                    break;

                case AxisRestriction.Z:

                    if (Math.Round(vec.Z, 7) == 1 || Math.Round(vec.Z, 7) == -1)
                    {
                        vec = Vector3.UnitZ * (float)Math.Round(vec.Z, 7);

                        translation = UnrestrictedTranslation();
                        translation *= Vector3.UnitZ;
                        goto Snapping;
                    }
                    else
                        vec.Z = 0;

                    translation = planeOrigin - control.ScreenCoordPlaneIntersection(mousePos, vec, planeOrigin);
                    translation *= Vector3.UnitZ;
                    break;

                case AxisRestriction.YZ:

                    if (Math.Round(vec.X, 7) == 0)
                    {
                        translation = UnrestrictedTranslation();
                        translation.X = 0;
                    }
                    else
                    {
                        translation = planeOrigin - control.ScreenCoordPlaneIntersection(mousePos, Vector3.UnitX, planeOrigin);
                    }
                    break;

                case AxisRestriction.XZ:

                    if (Math.Round(vec.Y, 7) == 0)
                    {
                        translation = UnrestrictedTranslation();
                        translation.Y = 0;
                    }
                    else
                    {
                        translation = planeOrigin - control.ScreenCoordPlaneIntersection(mousePos, Vector3.UnitY, planeOrigin);
                    }
                    break;

                case AxisRestriction.XY:

                    if (Math.Round(vec.Z, 7) == 0)
                    {
                        translation = UnrestrictedTranslation();
                        translation.Z = 0;
                    }
                    else
                    {
                        translation = planeOrigin - control.ScreenCoordPlaneIntersection(mousePos, Vector3.UnitZ, planeOrigin);
                    }
                    break;
            }

        Snapping:
            if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftCtrl))
                return;

            if (absoluteSnapping)
                vec = origin + translation;
            else
                vec = translation;

            if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
            {
                vec.X = (float)Math.Round(vec.X * 2) * 0.5f;
                vec.Y = (float)Math.Round(vec.Y * 2) * 0.5f;
                vec.Z = (float)Math.Round(vec.Z * 2) * 0.5f;
            }
            if (absoluteSnapping)
                translation = vec - origin;
            else
                translation = vec;
        }

        public override void ApplyScrolling(Point mousePos, float deltaScroll)
        {
            if (!allowScrolling)
                return;

            deltaScroll *= Math.Min(0.01f, (draggingDepth - scrolling) / 500f);
            Console.WriteLine((draggingDepth - scrolling) / 500f);
            scrolling -= deltaScroll;

            UpdateMousePos(mousePos);
        }

        public override void ApplyMarginScrolling(Point mousePos, float amountX, float amountY)
        {
            var targetTrans = new Vector3(amountX * control.FactorX * draggingDepth * 0.25f, -amountY * control.FactorY * draggingDepth * 0.25f, 0) * control.InvertedRotationMatrix;

            if (((int)axisRestriction & 0b100) > 0)
                targetTrans.X = 0;

            if (((int)axisRestriction & 0b010) > 0)
                targetTrans.Y = 0;

            if (((int)axisRestriction & 0b001) > 0)
                targetTrans.Z = 0;

            control.CameraTarget += targetTrans;

            UpdateMousePos(mousePos);
        }

        public override void KeyDown(KeyEventArgs e)
        {
            if (e.Shift && e.KeyCode == Keys.S)
                absoluteSnapping = !absoluteSnapping;

            AxisRestriction old = axisRestriction;
            switch (e.KeyCode)
            {
                case Keys.X:
                    axisRestriction = e.Shift ? AxisRestriction.YZ : AxisRestriction.X;
                    break;
                case Keys.Y:
                    axisRestriction = e.Shift ? AxisRestriction.XZ : AxisRestriction.Y;
                    break;
                case Keys.Z:
                    axisRestriction = e.Shift ? AxisRestriction.XY : AxisRestriction.Z;
                    break;
                default:
                    return;
            }

            if (axisRestriction == old)
                axisRestriction = AxisRestriction.NONE;

            Vector3 dir = control.InvertedRotationMatrix.Row2;

            //determine whether scrolling should be allowed based on the current axisRestriction and camera orientation
            if (axisRestriction == AxisRestriction.NONE ||
                (axisRestriction == AxisRestriction.X    &&   (Math.Round(dir.X, 7) == 1 || Math.Round(dir.X, 7) == -1)) || //camera facing left/right
                (axisRestriction == AxisRestriction.Y    &&   (Math.Round(dir.Y, 7) == 1 || Math.Round(dir.Y, 7) == -1)) || //camera facing up/down
                (axisRestriction == AxisRestriction.Z    &&   (Math.Round(dir.Z, 7) == 1 || Math.Round(dir.Z, 7) == -1)) || //camera facing forward/backward
                (axisRestriction == AxisRestriction.YZ   &&   Math.Round(dir.X, 7) == 0) || //camera facing up/down/forward/backward
                (axisRestriction == AxisRestriction.XZ   &&   Math.Round(dir.Y, 7) == 0) || //camera facing left/right/forward/backward
                (axisRestriction == AxisRestriction.YZ   &&   Math.Round(dir.Z, 7) == 0)    //camera facing left/right/up/down
                )
            {
                allowScrolling = true;
            }
            else
            {
                allowScrolling = false;
                scrolling = 0;
            }
        }

        public override Vector3 NewPos(Vector3 pos)
        {
            return pos + translation;
        }

        public override void Draw(GL_ControlModern controlModern)
        {
            if (axisRestriction == AxisRestriction.NONE)
                return;

            controlModern.CurrentShader = Framework.SolidColorShaderProgram;

            control.ResetModelMatrix();

            GL.LineWidth(1.0f);

            if (axisRestriction == AxisRestriction.X || axisRestriction == AxisRestriction.XY || axisRestriction == AxisRestriction.XZ)
            {
                Framework.SolidColorShaderProgram.SetVector4("color", TransformRendering.colorX);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(-planeOrigin + Vector3.UnitX * control.ZFar);
                GL.Vertex3(-planeOrigin - Vector3.UnitX * control.ZFar);
                GL.End();
            }

            if (axisRestriction == AxisRestriction.Y || axisRestriction == AxisRestriction.XY || axisRestriction == AxisRestriction.YZ)
            {
                Framework.SolidColorShaderProgram.SetVector4("color", TransformRendering.colorY);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(-planeOrigin + Vector3.UnitY * control.ZFar);
                GL.Vertex3(-planeOrigin - Vector3.UnitY * control.ZFar);
                GL.End();
            }

            if (axisRestriction == AxisRestriction.Z || axisRestriction == AxisRestriction.XZ || axisRestriction == AxisRestriction.YZ)
            {
                Framework.SolidColorShaderProgram.SetVector4("color", TransformRendering.colorZ);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(-planeOrigin + Vector3.UnitZ * control.ZFar);
                GL.Vertex3(-planeOrigin - Vector3.UnitZ * control.ZFar);
                GL.End();
            }
        }

        public override void Draw(GL_ControlLegacy controlLegacy)
        {
            if (axisRestriction == AxisRestriction.NONE)
                return;

            control.ResetModelMatrix();

            GL.LineWidth(1.0f);

            if (axisRestriction == AxisRestriction.X || axisRestriction == AxisRestriction.XY || axisRestriction == AxisRestriction.XZ)
            {
                GL.Color4(TransformRendering.colorX);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(-planeOrigin + Vector3.UnitX * control.ZFar);
                GL.Vertex3(-planeOrigin - Vector3.UnitX * control.ZFar);
                GL.End();
            }

            if (axisRestriction == AxisRestriction.Y || axisRestriction == AxisRestriction.XY || axisRestriction == AxisRestriction.YZ)
            {
                GL.Color4(TransformRendering.colorY);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(-planeOrigin + Vector3.UnitY * control.ZFar);
                GL.Vertex3(-planeOrigin - Vector3.UnitY * control.ZFar);
                GL.End();
            }

            if (axisRestriction == AxisRestriction.Z || axisRestriction == AxisRestriction.XZ || axisRestriction == AxisRestriction.YZ)
            {
                GL.Color4(TransformRendering.colorZ);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(-planeOrigin + Vector3.UnitZ * control.ZFar);
                GL.Vertex3(-planeOrigin - Vector3.UnitZ * control.ZFar);
                GL.End();
            }
        }
    }

    public class RotateAction : AbstractTransformAction
    {
        Point startMousePos;

        Vector3 center;

        Vector3 planeOrigin;

        Matrix3 deltaRotation = Matrix3.Identity;

        public override Matrix3 NewRot(Matrix3 rot) => rot * deltaRotation;

        public override Vector3 NewIndividualPos(Vector3 pos) => Vector3.Transform(pos, deltaRotation);

        static readonly double eighthPI = Math.PI / 8d;

        bool showRotationInicator = true;

        Vector3 lastIntersection = new Vector3();

        enum AxisRestriction
        {
            NONE,
            X,
            Y,
            Z
        }

        AxisRestriction axisRestriction = AxisRestriction.NONE;

        public RotateAction(GL_ControlBase control, Point mousePos, Vector3 center, float draggingDepth)
        {
            this.control = control;
            startMousePos = mousePos;
            this.center = center;
            planeOrigin = control.CoordFor(mousePos.X, mousePos.Y, draggingDepth);
        }

        public override void UpdateMousePos(Point mousePos)
        {
            Vector3 vec = new Vector3();

            double angle = 0;

            Point centerPoint = control.ScreenCoordFor(center);

            vec = control.InvertedRotationMatrix.Row2;

            switch (axisRestriction)
            {
                case AxisRestriction.NONE:
                    angle = (Math.Atan2(mousePos.X - centerPoint.X, mousePos.Y - centerPoint.Y) -
                             Math.Atan2(startMousePos.X - centerPoint.X, startMousePos.Y - centerPoint.Y));
                    showRotationInicator = false;
                    break;

                case AxisRestriction.X:
                    if (Math.Round(vec.X) == 0)
                    {
                        angle = (Math.Sin(control.CamRotX) * (startMousePos.X - mousePos.X) +
                                 Math.Cos(control.CamRotX) * (startMousePos.Y - mousePos.Y)) * -0.015625;

                        showRotationInicator = false;
                    }
                    else
                    {
                        lastIntersection = -control.ScreenCoordPlaneIntersection(mousePos, Vector3.UnitX, planeOrigin);
                        Vector3 vec2 = -control.ScreenCoordPlaneIntersection(startMousePos, Vector3.UnitX, planeOrigin);

                        angle = (Math.Atan2(lastIntersection.Z - center.Z, lastIntersection.Y - center.Y) -
                                 Math.Atan2(vec2.Z - center.Z, vec2.Y - center.Y));

                        showRotationInicator = true;
                    }
                    vec = Vector3.UnitX;

                    break;

                case AxisRestriction.Y:
                    if (Math.Round(vec.Y) == 0)
                    {
                        angle = (startMousePos.X - mousePos.X) * -0.015625;

                        if (control.RotXIsReversed)
                            angle *= -1;

                        showRotationInicator = false;
                    }
                    else
                    {
                        lastIntersection = -control.ScreenCoordPlaneIntersection(mousePos, Vector3.UnitY, planeOrigin);
                        Vector3 vec2 = -control.ScreenCoordPlaneIntersection(startMousePos, Vector3.UnitY, planeOrigin);

                        angle = (Math.Atan2(lastIntersection.X - center.X, lastIntersection.Z - center.Z) -
                                 Math.Atan2(vec2.X - center.X, vec2.Z - center.Z));

                        showRotationInicator = true;
                    }
                    vec = Vector3.UnitY;
                    break;

                case AxisRestriction.Z:
                    if (Math.Round(vec.Z) == 0)
                    {
                        angle = (Math.Cos(control.CamRotX) * (startMousePos.X - mousePos.X) +
                                 Math.Sin(control.CamRotX) * (startMousePos.Y - mousePos.Y)) * -0.015625;

                        showRotationInicator = false;
                    }
                    else
                    {
                        lastIntersection = -control.ScreenCoordPlaneIntersection(mousePos, Vector3.UnitZ, planeOrigin);
                        Vector3 vec2 = -control.ScreenCoordPlaneIntersection(startMousePos, Vector3.UnitZ, planeOrigin);

                        angle = (Math.Atan2(lastIntersection.Y - center.Y, lastIntersection.X - center.X) -
                                 Math.Atan2(vec2.Y - center.Y, vec2.X - center.X));

                        showRotationInicator = true;
                    }
                    vec = Vector3.UnitZ;
                    break;
            }

            if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift) && !WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftCtrl))
                angle = Math.Round(angle / eighthPI) * eighthPI;

            deltaRotation = Matrix3.CreateFromAxisAngle(vec, (float)angle);

        }

        public override void KeyDown(KeyEventArgs e)
        {
            AxisRestriction old = axisRestriction;
            switch (e.KeyCode)
            {
                case Keys.X:
                    axisRestriction = AxisRestriction.X;
                    break;
                case Keys.Y:
                    axisRestriction = AxisRestriction.Y;
                    break;
                case Keys.Z:
                    axisRestriction = AxisRestriction.Z;
                    break;
                default:
                    return;
            }

            if (axisRestriction == old)
                axisRestriction = AxisRestriction.NONE;
        }

        public override Vector3 NewPos(Vector3 pos)
        {
            return Vector3.Transform(pos - center, deltaRotation) + center;
        }

        public override void Draw(GL_ControlModern controlModern)
        {
            if (axisRestriction == AxisRestriction.NONE)
                return;

            controlModern.CurrentShader = Framework.SolidColorShaderProgram;

            control.ResetModelMatrix();

            GL.LineWidth(1.0f);
            Framework.SolidColorShaderProgram.SetVector4("color", new Vector4(1, 1, 1, 1));

            Vector3 vec = center;
            if (axisRestriction == AxisRestriction.X)
                vec.X = -planeOrigin.X;
            else if (axisRestriction == AxisRestriction.Y)
                vec.Y = -planeOrigin.Y;
            else
                vec.Z = -planeOrigin.Z;

            if (showRotationInicator)
            {
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(vec);
                GL.Vertex3(lastIntersection);
                GL.End();
            }

            if (axisRestriction == AxisRestriction.X)
            {
                Framework.SolidColorShaderProgram.SetVector4("color", TransformRendering.colorX);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(vec + Vector3.UnitX * control.ZFar);
                GL.Vertex3(vec - Vector3.UnitX * control.ZFar);
            }
            else if (axisRestriction == AxisRestriction.Y)
            {
                Framework.SolidColorShaderProgram.SetVector4("color", TransformRendering.colorY);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(vec + Vector3.UnitY * control.ZFar);
                GL.Vertex3(vec - Vector3.UnitY * control.ZFar);
            }
            else if (axisRestriction == AxisRestriction.Z)
            {
                Framework.SolidColorShaderProgram.SetVector4("color", TransformRendering.colorZ);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(vec + Vector3.UnitZ * control.ZFar);
                GL.Vertex3(vec - Vector3.UnitZ * control.ZFar);
            }
            GL.End();
        }

        public override void Draw(GL_ControlLegacy controlLegacy)
        {
            if (axisRestriction == AxisRestriction.NONE)
                return;

            control.ResetModelMatrix();

            GL.LineWidth(1.0f);
            GL.Color4(Color.White);

            Vector3 vec = center;
            if (axisRestriction == AxisRestriction.X)
                vec.X = -planeOrigin.X;
            else if (axisRestriction == AxisRestriction.Y)
                vec.Y = -planeOrigin.Y;
            else
                vec.Z = -planeOrigin.Z;

            if (showRotationInicator)
            {
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(vec);
                GL.Vertex3(lastIntersection);
                GL.End();
            }

            if (axisRestriction == AxisRestriction.X)
            {
                GL.Color4(TransformRendering.colorX);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(vec + Vector3.UnitX * control.ZFar);
                GL.Vertex3(vec - Vector3.UnitX * control.ZFar);
            }
            else if (axisRestriction == AxisRestriction.Y)
            {
                GL.Color4(TransformRendering.colorY);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(vec + Vector3.UnitY * control.ZFar);
                GL.Vertex3(vec - Vector3.UnitY * control.ZFar);
            }
            else if (axisRestriction == AxisRestriction.Z)
            {
                GL.Color4(TransformRendering.colorZ);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(vec + Vector3.UnitZ * control.ZFar);
                GL.Vertex3(vec - Vector3.UnitZ * control.ZFar);
            }
            GL.End();
        }
    }

    public class ScaleAction : AbstractTransformAction
    {
        Point startMousePos;

        Vector3 center;

        public override Vector3 NewScale(Vector3 _scale, Matrix3 rotation)
        {
            return new Vector3(
                _scale.X * new Vector3(rotation.Row0 * scale).Length,
                _scale.Y * new Vector3(rotation.Row1 * scale).Length,
                _scale.Z * new Vector3(rotation.Row2 * scale).Length
                );
        }

        public override Vector3 NewIndividualPos(Vector3 pos) => pos * scale;

        Vector3 scale = Vector3.One;

        enum AxisRestriction
        {
            NONE,
            X,
            Y,
            Z,
            YZ,
            XZ,
            XY
        }

        AxisRestriction axisRestriction = AxisRestriction.NONE;

        public ScaleAction(GL_ControlBase control, Point mousePos, Vector3 center)
        {
            this.control = control;
            startMousePos = mousePos;
            this.center = center;
        }

        public override void UpdateMousePos(Point mousePos)
        {
            Point centerPoint = control.ScreenCoordFor(center);

            int x1 = mousePos.X - centerPoint.X;
            int y1 = mousePos.Y - centerPoint.Y;
            int x2 = startMousePos.X - centerPoint.X;
            int y2 = startMousePos.Y - centerPoint.Y;
            float scaling = (float)(Math.Sqrt(x1 * x1 + y1 * y1) / Math.Sqrt(x2 * x2 + y2 * y2));
            if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
            {
                scaling = (float)Math.Round(scaling * 2) * 0.5f;
            }

            switch (axisRestriction)
            {
                case AxisRestriction.NONE:
                    scale = new Vector3(scaling, scaling, scaling);
                    break;
                case AxisRestriction.X:
                    scale = new Vector3(scaling, 1, 1);
                    break;
                case AxisRestriction.Y:
                    scale = new Vector3(1, scaling, 1);
                    break;
                case AxisRestriction.Z:
                    scale = new Vector3(1, 1, scaling);
                    break;
                case AxisRestriction.YZ:
                    scale = new Vector3(1, scaling, scaling);
                    break;
                case AxisRestriction.XZ:
                    scale = new Vector3(scaling, 1, scaling);
                    break;
                case AxisRestriction.XY:
                    scale = new Vector3(scaling, scaling, 1);
                    break;
            }
        }

        public override void KeyDown(KeyEventArgs e)
        {
            AxisRestriction old = axisRestriction;
            switch (e.KeyCode)
            {
                case Keys.X:
                    axisRestriction = e.Shift ? AxisRestriction.YZ : AxisRestriction.X;
                    break;
                case Keys.Y:
                    axisRestriction = e.Shift ? AxisRestriction.XZ : AxisRestriction.Y;
                    break;
                case Keys.Z:
                    axisRestriction = e.Shift ? AxisRestriction.XY : AxisRestriction.Z;
                    break;
                default:
                    return;
            }

            if (axisRestriction == old)
                axisRestriction = AxisRestriction.NONE;
        }

        public override Vector3 NewPos(Vector3 pos)
        {
            return (pos - center) * scale + center;
        }

        public override void Draw(GL_ControlModern controlModern)
        {
            if (axisRestriction == AxisRestriction.NONE)
                return;

            controlModern.CurrentShader = Framework.SolidColorShaderProgram;

            control.ResetModelMatrix();

            GL.LineWidth(1.0f);

            if (axisRestriction == AxisRestriction.X || axisRestriction == AxisRestriction.XY || axisRestriction == AxisRestriction.XZ)
            {
                Framework.SolidColorShaderProgram.SetVector4("color", TransformRendering.colorX);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(center + Vector3.UnitX * control.ZFar);
                GL.Vertex3(center - Vector3.UnitX * control.ZFar);
                GL.End();
            }

            if (axisRestriction == AxisRestriction.Y || axisRestriction == AxisRestriction.XY || axisRestriction == AxisRestriction.YZ)
            {
                Framework.SolidColorShaderProgram.SetVector4("color", TransformRendering.colorY);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(center + Vector3.UnitY * control.ZFar);
                GL.Vertex3(center - Vector3.UnitY * control.ZFar);
                GL.End();
            }

            if (axisRestriction == AxisRestriction.Z || axisRestriction == AxisRestriction.XZ || axisRestriction == AxisRestriction.YZ)
            {
                Framework.SolidColorShaderProgram.SetVector4("color", TransformRendering.colorZ);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(center + Vector3.UnitZ * control.ZFar);
                GL.Vertex3(center - Vector3.UnitZ * control.ZFar);
                GL.End();
            }
        }

        public override void Draw(GL_ControlLegacy controlLegacy)
        {
            if (axisRestriction == AxisRestriction.NONE)
                return;

            control.ResetModelMatrix();

            GL.LineWidth(1.0f);

            if (axisRestriction == AxisRestriction.X || axisRestriction == AxisRestriction.XY || axisRestriction == AxisRestriction.XZ)
            {
                GL.Color4(TransformRendering.colorX);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(center + Vector3.UnitX * control.ZFar);
                GL.Vertex3(center - Vector3.UnitX * control.ZFar);
                GL.End();
            }

            if (axisRestriction == AxisRestriction.Y || axisRestriction == AxisRestriction.XY || axisRestriction == AxisRestriction.YZ)
            {
                GL.Color4(TransformRendering.colorY);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(center + Vector3.UnitY * control.ZFar);
                GL.Vertex3(center - Vector3.UnitY * control.ZFar);
                GL.End();
            }

            if (axisRestriction == AxisRestriction.Z || axisRestriction == AxisRestriction.XZ || axisRestriction == AxisRestriction.YZ)
            {
                GL.Color4(TransformRendering.colorZ);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(center + Vector3.UnitZ * control.ZFar);
                GL.Vertex3(center - Vector3.UnitZ * control.ZFar);
                GL.End();
            }
        }
    }

    public class ScaleActionIndividual : AbstractTransformAction
    {
        Point startMousePos;

        public override Vector3 NewScale(Vector3 _scale, Matrix3 rotation) => new Vector3(
            WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift) ? (float)Math.Round(scale.X * _scale.X * 2) * 0.5f : scale.X * _scale.X,
            WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift) ? (float)Math.Round(scale.Y * _scale.Y * 2) * 0.5f : scale.Y * _scale.Y,
            WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift) ? (float)Math.Round(scale.Z * _scale.Z * 2) * 0.5f : scale.Z * _scale.Z
            );

        public override Vector3 NewIndividualPos(Vector3 pos) => new Vector3(
            (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift) ? (float)Math.Round(scale.X * 2) * 0.5f : scale.X) * pos.X,
            (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift) ? (float)Math.Round(scale.Y * 2) * 0.5f : scale.Y) * pos.Y,
            (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift) ? (float)Math.Round(scale.Z * 2) * 0.5f : scale.Z) * pos.Z
            );

        Vector3 scale = Vector3.One;

        EditableObject.LocalOrientation orientation;

        enum AxisRestriction
        {
            NONE,
            X,
            Y,
            Z,
            YZ,
            XZ,
            XY
        }

        AxisRestriction axisRestriction = AxisRestriction.NONE;

        public ScaleActionIndividual(GL_ControlBase control, Point mousePos, EditableObject.LocalOrientation orientation)
        {
            this.control = control;
            startMousePos = mousePos;
            this.orientation = orientation;
        }

        public override void UpdateMousePos(Point mousePos)
        {
            Point centerPoint = control.ScreenCoordFor(orientation.Origin);

            int a1 = mousePos.X - centerPoint.X;
            int b1 = mousePos.Y - centerPoint.Y;
            int a2 = startMousePos.X - centerPoint.X;
            int b2 = startMousePos.Y - centerPoint.Y;
            float scaling = (float)(Math.Sqrt(a1 * a1 + b1 * b1) / Math.Sqrt(a2 * a2 + b2 * b2));

            switch (axisRestriction)
            {
                case AxisRestriction.NONE:
                    scale = new Vector3(scaling, scaling, scaling);
                    break;
                case AxisRestriction.X:
                    scale = new Vector3(scaling, 1, 1);
                    break;
                case AxisRestriction.Y:
                    scale = new Vector3(1, scaling, 1);
                    break;
                case AxisRestriction.Z:
                    scale = new Vector3(1, 1, scaling);
                    break;
                case AxisRestriction.YZ:
                    scale = new Vector3(1, scaling, scaling);
                    break;
                case AxisRestriction.XZ:
                    scale = new Vector3(scaling, 1, scaling);
                    break;
                case AxisRestriction.XY:
                    scale = new Vector3(scaling, scaling, 1);
                    break;
            }
        }

        public override Vector3 NewPos(Vector3 pos)
        {
            return pos;
        }

        public override void KeyDown(KeyEventArgs e)
        {
            AxisRestriction old = axisRestriction;
            switch (e.KeyCode)
            {
                case Keys.X:
                    axisRestriction = e.Shift ? AxisRestriction.YZ : AxisRestriction.X;
                    break;
                case Keys.Y:
                    axisRestriction = e.Shift ? AxisRestriction.XZ : AxisRestriction.Y;
                    break;
                case Keys.Z:
                    axisRestriction = e.Shift ? AxisRestriction.XY : AxisRestriction.Z;
                    break;
                default:
                    return;
            }

            if (axisRestriction == old)
                axisRestriction = AxisRestriction.NONE;
        }

        public override void Draw(GL_ControlModern controlModern)
        {
            if (axisRestriction == AxisRestriction.NONE)
                return;

            controlModern.CurrentShader = Framework.SolidColorShaderProgram;

            control.ResetModelMatrix();

            GL.LineWidth(1.0f);

            if (axisRestriction == AxisRestriction.X || axisRestriction == AxisRestriction.XY || axisRestriction == AxisRestriction.XZ)
            {
                Framework.SolidColorShaderProgram.SetVector4("color", TransformRendering.colorX);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(orientation.Origin + orientation.Rotation * Vector3.UnitX * control.ZFar);
                GL.Vertex3(orientation.Origin - orientation.Rotation * Vector3.UnitX * control.ZFar);
                GL.End();
            }

            if (axisRestriction == AxisRestriction.Y || axisRestriction == AxisRestriction.XY || axisRestriction == AxisRestriction.YZ)
            {
                Framework.SolidColorShaderProgram.SetVector4("color", TransformRendering.colorY);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(orientation.Origin + orientation.Rotation * Vector3.UnitY * control.ZFar);
                GL.Vertex3(orientation.Origin - orientation.Rotation * Vector3.UnitY * control.ZFar);
                GL.End();
            }

            if (axisRestriction == AxisRestriction.Z || axisRestriction == AxisRestriction.XZ || axisRestriction == AxisRestriction.YZ)
            {
                Framework.SolidColorShaderProgram.SetVector4("color", TransformRendering.colorZ);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(orientation.Origin + orientation.Rotation * Vector3.UnitZ * control.ZFar);
                GL.Vertex3(orientation.Origin - orientation.Rotation * Vector3.UnitZ * control.ZFar);
                GL.End();
            }
        }

        public override void Draw(GL_ControlLegacy controlLegacy)
        {
            if (axisRestriction == AxisRestriction.NONE)
                return;

            control.ResetModelMatrix();

            GL.LineWidth(1.0f);

            if (axisRestriction == AxisRestriction.X || axisRestriction == AxisRestriction.XY || axisRestriction == AxisRestriction.XZ)
            {
                GL.Color4(TransformRendering.colorX);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(orientation.Origin + orientation.Rotation * Vector3.UnitX * control.ZFar);
                GL.Vertex3(orientation.Origin - orientation.Rotation * Vector3.UnitX * control.ZFar);
                GL.End();
            }

            if (axisRestriction == AxisRestriction.Y || axisRestriction == AxisRestriction.XY || axisRestriction == AxisRestriction.YZ)
            {
                GL.Color4(TransformRendering.colorY);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(orientation.Origin + orientation.Rotation * Vector3.UnitY * control.ZFar);
                GL.Vertex3(orientation.Origin - orientation.Rotation * Vector3.UnitY * control.ZFar);
                GL.End();
            }

            if (axisRestriction == AxisRestriction.Z || axisRestriction == AxisRestriction.XZ || axisRestriction == AxisRestriction.YZ)
            {
                GL.Color4(TransformRendering.colorZ);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(orientation.Origin + orientation.Rotation * Vector3.UnitZ * control.ZFar);
                GL.Vertex3(orientation.Origin - orientation.Rotation * Vector3.UnitZ * control.ZFar);
                GL.End();
            }
        }
    }

    public class SnapAction : AbstractTransformAction
    {
        public override Vector3 NewPos(Vector3 pos)
        {
            return new Vector3(
                (float)Math.Round(pos.X),
                (float)Math.Round(pos.Y),
                (float)Math.Round(pos.Z)
                );
        }
    }

    public class ResetRot : AbstractTransformAction
    {
        public override Matrix3 NewRot(Matrix3 rot) => Matrix3.Identity;
    }

    public class ResetScale : AbstractTransformAction
    {
        public override Vector3 NewScale(Vector3 rot, Matrix3 rotation) => Vector3.One;
    }

    public class PropertyChanges : AbstractTransformAction
    {
        public Vector3 translation = Vector3.Zero;
        public Matrix3? rotOverride = null;
        public Vector3? scaleOverride = null;

        public override Vector3 NewPos(Vector3 pos)
        {
            return pos + translation;
        }

        public override Matrix3 NewRot(Matrix3 rot)
        {
            if (rotOverride.HasValue)
                return rotOverride.Value;
            else
                return rot;
        }

        public override Vector3 NewScale(Vector3 scale, Matrix3 rotation)
        {
            if (scaleOverride.HasValue)
                return scaleOverride.Value;
            else
                return scale;
        }

        public override bool IsApplyOnRelease() => false;
    }

    public class NoTransformAction : AbstractTransformAction
    {

    }
}
