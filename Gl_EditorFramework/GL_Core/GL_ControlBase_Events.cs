using GL_EditorFramework.Interfaces;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GL_EditorFramework.GL_Core
{
	public partial class GL_ControlBase
	{
		void handleDrawableEvtResult(uint result)
		{
			shouldRedraw |= (result & AbstractGlDrawable.REDRAW) > 0;
			shouldRepick |= (result & AbstractGlDrawable.REPICK) > 0;
			skipCameraAction |= (result & AbstractGlDrawable.NO_CAMERA_ACTION) > 0;
		}

		void handleCameraEvtResult(uint result)
		{
			shouldRedraw |= result > 0;
			shouldRepick |= result > 0;
			if (shouldRepick)
			{
				mtxRotInv =
					Matrix3.CreateRotationX(-camRotY) *
					Matrix3.CreateRotationY(-camRotX);

			}
		}


		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (DesignMode || mainDrawable == null) return;

			Focus();

			RotXIsReversed = (Framework.HALF_PI < camRotY && camRotY < (Framework.PI + Framework.HALF_PI));

			lastMouseLoc = e.Location;
			if (dragStartPos == new Point(-1, -1))
				dragStartPos = e.Location;

			shouldRedraw = false;
			shouldRepick = false;
			skipCameraAction = false;
			handleDrawableEvtResult(mainDrawable.MouseDown(e, this));

			if (!skipCameraAction)
				handleCameraEvtResult(activeCamera.MouseDown(e, this));

			if (shouldRepick)
				Repick();

			if (shouldRedraw)
				Refresh();

			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (DesignMode || mainDrawable == null) return;

			shouldRedraw = false;
			shouldRepick = false;
			skipCameraAction = false;

			handleDrawableEvtResult(mainDrawable.MouseMove(e, lastMouseLoc, this));

			if (!skipCameraAction)
			{
				handleCameraEvtResult(activeCamera.MouseMove(e, lastMouseLoc, this));
			}

			if (shouldRepick)
				Repick();

			if (shouldRedraw || showFakeCursor)
				Refresh();

			lastMouseLoc = e.Location;
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (DesignMode || mainDrawable == null) return;

			shouldRedraw = false;
			shouldRepick = false;
			skipCameraAction = false;

			handleDrawableEvtResult(mainDrawable.MouseWheel(e, this));

			if (!skipCameraAction)
				handleCameraEvtResult(activeCamera.MouseWheel(e, this));

			if (shouldRepick)
				Repick();

			if (shouldRedraw)
				Refresh();
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (DesignMode || mainDrawable == null) return;

			shouldRedraw = false;
			shouldRepick = false;
			skipCameraAction = false;

			if ((e.Location.X == dragStartPos.X) && (e.Location.Y == dragStartPos.Y))
			{
				shouldRedraw = true;
				switch (showOrientationCube ? pickingFrameBuffer : 0)
				{
					case 1:
						camRotX = 0;
						camRotY = Framework.HALF_PI;
						break;
					case 2:
						camRotX = 0;
						camRotY = -Framework.HALF_PI;
						break;
					case 3:
						camRotX = 0;
						camRotY = 0;
						break;
					case 4:
						camRotX = Framework.PI;
						camRotY = 0;
						break;
					case 5:
						camRotX = -Framework.HALF_PI;
						camRotY = 0;
						break;
					case 6:
						camRotX = Framework.HALF_PI;
						camRotY = 0;
						break;
					default:
						shouldRedraw = false;
						handleDrawableEvtResult(mainDrawable.MouseClick(e, this));
						break;
				}
				if (!skipCameraAction)
					handleCameraEvtResult(activeCamera.MouseClick(e, this));
			}
			else
			{
				handleDrawableEvtResult(mainDrawable.MouseUp(e, this));
			}

			dragStartPos = new Point(-1, -1);

			if (!skipCameraAction)
				handleCameraEvtResult(activeCamera.MouseUp(e, this));

			if (shouldRepick)
				Repick();

			if (shouldRedraw)
				Refresh();
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			if (DesignMode)
			{
				base.OnMouseEnter(e);
				return;
			}
			if (stereoscopy)
			{
				showFakeCursor = true;
				Cursor.Hide();
			}
			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			if (DesignMode)
			{
				base.OnMouseLeave(e);
				return;
			}
			if (stereoscopy)
			{
				showFakeCursor = false;
				Cursor.Show();
			}
			base.OnMouseLeave(e);
			Refresh();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (DesignMode || mainDrawable == null) return;

			shouldRedraw = false;
			shouldRepick = false;
			skipCameraAction = false;

			handleDrawableEvtResult(mainDrawable.KeyDown(e, this));

			if (!skipCameraAction)
				handleCameraEvtResult(activeCamera.KeyDown(e, this));

			if (shouldRepick)
				Repick();

			if (shouldRedraw)
				Refresh();
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (DesignMode || mainDrawable == null) return;

			shouldRedraw = false;
			shouldRepick = false;
			skipCameraAction = false;

			handleDrawableEvtResult(mainDrawable.KeyUp(e, this));

			if (skipCameraAction)
				handleCameraEvtResult(activeCamera.KeyUp(e, this));

			if (shouldRepick)
				Repick();

			if (shouldRedraw)
				Refresh();
		}
	}
}
