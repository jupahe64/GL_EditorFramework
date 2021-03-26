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
    public class WalkaroundCamera : AbstractCamera
    {
        private readonly float maxCamMoveSpeed;

        private readonly float rotFactorX = 0.002f;
        private readonly float rotFactorY = 0.002f;

        public WalkaroundCamera(float maxCamMoveSpeed = 0.1f, bool invertX = false, bool invertY = false)
        {
            this.maxCamMoveSpeed = maxCamMoveSpeed;

            if (invertX)
                rotFactorX *= -1;
            if (invertY)
                rotFactorY *= -1;
        }

        public override uint MouseClick(MouseEventArgs e, GL_ControlBase control)
        {
            if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftCtrl) &&
                e.Button == MouseButtons.Right &&
                control.PickingDepth != control.ZFar)
            {
                control.CameraTarget = -control.CoordFor(e.Location.X, e.Location.Y, control.PickingDepth);
            }
            base.MouseDown(e, control);
            return UPDATE_CAMERA;
        }

        public override uint Update(GL_ControlBase control, float deltaTime)
        {
            if (control.MainDrawable == null || !control.Focused || WinInput.Keyboard.Modifiers!=WinInput.ModifierKeys.None)
                return 0;

            float speed = deltaTime * 0.01f;

            Vector3 vec = Vector3.Zero;

            if (WinInput.Keyboard.IsKeyDown(WinInput.Key.W)) vec.Z -= speed;
            if (WinInput.Keyboard.IsKeyDown(WinInput.Key.A)) vec.X -= speed;
            if (WinInput.Keyboard.IsKeyDown(WinInput.Key.S)) vec.Z += speed;
            if (WinInput.Keyboard.IsKeyDown(WinInput.Key.D)) vec.X += speed;


            Point mousePos = control.GetMousePos();

            Vector2 normCoords = control.NormMouseCoords(mousePos.X, mousePos.Y);

            vec.X += (-normCoords.X * vec.Z) * control.FactorX;
            vec.Y += (normCoords.Y * vec.Z) * control.FactorY;


            float up = 0;

            if (WinInput.Keyboard.IsKeyDown(WinInput.Key.Q)) up -= speed;
            if (WinInput.Keyboard.IsKeyDown(WinInput.Key.E)) up += speed;

            if (vec == Vector3.Zero && up == 0)
                return 0;

            control.CameraTarget += Vector3.Transform(control.InvertedRotationMatrix, vec) + Vector3.UnitY * up;

            (control.MainDrawable as EditorDrawables.EditorSceneBase)?.CurrentAction?.UpdateMousePos(mousePos);
            (control.MainDrawable as EditorDrawables.EditorSceneBase)?.SelectionTransformAction.UpdateMousePos(mousePos);

            return UPDATE_CAMERA;
        }

        public override uint MouseMove(MouseEventArgs e, Point lastMouseLoc, GL_ControlBase control)
        {
            float deltaX = e.Location.X - lastMouseLoc.X;
            float deltaY = e.Location.Y - lastMouseLoc.Y;

            if (e.Button == MouseButtons.Right)
            {
                if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftCtrl))
                {
                    float delta = ((float)deltaY * -5 * Math.Min(0.01f, depth / 500f));
                    Vector3 vec;
                    vec.X = 0;
                    vec.Y = 0;
                    vec.Z = delta;

                    control.CameraTarget -= Vector3.Transform(control.InvertedRotationMatrix, vec);
                }
                else
                {
                    if (!WinInput.Keyboard.IsKeyDown(WinInput.Key.Y))
                        control.RotateCameraX(deltaX * rotFactorX);
                    if (!WinInput.Keyboard.IsKeyDown(WinInput.Key.X))
                        control.CamRotY += deltaY * rotFactorY;
                }

                return UPDATE_CAMERA;
            }
            else if (e.Button == MouseButtons.Left)
            {
                base.MouseMove(e, lastMouseLoc, control);

                //code from Whitehole

                Vector3 vec;
                if (!WinInput.Keyboard.IsKeyDown(WinInput.Key.Y))
                    vec.X = deltaX * Math.Min(maxCamMoveSpeed, depth * control.FactorX);
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
            depth = control.PickingDepth;
            float delta = (e.Delta * Math.Min(WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift) ? 0.04f : 0.01f, depth / 500f));
            Vector3 vec;

            Vector2 normCoords = control.NormMouseCoords(e.Location.X, e.Location.Y);

            vec.X = (-normCoords.X * delta) * control.FactorX;
            vec.Y = ( normCoords.Y * delta) * control.FactorY;
            vec.Z = delta;

            control.CameraTarget -= Vector3.Transform(control.InvertedRotationMatrix, vec);
            
            return UPDATE_CAMERA;
        }
    }
}
