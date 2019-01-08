using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using GL_EditorFramework.Interfaces;
using GL_EditorFramework.StandardCameras;
using System.Drawing;
using System.Windows.Forms;

namespace GL_EditorFramework.GL_Core
{
	public class GL_ControlBase : GLControl, I3DControl
	{
		public GL_ControlBase(int maxGL_Version, int redrawerInterval) : base(OpenTK.Graphics.GraphicsMode.Default, maxGL_Version, 1, OpenTK.Graphics.GraphicsContextFlags.Default)
		{
			redrawer.Interval = redrawerInterval;
			redrawer.Tick += Redrawer_Tick;
		}

		private void Redrawer_Tick(object sender, EventArgs e)
		{
			base.Refresh();
			if (repickerOwners > 0)
				Repick();
			RedrawerFrame++;
		}

		public GL_ControlBase() : base(OpenTK.Graphics.GraphicsMode.Default, 1, 1, OpenTK.Graphics.GraphicsContextFlags.Default)
		{
			
		}

		public Color ViewportColor = Color.FromArgb(20,20,20);

		protected Matrix4 orientationCubeMtx;

		protected bool showFakeCursor;

		private Timer redrawer = new Timer();

		private uint redrawerOwners = 0;

		private uint repickerOwners = 0;

		protected Point lastMouseLoc;
		protected Point dragStartPos;
		protected float camRotX = 0;
		protected float camRotY = 0;
		protected float camDistance = -10f;
		protected Vector3 camTarget;

		protected float zfar = 1000f;
		protected float znear = 0.01f;
		protected float fov = MathHelper.PiOver4;

		protected uint[] pickingFrameBuffer = new uint[9];
		protected int pickingIndex;
		private int lastPicked = -1;
		protected float pickingModelDepth = 0f;
		protected float pickingDepth = 0f;

		protected Matrix4 mtxMdl, mtxCam, mtxProj;
		protected float factorX, factorY;

		protected bool stereoscopy;

		protected int viewPortX(int x) => stereoscopy ? x % (Width / 2) : x;
		protected int viewPortDX(int dx) => stereoscopy ? dx * 2 : dx;
		protected int viewPortXOff(int x) => stereoscopy ? (x - Width / 4) * 2 : x - Width / 2;

		public Point DragStartPos
		{
			get => dragStartPos;
			set
			{
				dragStartPos = value;
			}
		}

		public bool Stereoscopy
		{
			get => stereoscopy;
			set
			{
				stereoscopy = value;
				OnResize(null);
				Refresh();
			}
		}

		protected AbstractGlDrawable mainDrawable;
		public virtual AbstractGlDrawable MainDrawable { get; set; }

		protected AbstractCamera activeCamera;
		private bool shouldRedraw;
		private bool shouldRepick;
		private bool skipCameraAction;
		private const int pickingIndexOffset = 7;

		public AbstractCamera ActiveCamera
		{
			get => activeCamera;
			set
			{
				if (value == null) return;
				activeCamera = value;
				MakeCurrent();
				Refresh();
			}
		}

		public int ViewWidth => stereoscopy ? Width / 2 : Width;

		public int ViewHeighth => Height;

		public Vector2 NormMouseCoords(int x, int y) {
			return new Vector2(x - Width / 2, y - Height / 2);
		}

		public float ZFar { get => zfar; set { zfar = value; } }
		public float ZNear { get => znear; set { znear = value; } }
		public float Fov { get => fov; set { fov = value; } }

		public float FactorX => factorX;

		public float FactorY => factorY;

		public Vector3 CameraTarget { get => camTarget; set { camTarget = value; } }
		public float CameraDistance { get => camDistance; set { camDistance = value; } }
		public float CamRotX { get => camRotX; set { camRotX = value; } }
		public float CamRotY { get => camRotY; set { camRotY = value; } }

		public float PickingDepth => pickingDepth;
		public ulong RedrawerFrame { get; private set; } = 0;

		void handleDrawableEvtResult(uint result)
		{
			shouldRedraw |= (result&AbstractGlDrawable.REDRAW)> 0;
			shouldRepick |= (result & AbstractGlDrawable.REPICK) > 0;
			skipCameraAction |= (result & AbstractGlDrawable.NO_CAMERA_ACTION) > 0;
		}

		void handleCameraEvtResult(uint result)
		{
			shouldRedraw |= result > 0;
			shouldRepick |= result > 0;
		}

		public Color nextPickingColor()
		{
			return Color.FromArgb(pickingIndex++);
		}

		public void skipPickingColors(uint count)
		{
			pickingIndex += (int)count;
		}

		public virtual void UpdateModelMatrix(Matrix4 matrix) { }
		public virtual void ApplyModelTransform(Matrix4 matrix) { }

		public virtual void ResetModelMatrix() { }

		protected override void OnLoad(EventArgs e)
		{
			
			if (DesignMode) return;

			activeCamera = new WalkaroundCamera();

			
			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.CullFace);
			GL.CullFace(CullFaceMode.Front); GL.DepthFunc(DepthFunction.Lequal);
			GL.Enable(EnableCap.Texture2D);

			GL.Enable(EnableCap.AlphaTest);
			
			GL.Enable(EnableCap.LineSmooth);
			GL.Enable(EnableCap.PolygonSmooth);
			
		}

		protected override void OnResize(EventArgs e)
		{
			if (DesignMode) return;

			float aspect_ratio;
			if (stereoscopy)
				aspect_ratio = Width / 2 / (float)Height;
			else
				aspect_ratio = Width / (float)Height;

			mtxProj = Matrix4.CreatePerspectiveFieldOfView(fov, aspect_ratio, znear, zfar);

			//using the calculation from whitehole
			factorX = (2f * (float)Math.Tan(fov * 0.5f) * aspect_ratio) / Width;

			factorY = (2f * (float)Math.Tan(fov * 0.5f)) / Height;
			Refresh();
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (DesignMode || mainDrawable == null) return;

			Focus();

			lastMouseLoc = e.Location;
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

			if (shouldRedraw||showFakeCursor)
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

			if((e.Location.X == dragStartPos.X) && (e.Location.Y == dragStartPos.Y))
			{
				shouldRedraw = true;
				switch (pickingFrameBuffer[0])
				{
					case 1:
						camRotX = 0;
						camRotY = (float)Math.PI * 0.5f;
						break;
					case 2:
						camRotX = 0;
						camRotY = -(float)Math.PI * 0.5f;
						break;
					case 3:
						camRotX = 0;
						camRotY = 0;
						break;
					case 4:
						camRotX = (float)Math.PI;
						camRotY = 0;
						break;
					case 5:
						camRotX = -(float)Math.PI * 0.5f;
						camRotY = 0;
						break;
					case 6:
						camRotX = (float)Math.PI * 0.5f;
						camRotY = 0;
						break;
					default:
						shouldRedraw = false;
						handleDrawableEvtResult(mainDrawable.MouseClick(e, this));
						break;
				}
			}
			else
			{
				handleDrawableEvtResult(mainDrawable.MouseUp(e, this));
			}

			if (!skipCameraAction)
				handleCameraEvtResult(activeCamera.MouseUp(e, this));

			if (shouldRepick)
				Repick();

			if (shouldRedraw)
				Refresh();
		}

		protected void Repick()
		{
			int pickingMouseX = stereoscopy ? lastMouseLoc.X / 2 : lastMouseLoc.X;

			pickingIndex = 1;

			DrawPicking();
			GL.Flush();

			GL.ReadPixels(pickingMouseX, Height - lastMouseLoc.Y, 1, 1, PixelFormat.DepthComponent, PixelType.Float, ref pickingModelDepth);

			pickingModelDepth = -(zfar * znear / (pickingModelDepth * (zfar - znear) - zfar));



			GL.Flush();

			GL.ReadPixels(pickingMouseX, Height - lastMouseLoc.Y, 1, 1, PixelFormat.Bgra, PixelType.UnsignedByte, pickingFrameBuffer);
			
			// depth math from http://www.opengl.org/resources/faq/technical/depthbuffer.htm

			GL.ReadPixels(pickingMouseX, Height - lastMouseLoc.Y, 1, 1, PixelFormat.DepthComponent, PixelType.Float, ref pickingDepth);

			pickingDepth = -(zfar * znear / (pickingDepth * (zfar - znear) - zfar));


			int picked = (int)pickingFrameBuffer[0] - pickingIndexOffset;
			if (lastPicked != picked)
			{
				if (picked >= 0)
				{
					handleDrawableEvtResult(mainDrawable.MouseEnter(picked, this));
				}
				else
				{
					handleDrawableEvtResult(mainDrawable.MouseLeaveEntirely(this));
				}
				if (lastPicked >= 0)
				{
					handleDrawableEvtResult(mainDrawable.MouseLeave(lastPicked, this));
				}
				lastPicked = picked;
			}
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

		public virtual void DrawPicking() { }

		protected void DrawOrientationCube()
		{
			GL.UseProgram(0);
			GL.BindTexture(TextureTarget.Texture2D, Framework.TextureSheet);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Disable(EnableCap.DepthTest);
			GL.Begin(PrimitiveType.Quads);
			GL.Color3(new Vector3(pickingFrameBuffer[0] == 1 ? 1f : 0.75f));
			GL.TexCoord2(0f, 0.5f);
			GL.Vertex3(-1f, 1f, -1f);
			GL.TexCoord2(0.25f, 0.5f);
			GL.Vertex3(1f, 1f, -1f);
			GL.TexCoord2(0.25f, 1f);
			GL.Vertex3(1f, 1f, 1f);
			GL.TexCoord2(0f, 1f);
			GL.Vertex3(-1f, 1f, 1f);
			GL.Color3(new Vector3(pickingFrameBuffer[0] == 2 ? 1f : 0.75f));
			GL.TexCoord2(0.25f, 0.5f);
			GL.Vertex3(-1f, -1f, 1f);
			GL.TexCoord2(0.5f, 0.5f);
			GL.Vertex3(1f, -1f, 1f);
			GL.TexCoord2(0.5f, 1f);
			GL.Vertex3(1f, -1f, -1f);
			GL.TexCoord2(0.25f, 1f);
			GL.Vertex3(-1f, -1f, -1f);
			GL.Color3(new Vector3(pickingFrameBuffer[0] == 3 ? 1f : 0.75f));
			GL.TexCoord2(0f, 0.0f);
			GL.Vertex3(-1f, 1f, 1f);
			GL.TexCoord2(0.25f, 0.0f);
			GL.Vertex3(1f, 1f, 1f);
			GL.TexCoord2(0.25f, 0.5f);
			GL.Vertex3(1f, -1f, 1f);
			GL.TexCoord2(0f, 0.5f);
			GL.Vertex3(-1f, -1f, 1f);
			GL.Color3(new Vector3(pickingFrameBuffer[0] == 4 ? 1f : 0.75f));
			GL.TexCoord2(0.5f, 0f);
			GL.Vertex3(1f, 1f, -1f);
			GL.TexCoord2(0.75f, 0f);
			GL.Vertex3(-1f, 1f, -1f);
			GL.TexCoord2(0.75f, 0.5f);
			GL.Vertex3(-1f, -1f, -1f);
			GL.TexCoord2(0.5f, 0.5f);
			GL.Vertex3(1f, -1f, -1f);
			GL.Color3(new Vector3(pickingFrameBuffer[0] == 5 ? 1f : 0.75f));
			GL.TexCoord2(0.25f, 0f);
			GL.Vertex3(1f, 1f, 1f);
			GL.TexCoord2(0.5f, 0f);
			GL.Vertex3(1f, 1f, -1f);
			GL.TexCoord2(0.5f, 0.5f);
			GL.Vertex3(1f, -1f, -1f);
			GL.TexCoord2(0.25f, 0.5f);
			GL.Vertex3(1f, -1f, 1f);
			GL.Color3(new Vector3(pickingFrameBuffer[0] == 6 ? 1f : 0.75f));
			GL.TexCoord2(0.75f, 0f);
			GL.Vertex3(-1f, 1f, -1f);
			GL.TexCoord2(1f, 0f);
			GL.Vertex3(-1f, 1f, 1f);
			GL.TexCoord2(1f, 0.5f);
			GL.Vertex3(-1f, -1f, 1f);
			GL.TexCoord2(0.75f, 0.5f);
			GL.Vertex3(-1f, -1f, -1f);
			GL.End();
			GL.Enable(EnableCap.DepthTest);
		}

		public override void Refresh()
		{
			if(redrawerOwners==0) //Redrawer is deactivated?
				base.Refresh();   //event can force a redraw
		}

		public void AttachRedrawer()
		{
			if (redrawerOwners == 0)
				redrawer.Start();
			redrawerOwners++;
		}

		public void AttachPickingRedrawer()
		{
			if (redrawerOwners == 0)
				redrawer.Start();
			repickerOwners++;
			redrawerOwners++;
		}

		public void DetachRedrawer()
		{
			redrawerOwners--;
			if (redrawerOwners == 0)
			{
				RedrawerFrame = 0;
				redrawer.Stop();
			}
		}

		public void DetachPickingRedrawer()
		{
			redrawerOwners--;
			repickerOwners--;
			if (redrawerOwners == 0)
			{
				RedrawerFrame = 0;
				redrawer.Stop();
			}
		}
	}
}
