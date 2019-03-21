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
	public partial class GL_ControlBase : GLControl
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
				_Repick();
			RedrawerFrame++;
		}

		public GL_ControlBase() : base(OpenTK.Graphics.GraphicsMode.Default, 1, 1, OpenTK.Graphics.GraphicsContextFlags.Default)
		{
			
		}



		protected Matrix4 orientationCubeMtx;

		protected bool showFakeCursor;

		private Timer redrawer = new Timer();

		private uint redrawerOwners = 0;

		private uint repickerOwners = 0;

		protected Point lastMouseLoc;
		protected Point dragStartPos = new Point(-1, -1);
		protected float camRotX = 0;
		protected float camRotY = 0;
		protected float camDistance = -10f;
		protected Vector3 camTarget;

		protected Matrix3 mtxRotInv;

		protected float zfar = 1000f;
		protected float znear = 0.01f;
		protected float fov = MathHelper.PiOver4;

		public float PickedObjectPart => pickingFrameBuffer;

		protected uint pickingFrameBuffer;
		protected int pickingIndex;
		private int lastPicked = -1;
		protected float normPickingDepth = 0f;
		protected float pickingDepth = 0f;

		protected Matrix4 mtxMdl, mtxCam, mtxProj;

		public Matrix4 ModelMatrix => mtxMdl;
		public Matrix4 CameraMatrix => mtxCam;
		public Matrix4 ProjectionMatrix => mtxProj;
		public Matrix3 InvertedRotationMatrix => mtxRotInv;

		protected float factorX, factorY;

		protected bool stereoscopy;
		protected bool showOrientationCube = true;

		protected int viewPortX(int x) => stereoscopy ? x % (Width / 2) : x;
		protected int viewPortDX(int dx) => stereoscopy ? dx * 2 : dx;
		protected int viewPortXOff(int x) => stereoscopy ? (x - Width / 4) * 2 : x - Width / 2;

		public Color BackgroundColor1 = Color.FromArgb(20, 20, 20);

		public Color BackgroundColor2 = Color.FromArgb(70, 70, 70);

		public bool GradientBackground;

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

		public bool ShowOrientationCube
		{
			get => showOrientationCube;
			set
			{
				showOrientationCube = value;
				pickingIndexOffset = value?7:1;
				Refresh();
			}
		}

		public Vector3 coordFor(int x, int y, float depth)
		{
			Vector3 vec;
			
			Vector2 normCoords = NormMouseCoords(x, y);

			vec.X = (-normCoords.X * depth) * factorX;
			vec.Y = ( normCoords.Y * depth) * factorY;

			vec.Z = depth + camDistance;

			return camTarget + Vector3.Transform(mtxRotInv, vec);
		}

		public static Vector3 IntersectPoint(Vector3 rayVector, Vector3 rayPoint, Vector3 planeNormal, Vector3 planePoint)
		{
			//code from: https://rosettacode.org/wiki/Find_the_intersection_of_a_line_with_a_plane
			var diff = rayPoint - planePoint;
			var prod1 = Vector3.Dot(diff, planeNormal);
			var prod2 = Vector3.Dot(rayVector, planeNormal);
			var prod3 = prod1 / prod2;
			return rayPoint - rayVector * prod3;
		}

		public Vector3 screenCoordPlaneIntersection(Point point, Vector3 planeNormal, Vector3 planeOrigin)
		{
			Vector3 ray;

			Vector2 normCoords = NormMouseCoords(point.X, point.Y);

			ray.X = (-normCoords.X * zfar) * factorX;
			ray.Y = ( normCoords.Y * zfar) * factorY;
			ray.Z = zfar;
			return IntersectPoint(
				Vector3.Transform(mtxRotInv,ray), 
				camTarget + Vector3.Transform(mtxRotInv, new Vector3(0,0,camDistance)), 
				planeNormal, planeOrigin);
		}

		public Point screenCoordFor(Vector3 coord)
		{
			Vector3 vec = Vector3.Project(coord, 0, 0, Width, Height, -1, 1,  mtxCam * mtxProj);

			return new Point((int)vec.X, Height-(int)(vec.Y));
		}

		protected AbstractGlDrawable mainDrawable;
		public virtual AbstractGlDrawable MainDrawable { get; set; }

		protected AbstractCamera activeCamera;
		private bool shouldRedraw;
		private bool shouldRepick;
		private bool skipCameraAction;
		private int pickingIndexOffset = 7;

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
		public float CamRotX { get => camRotX; set { camRotX = ((value % Framework.TWO_PI) + Framework.TWO_PI) % Framework.TWO_PI; ; } }

		public bool RotXIsReversed { get; private set; }

		public void RotateCameraX(float amount)
		{
			if (RotXIsReversed)
				camRotX -= amount;
			else
				camRotX += amount;

			camRotX = ((camRotX % Framework.TWO_PI) + Framework.TWO_PI) % Framework.TWO_PI;
		}
		public float CamRotY { get => camRotY; set { camRotY = ((value % Framework.TWO_PI) + Framework.TWO_PI) % Framework.TWO_PI; } }

		public float PickingDepth => pickingDepth;

		public float NormPickingDepth => normPickingDepth;

		public ulong RedrawerFrame { get; private set; } = 0;

		public Vector4 nextPickingColor()
		{
			return new Vector4(
				((pickingIndex >> 16) & 0xFF)/256f,
				((pickingIndex >>  8) & 0xFF)/256f,
				(pickingIndex & 0xFF) / 256f,
				((pickingIndex++ >> 24) & 0xFF) / 256f
				);
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
			GL.CullFace(CullFaceMode.Back);
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

		

		public void Repick()
		{
			if (redrawerOwners == 0) //Redrawer is deactivated?
				_Repick();
		}

		protected void _Repick()
		{
			int pickingMouseX = stereoscopy ? lastMouseLoc.X / 2 : lastMouseLoc.X;

			pickingIndex = 1;

			DrawPicking();

			GL.Flush();

			GL.ReadPixels(pickingMouseX, Height - lastMouseLoc.Y, 1, 1, PixelFormat.Bgra, PixelType.UnsignedByte, ref pickingFrameBuffer);

			// depth math from http://www.opengl.org/resources/faq/technical/depthbuffer.htm

			GL.ReadPixels(pickingMouseX, Height - lastMouseLoc.Y, 1, 1, PixelFormat.DepthComponent, PixelType.Float, ref normPickingDepth);

			pickingDepth = -(zfar * znear / (normPickingDepth * (zfar - znear) - zfar));

			int picked = (int)pickingFrameBuffer - pickingIndexOffset;
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
				Console.WriteLine(picked);
			}
		}

		public virtual void DrawPicking() { }

		protected void DrawFakeCursor()
		{
			GL.Color3(1f, 1f, 1f);
			GL.Disable(EnableCap.Texture2D);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();
			GL.Translate(lastMouseLoc.X * 2 / (float)Width - 1, -(lastMouseLoc.Y * 2 / (float)Height - 1), 0);
			GL.Scale(80f / Width, 40f / Height, 1);
			GL.Begin(PrimitiveType.Polygon);
			GL.Vertex2(0, 0);
			GL.Vertex2(0, -1);
			GL.Vertex2(0.25, -0.75);
			GL.Vertex2(0.625, -0.75);
			GL.End();
			GL.Enable(EnableCap.Texture2D);
		}

		protected void DrawOrientationCube()
		{
			GL.BindTexture(TextureTarget.Texture2D, Framework.TextureSheet);
			GL.Disable(EnableCap.DepthTest);

			GL.Begin(PrimitiveType.Quads);
			GL.Color3(new Vector3(pickingFrameBuffer == 1 ? 1f : 0.75f)); //UP
			GL.TexCoord2(0f, 1f);
			GL.Vertex3(-1f, 1f, 1f);
			GL.TexCoord2(0.25f, 1f);
			GL.Vertex3(1f, 1f, 1f);
			GL.TexCoord2(0.25f, 0.5f);
			GL.Vertex3(1f, 1f, -1f);
			GL.TexCoord2(0f, 0.5f);
			GL.Vertex3(-1f, 1f, -1f);
			GL.Color3(new Vector3(pickingFrameBuffer == 2 ? 1f : 0.75f)); //DOWN
			GL.TexCoord2(0.25f, 1f);
			GL.Vertex3(-1f, -1f, -1f);
			GL.TexCoord2(0.5f, 1f);
			GL.Vertex3(1f, -1f, -1f);
			GL.TexCoord2(0.5f, 0.5f);
			GL.Vertex3(1f, -1f, 1f);
			GL.TexCoord2(0.25f, 0.5f);
			GL.Vertex3(-1f, -1f, 1f);
			GL.Color3(new Vector3(pickingFrameBuffer == 3 ? 1f : 0.75f)); //FRONT
			GL.TexCoord2(0.25f, 0f);
			GL.Vertex3(1f, 1f, 1f);
			GL.TexCoord2(0f, 0f);
			GL.Vertex3(-1f, 1f, 1f);
			GL.TexCoord2(0f, 0.5f);
			GL.Vertex3(-1f, -1f, 1f);
			GL.TexCoord2(0.25f, 0.5f);
			GL.Vertex3(1f, -1f, 1f);
			GL.Color3(new Vector3(pickingFrameBuffer == 4 ? 1f : 0.75f)); //BACK	
			GL.TexCoord2(0.75f, 0.0f);
			GL.Vertex3(-1f, 1f, -1f);
			GL.TexCoord2(0.5f, 0.0f);
			GL.Vertex3(1f, 1f, -1f);
			GL.TexCoord2(0.5f, 0.5f);
			GL.Vertex3(1f, -1f, -1f);
			GL.TexCoord2(0.75f, 0.5f);
			GL.Vertex3(-1f, -1f, -1f);
			GL.Color3(new Vector3(pickingFrameBuffer == 5 ? 1f : 0.75f)); //LEFT
			GL.TexCoord2(0.5f, 0f);
			GL.Vertex3(1f, 1f, -1f);
			GL.TexCoord2(0.25f, 0f);
			GL.Vertex3(1f, 1f, 1f);
			GL.TexCoord2(0.25f, 0.5f);
			GL.Vertex3(1f, -1f, 1f);
			GL.TexCoord2(0.5f, 0.5f);
			GL.Vertex3(1f, -1f, -1f);
			GL.Color3(new Vector3(pickingFrameBuffer == 6 ? 1f : 0.75f)); //RIGHT
			GL.TexCoord2(1f, 0f);
			GL.Vertex3(-1f, 1f, 1f);
			GL.TexCoord2(0.75f, 0f);
			GL.Vertex3(-1f, 1f, -1f);
			GL.TexCoord2(0.75f, 0.5f);
			GL.Vertex3(-1f, -1f, -1f);
			GL.TexCoord2(1f, 0.5f);
			GL.Vertex3(-1f, -1f, 1f);
			GL.End();
			GL.Enable(EnableCap.DepthTest);
		}

		protected void DrawGradientBG()
		{
			GL.Disable(EnableCap.Texture2D);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();
			GL.Begin(PrimitiveType.TriangleStrip);
			GL.Color3(BackgroundColor2);
			GL.Vertex3(1, 1, 0.99998);
			GL.Vertex3(-1, 1,  0.99998);
			GL.Color3(BackgroundColor1);
			GL.Vertex3(1, -1, 0.99998);
			GL.Vertex3(-1, -1, 0.99998);
			GL.End();
			GL.Enable(EnableCap.Texture2D);
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
