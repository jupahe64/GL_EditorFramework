using GL_EditorFramework.Interfaces;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GL_EditorFramework.StandardCameras
{
	public class InspectCamera : AbstractCamera
	{
		public InspectCamera()
		{

		}

		public override uint MouseDown(MouseEventArgs e, I3DControl control)
		{
			if (OpenTK.Input.Keyboard.GetState().IsKeyDown(OpenTK.Input.Key.ControlLeft) &&
				e.Button == MouseButtons.Right &&
				control.PickingDepth != control.ZFar)
			{
				float delta = control.PickingDepth + control.CameraDistance;
				control.CameraTarget -= Vector3.UnitX * (float)Math.Sin(control.CamRotX) * (float)Math.Cos(control.CamRotY) * delta;
				control.CameraTarget += Vector3.UnitY * (float)Math.Sin(control.CamRotY) * delta;
				control.CameraTarget += Vector3.UnitZ * (float)Math.Cos(control.CamRotX) * (float)Math.Cos(control.CamRotY) * delta;

				Vector2 normCoords = control.NormMouseCoords(e.Location.X, e.Location.Y);

				float factoffX = (float)(-normCoords.X * control.PickingDepth) * control.FactorX;
				float factoffY = (float)(-normCoords.Y * control.PickingDepth) * control.FactorY;

				control.CameraTarget += Vector3.UnitX * (float)Math.Cos(control.CamRotX) * factoffX;
				control.CameraTarget -= Vector3.UnitX * (float)Math.Sin(control.CamRotX) * (float)Math.Sin(control.CamRotY) * factoffY;
				control.CameraTarget -= Vector3.UnitY * (float)Math.Cos(control.CamRotY) * factoffY;
				control.CameraTarget += Vector3.UnitZ * (float)Math.Sin(control.CamRotX) * factoffX;
				control.CameraTarget += Vector3.UnitZ * (float)Math.Cos(control.CamRotX) * (float)Math.Sin(control.CamRotY) * factoffY;
			}
			return base.MouseDown(e, control);
		}

		public override uint MouseMove(MouseEventArgs e, Point lastMouseLoc, I3DControl control)
		{
			float deltaX = e.Location.X - lastMouseLoc.X;
			float deltaY = e.Location.Y - lastMouseLoc.Y;

			if (e.Button == MouseButtons.Right)
			{
				control.CamRotX += deltaX * 0.002f;
				control.CamRotY += deltaY * 0.002f;
				return UPDATE_CAMERA;
			}
			else if (e.Button == MouseButtons.Left)
			{
				base.MouseMove(e, lastMouseLoc, control);

				if (OpenTK.Input.Keyboard.GetState().IsKeyDown(OpenTK.Input.Key.ControlLeft))
					control.CameraDistance *= 1f - deltaY*-5 * 0.001f;
				else
				{
					//code from Whitehole

					deltaX *= Math.Min(0.1f, depth * control.FactorX);
					deltaY *= Math.Min(0.1f, depth * control.FactorY);

					control.CameraTarget += Vector3.UnitX * deltaX * (float)Math.Cos(control.CamRotX);
					control.CameraTarget -= Vector3.UnitX * deltaY * (float)Math.Sin(control.CamRotX) * (float)Math.Sin(control.CamRotY);
					control.CameraTarget -= Vector3.UnitY * deltaY * (float)Math.Cos(control.CamRotY);
					control.CameraTarget += Vector3.UnitZ * deltaX * (float)Math.Sin(control.CamRotX);
					control.CameraTarget += Vector3.UnitZ * deltaY * (float)Math.Cos(control.CamRotX) * (float)Math.Sin(control.CamRotY);
				}

				return UPDATE_CAMERA;
			}

			return 0;
		}

		public override uint MouseWheel(MouseEventArgs e, I3DControl control)
		{
			control.CameraDistance *= 1f - e.Delta * 0.001f;
			return UPDATE_CAMERA;
		}
	}
}
