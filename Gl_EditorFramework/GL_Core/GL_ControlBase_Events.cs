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
        private bool shouldRedraw;
        private bool shouldRepick;
        private bool skipCameraAction;
        private bool forceReEnter;

        void HandleDrawableEvtResult(uint result)
        {
            shouldRedraw |= (result & AbstractGlDrawable.REDRAW) > 0;
            shouldRepick |= (result & AbstractGlDrawable.REPICK) > 0;
            skipCameraAction |= (result & AbstractGlDrawable.NO_CAMERA_ACTION) > 0;
            forceReEnter |= (result & AbstractGlDrawable.FORCE_REENTER) > 0;
        }

        void HandleCameraEvtResult(uint result)
        {
            shouldRedraw |= result > 0;
            shouldRepick |= result > 0;
            if (shouldRepick)
            {
                mtxRotInv =
                    Matrix3.CreateRotationX(-camRotY) *
                    Matrix3.CreateRotationY(-camRotX);

                CameraPosition = CameraTarget + mtxRotInv.Row2*CameraDistance;
            }
        }


        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (DesignMode || mainDrawable == null) return;

            Focus();

            RotXIsReversed = (Framework.HALF_PI < camRotY && camRotY < (Framework.PI + Framework.HALF_PI));

            lastMouseLoc = e.Location;
            if (dragStartPos == new Point(-1, -1))
                dragStartPos = e.Location;

            shouldRedraw = false;
            shouldRepick = false;
            skipCameraAction = false;
            forceReEnter = false;

            HandleDrawableEvtResult(mainDrawable.MouseDown(e, this));

            if (!skipCameraAction)
                HandleCameraEvtResult(activeCamera.MouseDown(e, this));

            if (shouldRepick)
                Repick();

            if (shouldRedraw)
                Refresh();

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (DesignMode || mainDrawable == null) return;

            shouldRedraw = false;
            shouldRepick = false;
            skipCameraAction = false;
            forceReEnter = false;

            HandleDrawableEvtResult(mainDrawable.MouseMove(e, lastMouseLoc, this));

            if (!skipCameraAction)
            {
                HandleCameraEvtResult(activeCamera.MouseMove(e, lastMouseLoc, this));
            }

            if (shouldRepick)
                Repick();

            if (shouldRedraw || showFakeCursor)
                Refresh();

            lastMouseLoc = e.Location;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (DesignMode || mainDrawable == null) return;

            shouldRedraw = false;
            shouldRepick = false;
            skipCameraAction = false;
            forceReEnter = false;

            HandleDrawableEvtResult(mainDrawable.MouseWheel(e, this));

            if (!skipCameraAction)
                HandleCameraEvtResult(activeCamera.MouseWheel(e, this));

            if (shouldRepick)
                Repick();

            if (shouldRedraw)
                Refresh();
        }

        public new ContextMenuStrip ContextMenuStrip { get; set; }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (DesignMode || mainDrawable == null) return;

            shouldRedraw = false;
            shouldRepick = false;
            skipCameraAction = false;
            forceReEnter = false;

            HandleDrawableEvtResult(mainDrawable.MouseUp(e, this));

            if ((e.Location.X == dragStartPos.X) && (e.Location.Y == dragStartPos.Y))
            {
                shouldRedraw = true;
                switch (showOrientationCube ? pickingFrameBuffer : 0)
                {
                    #region generated code
                    case 1:
                        CamRotX = 0f;
                        CamRotY = -1.570796f;
                        break;
                    case 2:
                        CamRotX = 1.570796f;
                        CamRotY = 0f;
                        break;
                    case 3:
                        CamRotX = 0f;
                        CamRotY = 0f;
                        break;
                    case 4:
                        CamRotX = 0f;
                        CamRotY = 1.570796f;
                        break;
                    case 5:
                        CamRotX = -1.570796f;
                        CamRotY = 0f;
                        break;
                    case 6:
                        CamRotX = -2.356194f;
                        CamRotY = 0.6155406f;
                        break;
                    case 7:
                        CamRotX = -2.356194f;
                        CamRotY = -0.6155406f;
                        break;
                    case 8:
                        CamRotX = -0.7853982f;
                        CamRotY = 0.6155406f;
                        break;
                    case 9:
                        CamRotX = -0.7853982f;
                        CamRotY = -0.6155406f;
                        break;
                    case 10:
                        CamRotX = 2.356194f;
                        CamRotY = 0.6155406f;
                        break;
                    case 11:
                        CamRotX = 2.356194f;
                        CamRotY = -0.6155406f;
                        break;
                    case 12:
                        CamRotX = 0.7853982f;
                        CamRotY = 0.6155406f;
                        break;
                    case 13:
                        CamRotX = 0.7853982f;
                        CamRotY = -0.6155406f;
                        break;
                    case 14:
                        CamRotX = 1.570796f;
                        CamRotY = -0.7853885f;
                        break;
                    case 15:
                        CamRotX = -3.141593f;
                        CamRotY = -0.7853885f;
                        break;
                    case 16:
                        CamRotX = -2.356194f;
                        CamRotY = 0f;
                        break;
                    case 17:
                        CamRotX = 0.7853982f;
                        CamRotY = 0f;
                        break;
                    case 18:
                        CamRotX = -0.7853982f;
                        CamRotY = 0f;
                        break;
                    case 19:
                        CamRotX = 2.356194f;
                        CamRotY = 0f;
                        break;
                    case 20:
                        CamRotX = 0f;
                        CamRotY = 0.7853885f;
                        break;
                    case 21:
                        CamRotX = -1.570796f;
                        CamRotY = 0.7853885f;
                        break;
                    case 22:
                        CamRotX = 0f;
                        CamRotY = -0.7853885f;
                        break;
                    case 23:
                        CamRotX = 1.570796f;
                        CamRotY = 0.7853885f;
                        break;
                    case 24:
                        CamRotX = -3.141593f;
                        CamRotY = 0.7853885f;
                        break;
                    case 25:
                        CamRotX = -1.570796f;
                        CamRotY = -0.7853885f;
                        break;
                    case 26:
                        CamRotX = -3.141593f;
                        CamRotY = 0f;
                        break;


                    #endregion
                    default:
                        shouldRedraw = false;
                        if (e.Button == MouseButtons.Right && ModifierKeys == Keys.None)
                            ContextMenuStrip?.Show(this, e.Location);
                        else
                            HandleDrawableEvtResult(mainDrawable.MouseClick(e, this));
                        break;
                }
                if (!skipCameraAction)
                    HandleCameraEvtResult(activeCamera.MouseClick(e, this));
            }

            dragStartPos = new Point(-1, -1);

            if (!skipCameraAction)
                HandleCameraEvtResult(activeCamera.MouseUp(e, this));

            if (shouldRepick)
                Repick();

            if (shouldRedraw)
                Refresh();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
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
            base.OnMouseLeave(e);
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

        HashSet<Keys> pressedKeyCodes = new HashSet<Keys>();

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (DesignMode || mainDrawable == null) return;

            shouldRedraw = false;
            shouldRepick = false;
            skipCameraAction = false;
            forceReEnter = false;

            HandleDrawableEvtResult(mainDrawable.KeyDown(e, this, pressedKeyCodes.Contains(e.KeyCode)));

            if (!skipCameraAction)
                HandleCameraEvtResult(activeCamera.KeyDown(e, this, pressedKeyCodes.Contains(e.KeyCode)));

            pressedKeyCodes.Add(e.KeyCode);

            if (shouldRepick)
                Repick();

            if (shouldRedraw)
                Refresh();
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (DesignMode || mainDrawable == null) return;

            shouldRedraw = false;
            shouldRepick = false;
            skipCameraAction = false;
            forceReEnter = false;

            HandleDrawableEvtResult(mainDrawable.KeyUp(e, this));

            if (skipCameraAction)
                HandleCameraEvtResult(activeCamera.KeyUp(e, this));

            pressedKeyCodes.Remove(e.KeyCode);

            if (shouldRepick)
                Repick();

            if (shouldRedraw)
                Refresh();
        }
    }
}
