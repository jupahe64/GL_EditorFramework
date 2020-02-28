using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinInput = System.Windows.Input;

namespace GL_EditorFramework.StandardCameras
{
    public class InspectCamera : AbstractCamera
    {
        private readonly float maxCamMoveSpeed;

        public InspectCamera(float maxCamMoveSpeed = 0.1f)
        {
            this.maxCamMoveSpeed = maxCamMoveSpeed;
        }

        public override uint MouseClick(MouseEventArgs e, GL_ControlBase control)
        {
            if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftCtrl) &&
                e.Button == MouseButtons.Right &&
                control.PickingDepth != control.ZFar)
            {
                control.CameraTarget = -control.CoordFor(e.Location.X, e.Location.Y, control.PickingDepth);

                return UPDATE_CAMERA;
            }
            return base.MouseDown(e, control);
        }

        public override uint MouseMove(MouseEventArgs e, Point lastMouseLoc, GL_ControlBase control)
        {
            float deltaX = e.Location.X - lastMouseLoc.X;
            float deltaY = e.Location.Y - lastMouseLoc.Y;

            if (e.Button == MouseButtons.Right)
            {
                if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftCtrl))
                    control.CameraDistance *= 1f - deltaY * -5 * 0.001f;
                else
                {
                    if (!WinInput.Keyboard.IsKeyDown(WinInput.Key.Y))
                        control.RotateCameraX(deltaX * 0.002f);
                    if (!WinInput.Keyboard.IsKeyDown(WinInput.Key.X))
                        control.CamRotY += deltaY * 0.002f;
                }
                return UPDATE_CAMERA;
            }
            else if (e.Button == MouseButtons.Left)
            {
                base.MouseMove(e, lastMouseLoc, control);

                //code from Whitehole

                Vector3 vec;
                if (!WinInput.Keyboard.IsKeyDown(WinInput.Key.Y))
                    vec.X =  deltaX * Math.Min(maxCamMoveSpeed, depth * control.FactorX);
                else
                    vec.X = 0;
                if (!WinInput.Keyboard.IsKeyDown(WinInput.Key.X))
                    vec.Y = -deltaY * Math.Min(maxCamMoveSpeed, depth * control.FactorY);
                else
                    vec.Y = 0;

                vec.Z = 0;
                control.CameraTarget -= Vector3.Transform(control.InvertedRotationMatrix, vec);

                return UPDATE_CAMERA;
            }

            return 0;
        }

        public override uint MouseWheel(MouseEventArgs e, GL_ControlBase control)
        {
            if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftCtrl))
            {
                depth = control.PickingDepth;
                float delta = e.Delta * Math.Min(0.01f, depth / 500f);

                Vector2 normCoords = control.NormMouseCoords(e.Location.X, e.Location.Y);

                Vector3 vec = control.InvertedRotationMatrix.Row0 * -normCoords.X * delta * control.FactorX+
                              control.InvertedRotationMatrix.Row1 *  normCoords.Y * delta * control.FactorY+
                              control.InvertedRotationMatrix.Row2 * delta;

                control.CameraTarget -= vec;
            }else
                control.CameraDistance *= 1f - e.Delta * 0.001f;

            return UPDATE_CAMERA;
        }
    }
}
