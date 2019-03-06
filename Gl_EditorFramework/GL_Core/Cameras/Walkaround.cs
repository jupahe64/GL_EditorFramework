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
	public class WalkaroundCamera : AbstractCamera
	{
		private float maxCamMoveSpeed;

		public WalkaroundCamera(float maxCamMoveSpeed = 0.1f)
		{
			this.maxCamMoveSpeed = maxCamMoveSpeed;
		}

		public override uint MouseDown(MouseEventArgs e, I3DControl control)
		{
			if (OpenTK.Input.Keyboard.GetState().IsKeyDown(OpenTK.Input.Key.ControlLeft) &&
				e.Button == MouseButtons.Right &&
				control.PickingDepth != control.ZFar)
			{
				control.CameraTarget = control.coordFor(e.Location.X, e.Location.Y, control.PickingDepth);
			}
			base.MouseDown(e, control);
			return UPDATE_CAMERA;
		}

		public override uint MouseMove(MouseEventArgs e, Point lastMouseLoc, I3DControl control)
		{
			float deltaX = e.Location.X - lastMouseLoc.X;
			float deltaY = e.Location.Y - lastMouseLoc.Y;

			if (e.Button == MouseButtons.Right)
			{
				control.CamRotX += deltaX * 0.00390625f;
				control.CamRotY += deltaY * 0.00390625f;
				return UPDATE_CAMERA;
			}
			else if (e.Button == MouseButtons.Left)
			{
				base.MouseMove(e, lastMouseLoc, control);

				if (OpenTK.Input.Keyboard.GetState().IsKeyDown(OpenTK.Input.Key.ControlLeft))
				{
					float delta = ((float)deltaY*-5 * Math.Min(0.01f, depth / 500f));
					Vector3 vec;
					vec.X = 0;
					vec.Y = 0;
					vec.Z = delta;

					control.CameraTarget += Vector3.Transform(control.InvertedRotationMatrix, vec);
				}
				else
				{

					//code from Whitehole

					Vector3 vec;
					vec.X = deltaX * Math.Min(maxCamMoveSpeed, depth * control.FactorX);
					vec.Y = -deltaY * Math.Min(maxCamMoveSpeed, depth * control.FactorY);
					vec.Z = 0;
					control.CameraTarget += Vector3.Transform(control.InvertedRotationMatrix, vec);
				}

				return UPDATE_CAMERA;
			}
			return 0;
		}

		public override uint MouseWheel(MouseEventArgs e, I3DControl control)
		{
			depth = control.PickingDepth;
			float delta = ((float)e.Delta * Math.Min(0.01f, depth / 500f));
			Vector3 vec;

			Vector2 normCoords = control.NormMouseCoords(e.Location.X, e.Location.Y);

			vec.X = (float)(-normCoords.X * delta) * control.FactorX;
			vec.Y = (float)( normCoords.Y * delta) * control.FactorY;
			vec.Z = delta;

			control.CameraTarget += Vector3.Transform(control.InvertedRotationMatrix, vec);
			
			return UPDATE_CAMERA;
		}
	}
}
