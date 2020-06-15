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
using System.ComponentModel;
using OpenTK.Graphics;

namespace GL_EditorFramework.GL_Core
{
    public partial class GL_ControlBase : GLControl
    {
        public GL_ControlBase(int maxGL_Version, int redrawerInterval) : base(
            new GraphicsMode(
                32, //color bits
                24, // Depth bits
                1 //stencil bits
            )
            , maxGL_Version, 1, GraphicsContextFlags.Default)
        {
            redrawer.Interval = redrawerInterval;
            redrawer.Tick += Redrawer_Tick;
            marginScrollTimer.Tick += MarginScrollTimer_Tick;
            marginScrollTimer.Interval = 10;
        }

        protected override void Dispose(bool disposing)
        {
            redrawer.Stop();
            marginScrollTimer.Stop();

            base.Dispose(disposing);
        }

        private void MarginScrollTimer_Tick(object sender, EventArgs e)
        {
            if (MainDrawable == null)
                marginScrollTimer.Stop();
            else
            {
                int x = 0;
                int y = 0;

                if (lastMouseLoc.X < 0)
                    x = lastMouseLoc.X;
                else if (lastMouseLoc.X > Width)
                    x = lastMouseLoc.X - Width;

                if (lastMouseLoc.Y < 0)
                    y = lastMouseLoc.Y;
                else if (lastMouseLoc.Y > Height)
                    y = lastMouseLoc.Y - Height;

                mainDrawable.MarginScroll(new MarginScrollEventArgs(lastMouseLoc, x, y), this);
            }
        }

        bool drawAnim = false;

        ulong drawAnimStopFrame = 0;

        bool pickingAnim = false;

        ulong pickingAnimStopFrame = 0;

        public void RedrawFor(ulong frames, bool repick)
        {
            if (mainDrawable == null)
                return;

            if (!drawAnim)
            {
                drawAnim = true;
                redrawerOwners++;

                if (redrawerOwners == 1)
                    redrawer.Start();
            }

            drawAnimStopFrame = RedrawerFrame + frames;

            if (repick)
            {
                if (!pickingAnim)
                {
                    pickingAnim = true;
                    repickerOwners++;
                }

                pickingAnimStopFrame = RedrawerFrame + frames;
            }


        }

        private void Redrawer_Tick(object sender, EventArgs e)
        {
            if (drawAnim && RedrawerFrame == drawAnimStopFrame)
            {
                drawAnim = false;
                redrawerOwners--;
            }

            if (pickingAnim && RedrawerFrame == pickingAnimStopFrame)
            {
                pickingAnim = false;
                repickerOwners--;
            }

            if (redrawerOwners == 0)
            {
                redrawer.Stop();
                return;
            }

            base.Refresh();
            if (repickerOwners > 0)
                _Repick();
            RedrawerFrame++;
        }

        public GL_ControlBase() : base(new OpenTK.Graphics.GraphicsMode(new OpenTK.Graphics.ColorFormat(), 24), 1, 1, OpenTK.Graphics.GraphicsContextFlags.Default)
        {

        }

        //For framing the camera
        private DefaultCameraFraming DefaultCameraFrame = new DefaultCameraFraming();

        class DefaultCameraFraming
        {
            public float camRotX = 0;
            public float camRotY = 0;

            public float Distance = 10f;

            public Vector3 CameraTarget = new Vector3(0);

            public void Reset(GL_ControlBase control)
            {
                camRotX = control.camRotX;
                camRotY = control.camRotY;
                Distance = control.CameraDistance;
                CameraTarget = control.CameraTarget;
            }
        }

        public void ResetCamera(bool FrameCamera)
        {
            if (FrameCamera)
            {
                CameraTarget = DefaultCameraFrame.CameraTarget;
                camRotX = DefaultCameraFrame.camRotX;
                camRotY = DefaultCameraFrame.camRotY;
                CameraDistance = DefaultCameraFrame.Distance;
            }
            else
            {
                CameraTarget = new Vector3(0);
                camRotX = 0;
                camRotY = 0;
                CameraDistance = 10f;
            }
        }

        public Random RNG;

        protected Matrix4 orientationCubeMtx;

        protected bool showFakeCursor;

        private Timer redrawer = new Timer();

        private Timer marginScrollTimer = new Timer();

        private uint redrawerOwners = 0;

        private uint repickerOwners = 0;

        protected Point lastMouseLoc;
        protected Point dragStartPos = new Point(-1, -1);

        protected Matrix3 mtxRotInv;
        public Vector3 CameraPosition { get; private set; }

        protected float zfar = 1000f;
        protected float znear = 0.01f;
        protected float fov = MathHelper.PiOver4;

        public float PickedObjectPart => pickingFrameBuffer;

        protected uint pickingFrameBuffer;
        protected int pickingIndex;
        private int lastPicked = -1;

        public delegate Vector4 NextPickingColorHijackDel();

        protected Matrix4 mtxMdl, mtxCam, mtxProj;

        public Matrix4 ModelMatrix => mtxMdl;
        public Matrix4 CameraMatrix => mtxCam;
        public Matrix4 ProjectionMatrix => mtxProj;
        public Matrix3 InvertedRotationMatrix => mtxRotInv;

        public enum StereoscopyType
        {
            DISABLED,
            CROSS_EYE,
            CROSS_EYE_SWITCHED
        }

        protected StereoscopyType stereoscopyType;

        protected bool crossEye;
        protected bool showOrientationCube = true;

        protected int ViewPortX(int x) => crossEye ? x % (Width / 2) : x;
        protected int ViewPortDX(int dx) => crossEye ? dx * 2 : dx;
        protected int ViewPortXOff(int x) => crossEye ? (x - Width / 4) * 2 : x - Width / 2;

        public Color BackgroundColor1 = Color.FromArgb(20, 20, 20);

        public Color BackgroundColor2 = Color.FromArgb(70, 70, 70);

        public bool GradientBackground;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Point DragStartPos
        {
            get => dragStartPos;
            set
            {
                dragStartPos = value;
            }
        }

        public StereoscopyType Stereoscopy
        {
            get => stereoscopyType;
            set
            {
                stereoscopyType = value;
                crossEye = value == StereoscopyType.CROSS_EYE || value == StereoscopyType.CROSS_EYE_SWITCHED;
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
                PickingIndexOffset = value ? orientationCubePickingColors + 1 : 1;
                Refresh();
            }
        }

        Matrix3 animOrientationMatrix = Matrix3.Identity;

        protected Matrix4 GetAnimOrientationMatrix()
        {
            float blendFactor = 0.25f;

            Matrix3 desired = Matrix3.CreateRotationY(camRotX) * Matrix3.CreateRotationX(camRotY);

            if (!drawAnim)
                return new Matrix4(desired);

            return new Matrix4(animOrientationMatrix = new Matrix3(
                desired.Row0 * blendFactor + animOrientationMatrix.Row0 * (1 - blendFactor),
                desired.Row1 * blendFactor + animOrientationMatrix.Row1 * (1 - blendFactor),
                desired.Row2 * blendFactor + animOrientationMatrix.Row2 * (1 - blendFactor)
                ));
        }

        Vector3 animCameraTarget = Vector3.Zero;

        protected Vector3 GetAnimCameraTarget()
        {
            float blendFactor = 0.5f;

            Vector3 desired = CameraTarget;

            if (!drawAnim)
                return desired;

            return animCameraTarget = desired * blendFactor + animCameraTarget * (1 - blendFactor);
        }

        float animCameraDistance = 0;

        protected float GetAnimCameraDistance()
        {
            float blendFactor = 0.125f;

            float desired = CameraDistance;

            if (!drawAnim)
                return desired;

            return animCameraDistance = desired * blendFactor + animCameraDistance * (1 - blendFactor);
        }

        public Vector3 CoordFor(int x, int y, float depth)
        {
            Vector3 vec;

            Vector2 normCoords = NormMouseCoords(x, y);

            vec.X = (-normCoords.X * depth) * FactorX;
            vec.Y = (normCoords.Y * depth) * FactorY;

            vec.Z = depth - CameraDistance;

            return -CameraTarget + Vector3.Transform(mtxRotInv, vec);
        }

        public Vector3 GetPointUnderMouse() => CoordFor(lastMouseLoc.X, lastMouseLoc.Y, PickingDepth);

        public Point GetMousePos() => lastMouseLoc;

        public static Vector3 IntersectPoint(Vector3 rayVector, Vector3 rayPoint, Vector3 planeNormal, Vector3 planePoint)
        {
            //code from: https://rosettacode.org/wiki/Find_the_intersection_of_a_line_with_a_plane
            var diff = rayPoint - planePoint;
            var prod1 = Vector3.Dot(diff, planeNormal);
            var prod2 = Vector3.Dot(rayVector, planeNormal);
            var prod3 = prod1 / prod2;
            return rayPoint - rayVector * prod3;
        }

        public Vector3 ScreenCoordPlaneIntersection(Point point, Vector3 planeNormal, Vector3 planeOrigin)
        {
            Vector3 ray;

            Vector2 normCoords = NormMouseCoords(point.X, point.Y);

            ray.X = (-normCoords.X * zfar) * FactorX;
            ray.Y = (normCoords.Y * zfar) * FactorY;
            ray.Z = zfar;
            return IntersectPoint(
                Vector3.Transform(mtxRotInv, ray),
                -CameraTarget - Vector3.Transform(mtxRotInv, new Vector3(0, 0, CameraDistance)),
                planeNormal, planeOrigin);
        }

        public Point ScreenCoordFor(Vector3 coord)
        {
            Vector3 vec = Vector3.Project(coord, 0, 0, Width, Height, -1, 1, mtxCam * mtxProj);

            return new Point((int)vec.X, Height - (int)(vec.Y));
        }

        protected AbstractGlDrawable mainDrawable;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual AbstractGlDrawable MainDrawable { get; set; }

        protected AbstractCamera activeCamera = new WalkaroundCamera();
        public int PickingIndexOffset { get; private set; } = 1 + orientationCubePickingColors;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public AbstractCamera ActiveCamera
        {
            get => activeCamera;
            set
            {
                if (value == null) return;
                activeCamera = value;
                Refresh();
            }
        }

        public int ViewWidth => crossEye ? Width / 2 : Width;

        public int ViewHeighth => Height;

        public Vector2 NormMouseCoords(int x, int y)
        {
            return new Vector2(x - Width / 2, y - Height / 2);
        }

        public float ZFar
        {
            get => zfar * 32; set
            {
                zfar = value * 0.03125f;

                if (DesignMode) return;

                float aspect_ratio;
                if (crossEye)
                    aspect_ratio = Width / 2 / (float)Height;
                else
                    aspect_ratio = Width / (float)Height;

                mtxProj = Matrix4.CreatePerspectiveFieldOfView(fov, aspect_ratio, znear, zfar);
            }
        }

        public float ZNear
        {
            get => znear * 32; set
            {
                znear = value * 0.03125f;

                if (DesignMode) return;

                float aspect_ratio;
                if (crossEye)
                    aspect_ratio = Width / 2 / (float)Height;
                else
                    aspect_ratio = Width / (float)Height;

                mtxProj = Matrix4.CreatePerspectiveFieldOfView(fov, aspect_ratio, znear, zfar);
            }
        }

        public float Fov
        {
            get => fov; set
            {
                fov = value;

                if (DesignMode) return;

                float aspect_ratio;
                if (crossEye)
                    aspect_ratio = Width / 2 / (float)Height;
                else
                    aspect_ratio = Width / (float)Height;

                mtxProj = Matrix4.CreatePerspectiveFieldOfView(fov, aspect_ratio, znear, zfar);

                //using the calculation from whitehole
                FactorX = (2f * (float)Math.Tan(fov * 0.5f) * aspect_ratio) / Width;

                FactorY = (2f * (float)Math.Tan(fov * 0.5f)) / Height;
            }
        }

        public float FactorX { get; protected set; }

        public float FactorY { get; protected set; }

        private Vector3 cameraTarget;
        public Vector3 CameraTarget
        {
            get => cameraTarget;
            set
            {
                cameraTarget = value;
                RedrawFor(60, true);

                CameraPosition = cameraTarget + mtxRotInv.Row2 * cameraDistance;
            }
        }

        private float cameraDistance = 10f;
        public float CameraDistance
        {
            get => cameraDistance;
            set
            {
                cameraDistance = value;
                RedrawFor(60, true);

                CameraPosition = cameraTarget + mtxRotInv.Row2 * cameraDistance;
            }
        }

        protected float camRotX = 0;
        public float CamRotX
        {
            get => camRotX;
            set
            {
                camRotX = ((value % Framework.TWO_PI) + Framework.TWO_PI) % Framework.TWO_PI;
                RedrawFor(60, true);

                CameraPosition = cameraTarget + mtxRotInv.Row2 * cameraDistance;

                mtxRotInv =
                    Matrix3.CreateRotationX(-camRotY) *
                    Matrix3.CreateRotationY(-camRotX);
            }
        }

        public bool RotXIsReversed { get; protected set; }

        public void RotateCameraX(float amount)
        {
            if (RotXIsReversed)
                CamRotX -= amount;
            else
                CamRotX += amount;
        }


        protected float camRotY = 0;
        public float CamRotY
        {
            get => camRotY;
            set
            {
                camRotY = ((value % Framework.TWO_PI) + Framework.TWO_PI) % Framework.TWO_PI;
                RedrawFor(60, true);

                CameraPosition = cameraTarget + mtxRotInv.Row2 * cameraDistance;

                mtxRotInv =
                    Matrix3.CreateRotationX(-camRotY) *
                    Matrix3.CreateRotationY(-camRotX);
            }
        }

        public float PickingDepth { get; protected set; } = 0;

        protected float normPickingDepth = 0;
        public float NormPickingDepth
        {
            get => normPickingDepth; set
            {
                normPickingDepth = value;
            }
        }

        public ulong RedrawerFrame { get; private set; } = 0;

        public NextPickingColorHijackDel NextPickingColorHijack;

        public Vector4 NextPickingColor()
        {
            return NextPickingColorHijack?.Invoke() ??
            new Vector4(
                ((pickingIndex >> 16) & 0xFF) / 255f,
                ((pickingIndex >> 8) & 0xFF) / 255f,
                (pickingIndex & 0xFF) / 255f,
                ((pickingIndex++ >> 24) & 0xFF) / 255f
                );
        }

        public void SkipPickingColors(uint count)
        {
            pickingIndex += (int)count;
        }

        public virtual void UpdateModelMatrix(Matrix4 matrix) { }
        public virtual void ApplyModelTransform(Matrix4 matrix) { }

        public virtual void ResetModelMatrix() { }

        protected override void OnLoad(EventArgs e)
        {

            if (DesignMode) return;


            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.Enable(EnableCap.Texture2D);

            GL.Enable(EnableCap.LineSmooth);
            GL.Enable(EnableCap.PolygonSmooth);

            GL.Enable(EnableCap.AlphaTest);

            GL.LineWidth(2f);

            GL.Hint(HintTarget.MultisampleFilterHintNv, HintMode.Nicest);

            base.OnLoad(e);
        }

        protected override void OnResize(EventArgs e)
        {
            if (DesignMode) return;

            float aspect_ratio;
            if (crossEye)
                aspect_ratio = Width / 2 / (float)Height;
            else
                aspect_ratio = Width / (float)Height;

            mtxProj = Matrix4.CreatePerspectiveFieldOfView(fov, aspect_ratio, znear, zfar);

            //using the calculation from whitehole
            FactorX = (2f * (float)Math.Tan(fov * 0.5f) * aspect_ratio) / Width;

            FactorY = (2f * (float)Math.Tan(fov * 0.5f)) / Height;
            Refresh();
        }

        public void Repick()
        {
            if (repickerOwners == 0)
                _Repick();
        }

        protected void _Repick()
        {
            int pickingMouseX = crossEye ? lastMouseLoc.X / 2 : lastMouseLoc.X;

            pickingIndex = 1;

            DrawPicking();

            GL.Flush();

            GL.ReadPixels(pickingMouseX, Height - lastMouseLoc.Y, 1, 1, PixelFormat.Bgra, PixelType.UnsignedByte, ref pickingFrameBuffer);

            // depth math from http://www.opengl.org/resources/faq/technical/depthbuffer.htm

            GL.ReadPixels(pickingMouseX, Height - lastMouseLoc.Y, 1, 1, PixelFormat.DepthComponent, PixelType.Float, ref normPickingDepth);

            PickingDepth = -(zfar * znear / (NormPickingDepth * (zfar - znear) - zfar)) * 32;

            int picked = (int)pickingFrameBuffer - PickingIndexOffset;
            if (lastPicked != picked || forceReEnter)
            {
                if (lastPicked >= 0)
                {
                    HandleDrawableEvtResult(mainDrawable.MouseLeave(lastPicked, this));
                }
                if (picked >= 0)
                {
                    HandleDrawableEvtResult(mainDrawable.MouseEnter(picked, this));
                }
                else
                {
                    HandleDrawableEvtResult(mainDrawable.MouseLeaveEntirely(this));
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
            float oc_faceSize = 0.85f;

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, Framework.TextureSheet);
            GL.Disable(EnableCap.DepthTest);

            #region generated code
            GL.Begin(PrimitiveType.Quads);
            GL.Color3(new Vector3(pickingFrameBuffer == 1 ? 1f : 0.75f));
            GL.TexCoord2(0.257452, 0.514903);
            GL.Vertex3(-oc_faceSize, -1, -oc_faceSize);
            GL.TexCoord2(0.492548, 0.514903);
            GL.Vertex3(oc_faceSize, -1, -oc_faceSize);
            GL.TexCoord2(0.492548, 0.985097);
            GL.Vertex3(oc_faceSize, -1, oc_faceSize);
            GL.TexCoord2(0.257452, 0.985097);
            GL.Vertex3(-oc_faceSize, -1, oc_faceSize);
            GL.Color3(new Vector3(pickingFrameBuffer == 2 ? 1f : 0.75f));
            GL.TexCoord2(0.992548, 0.485097);
            GL.Vertex3(-1, -oc_faceSize, oc_faceSize);
            GL.TexCoord2(0.992548, 0.01490301);
            GL.Vertex3(-1, oc_faceSize, oc_faceSize);
            GL.TexCoord2(0.757452, 0.01490301);
            GL.Vertex3(-1, oc_faceSize, -oc_faceSize);
            GL.TexCoord2(0.757452, 0.485097);
            GL.Vertex3(-1, -oc_faceSize, -oc_faceSize);
            GL.Color3(new Vector3(pickingFrameBuffer == 3 ? 1f : 0.75f));
            GL.TexCoord2(0.242548, 0.485097);
            GL.Vertex3(oc_faceSize, -oc_faceSize, 1);
            GL.TexCoord2(0.242548, 0.01490301);
            GL.Vertex3(oc_faceSize, oc_faceSize, 1);
            GL.TexCoord2(0.007452, 0.01490301);
            GL.Vertex3(-oc_faceSize, oc_faceSize, 1);
            GL.TexCoord2(0.007452, 0.485097);
            GL.Vertex3(-oc_faceSize, -oc_faceSize, 1);
            GL.Color3(new Vector3(pickingFrameBuffer == 4 ? 1f : 0.75f));
            GL.TexCoord2(0.242548, 0.514903);
            GL.Vertex3(oc_faceSize, 1, -oc_faceSize);
            GL.TexCoord2(0.007452, 0.514903);
            GL.Vertex3(-oc_faceSize, 1, -oc_faceSize);
            GL.TexCoord2(0.007452, 0.985097);
            GL.Vertex3(-oc_faceSize, 1, oc_faceSize);
            GL.TexCoord2(0.242548, 0.985097);
            GL.Vertex3(oc_faceSize, 1, oc_faceSize);
            GL.Color3(new Vector3(pickingFrameBuffer == 5 ? 1f : 0.75f));
            GL.TexCoord2(0.492548, 0.485097);
            GL.Vertex3(1, -oc_faceSize, -oc_faceSize);
            GL.TexCoord2(0.492548, 0.01490301);
            GL.Vertex3(1, oc_faceSize, -oc_faceSize);
            GL.TexCoord2(0.257452, 0.01490301);
            GL.Vertex3(1, oc_faceSize, oc_faceSize);
            GL.TexCoord2(0.257452, 0.485097);
            GL.Vertex3(1, -oc_faceSize, oc_faceSize);
            GL.Color3(new Vector3(pickingFrameBuffer == 14 ? 1f : 0.75f));
            GL.TexCoord2(0.9375, 0.625);
            GL.Vertex3(-oc_faceSize, -1, -oc_faceSize);
            GL.TexCoord2(0.9375, 0.625);
            GL.Vertex3(-oc_faceSize, -1, oc_faceSize);
            GL.TexCoord2(0.9375, 0.625);
            GL.Vertex3(-1, -oc_faceSize, oc_faceSize);
            GL.TexCoord2(0.9375, 0.625);
            GL.Vertex3(-1, -oc_faceSize, -oc_faceSize);
            GL.Color3(new Vector3(pickingFrameBuffer == 15 ? 1f : 0.75f));
            GL.TexCoord2(0.8125, 0.875);
            GL.Vertex3(oc_faceSize, -1, -oc_faceSize);
            GL.TexCoord2(0.8125, 0.875);
            GL.Vertex3(-oc_faceSize, -1, -oc_faceSize);
            GL.TexCoord2(0.8125, 0.875);
            GL.Vertex3(-oc_faceSize, -oc_faceSize, -1);
            GL.TexCoord2(0.8125, 0.875);
            GL.Vertex3(oc_faceSize, -oc_faceSize, -1);
            GL.Color3(new Vector3(pickingFrameBuffer == 16 ? 1f : 0.75f));
            GL.TexCoord2(0.8125, 0.625);
            GL.Vertex3(1, oc_faceSize, -oc_faceSize);
            GL.TexCoord2(0.8125, 0.625);
            GL.Vertex3(1, -oc_faceSize, -oc_faceSize);
            GL.TexCoord2(0.8125, 0.625);
            GL.Vertex3(oc_faceSize, -oc_faceSize, -1);
            GL.TexCoord2(0.8125, 0.625);
            GL.Vertex3(oc_faceSize, oc_faceSize, -1);
            GL.Color3(new Vector3(pickingFrameBuffer == 17 ? 1f : 0.75f));
            GL.TexCoord2(0.8125, 0.625);
            GL.Vertex3(-oc_faceSize, -oc_faceSize, 1);
            GL.TexCoord2(0.8125, 0.625);
            GL.Vertex3(-oc_faceSize, oc_faceSize, 1);
            GL.TexCoord2(0.8125, 0.625);
            GL.Vertex3(-1, oc_faceSize, oc_faceSize);
            GL.TexCoord2(0.8125, 0.625);
            GL.Vertex3(-1, -oc_faceSize, oc_faceSize);
            GL.Color3(new Vector3(pickingFrameBuffer == 18 ? 1f : 0.75f));
            GL.TexCoord2(0.8125, 0.625);
            GL.Vertex3(oc_faceSize, oc_faceSize, 1);
            GL.TexCoord2(0.8125, 0.625);
            GL.Vertex3(oc_faceSize, -oc_faceSize, 1);
            GL.TexCoord2(0.8125, 0.625);
            GL.Vertex3(1, -oc_faceSize, oc_faceSize);
            GL.TexCoord2(0.8125, 0.625);
            GL.Vertex3(1, oc_faceSize, oc_faceSize);
            GL.Color3(new Vector3(pickingFrameBuffer == 19 ? 1f : 0.75f));
            GL.TexCoord2(0.8125, 0.625);
            GL.Vertex3(-oc_faceSize, oc_faceSize, -1);
            GL.TexCoord2(0.8125, 0.625);
            GL.Vertex3(-oc_faceSize, -oc_faceSize, -1);
            GL.TexCoord2(0.8125, 0.625);
            GL.Vertex3(-1, -oc_faceSize, -oc_faceSize);
            GL.TexCoord2(0.8125, 0.625);
            GL.Vertex3(-1, oc_faceSize, -oc_faceSize);
            GL.Color3(new Vector3(pickingFrameBuffer == 20 ? 1f : 0.75f));
            GL.TexCoord2(0.8125, 0.875);
            GL.Vertex3(oc_faceSize, 1, oc_faceSize);
            GL.TexCoord2(0.8125, 0.875);
            GL.Vertex3(-oc_faceSize, 1, oc_faceSize);
            GL.TexCoord2(0.8125, 0.875);
            GL.Vertex3(-oc_faceSize, oc_faceSize, 1);
            GL.TexCoord2(0.8125, 0.875);
            GL.Vertex3(oc_faceSize, oc_faceSize, 1);
            GL.Color3(new Vector3(pickingFrameBuffer == 21 ? 1f : 0.75f));
            GL.TexCoord2(0.9375, 0.625);
            GL.Vertex3(oc_faceSize, 1, -oc_faceSize);
            GL.TexCoord2(0.9375, 0.625);
            GL.Vertex3(oc_faceSize, 1, oc_faceSize);
            GL.TexCoord2(0.9375, 0.625);
            GL.Vertex3(1, oc_faceSize, oc_faceSize);
            GL.TexCoord2(0.9375, 0.625);
            GL.Vertex3(1, oc_faceSize, -oc_faceSize);
            GL.Color3(new Vector3(pickingFrameBuffer == 22 ? 1f : 0.75f));
            GL.TexCoord2(0.8125, 0.875);
            GL.Vertex3(-oc_faceSize, -1, oc_faceSize);
            GL.TexCoord2(0.8125, 0.875);
            GL.Vertex3(oc_faceSize, -1, oc_faceSize);
            GL.TexCoord2(0.8125, 0.875);
            GL.Vertex3(oc_faceSize, -oc_faceSize, 1);
            GL.TexCoord2(0.8125, 0.875);
            GL.Vertex3(-oc_faceSize, -oc_faceSize, 1);
            GL.Color3(new Vector3(pickingFrameBuffer == 23 ? 1f : 0.75f));
            GL.TexCoord2(0.9375, 0.625);
            GL.Vertex3(-oc_faceSize, 1, oc_faceSize);
            GL.TexCoord2(0.9375, 0.625);
            GL.Vertex3(-oc_faceSize, 1, -oc_faceSize);
            GL.TexCoord2(0.9375, 0.625);
            GL.Vertex3(-1, oc_faceSize, -oc_faceSize);
            GL.TexCoord2(0.9375, 0.625);
            GL.Vertex3(-1, oc_faceSize, oc_faceSize);
            GL.Color3(new Vector3(pickingFrameBuffer == 24 ? 1f : 0.75f));
            GL.TexCoord2(0.8125, 0.875);
            GL.Vertex3(-oc_faceSize, 1, -oc_faceSize);
            GL.TexCoord2(0.8125, 0.875);
            GL.Vertex3(oc_faceSize, 1, -oc_faceSize);
            GL.TexCoord2(0.8125, 0.875);
            GL.Vertex3(oc_faceSize, oc_faceSize, -1);
            GL.TexCoord2(0.8125, 0.875);
            GL.Vertex3(-oc_faceSize, oc_faceSize, -1);
            GL.Color3(new Vector3(pickingFrameBuffer == 25 ? 1f : 0.75f));
            GL.TexCoord2(0.9375, 0.625);
            GL.Vertex3(oc_faceSize, -1, oc_faceSize);
            GL.TexCoord2(0.9375, 0.625);
            GL.Vertex3(oc_faceSize, -1, -oc_faceSize);
            GL.TexCoord2(0.9375, 0.625);
            GL.Vertex3(1, -oc_faceSize, -oc_faceSize);
            GL.TexCoord2(0.9375, 0.625);
            GL.Vertex3(1, -oc_faceSize, oc_faceSize);
            GL.Color3(new Vector3(pickingFrameBuffer == 26 ? 1f : 0.75f));
            GL.TexCoord2(0.742548, 0.485097);
            GL.Vertex3(-oc_faceSize, -oc_faceSize, -1);
            GL.TexCoord2(0.742548, 0.01490301);
            GL.Vertex3(-oc_faceSize, oc_faceSize, -1);
            GL.TexCoord2(0.507452, 0.01490301);
            GL.Vertex3(oc_faceSize, oc_faceSize, -1);
            GL.TexCoord2(0.507452, 0.485097);
            GL.Vertex3(oc_faceSize, -oc_faceSize, -1);
            GL.End();

            GL.Begin(PrimitiveType.Triangles);
            GL.Color3(new Vector3(pickingFrameBuffer == 6 ? 1f : 0.75f));
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(oc_faceSize, oc_faceSize, -1);
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(oc_faceSize, 1, -oc_faceSize);
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(1, oc_faceSize, -oc_faceSize);
            GL.Color3(new Vector3(pickingFrameBuffer == 7 ? 1f : 0.75f));
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(oc_faceSize, -1, -oc_faceSize);
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(oc_faceSize, -oc_faceSize, -1);
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(1, -oc_faceSize, -oc_faceSize);
            GL.Color3(new Vector3(pickingFrameBuffer == 8 ? 1f : 0.75f));
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(1, oc_faceSize, oc_faceSize);
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(oc_faceSize, 1, oc_faceSize);
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(oc_faceSize, oc_faceSize, 1);
            GL.Color3(new Vector3(pickingFrameBuffer == 9 ? 1f : 0.75f));
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(1, -oc_faceSize, oc_faceSize);
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(oc_faceSize, -oc_faceSize, 1);
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(oc_faceSize, -1, oc_faceSize);
            GL.Color3(new Vector3(pickingFrameBuffer == 10 ? 1f : 0.75f));
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(-oc_faceSize, oc_faceSize, -1);
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(-1, oc_faceSize, -oc_faceSize);
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(-oc_faceSize, 1, -oc_faceSize);
            GL.Color3(new Vector3(pickingFrameBuffer == 11 ? 1f : 0.75f));
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(-1, -oc_faceSize, -oc_faceSize);
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(-oc_faceSize, -oc_faceSize, -1);
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(-oc_faceSize, -1, -oc_faceSize);
            GL.Color3(new Vector3(pickingFrameBuffer == 12 ? 1f : 0.75f));
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(-1, oc_faceSize, oc_faceSize);
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(-oc_faceSize, oc_faceSize, 1);
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(-oc_faceSize, 1, oc_faceSize);
            GL.Color3(new Vector3(pickingFrameBuffer == 13 ? 1f : 0.75f));
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(-oc_faceSize, -1, oc_faceSize);
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(-oc_faceSize, -oc_faceSize, 1);
            GL.TexCoord2(0.9375, 0.875);
            GL.Vertex3(-1, -oc_faceSize, oc_faceSize);
            GL.End();
            #endregion

            GL.Enable(EnableCap.DepthTest);
        }

        protected const int orientationCubePickingColors = 6 + 12 + 8;

        protected void DrawOrientationCubePicking()
        {
            float oc_faceSize = 0.7f;

            GL.Disable(EnableCap.Texture2D);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.MatrixMode(MatrixMode.Modelview);
            orientationCubeMtx =
                Matrix4.CreateRotationY(camRotX) *
                Matrix4.CreateRotationX(camRotY) *
                Matrix4.CreateScale((crossEye ? 80f : 40f) / Width, 40f / Height, 0.25f) *
                Matrix4.CreateTranslation(1 - (crossEye ? 160f : 80f) / Width, 1 - 80f / Height, 0);
            GL.LoadMatrix(ref orientationCubeMtx);
            GL.Disable(EnableCap.DepthTest);

            #region generated code
            GL.Begin(PrimitiveType.Quads);
            GL.Color4(Color.FromArgb(1));
            GL.Vertex3(-oc_faceSize, -1, -oc_faceSize);
            GL.Vertex3(oc_faceSize, -1, -oc_faceSize);
            GL.Vertex3(oc_faceSize, -1, oc_faceSize);
            GL.Vertex3(-oc_faceSize, -1, oc_faceSize);
            GL.Color4(Color.FromArgb(2));
            GL.Vertex3(-1, -oc_faceSize, oc_faceSize);
            GL.Vertex3(-1, oc_faceSize, oc_faceSize);
            GL.Vertex3(-1, oc_faceSize, -oc_faceSize);
            GL.Vertex3(-1, -oc_faceSize, -oc_faceSize);
            GL.Color4(Color.FromArgb(3));
            GL.Vertex3(oc_faceSize, -oc_faceSize, 1);
            GL.Vertex3(oc_faceSize, oc_faceSize, 1);
            GL.Vertex3(-oc_faceSize, oc_faceSize, 1);
            GL.Vertex3(-oc_faceSize, -oc_faceSize, 1);
            GL.Color4(Color.FromArgb(4));
            GL.Vertex3(oc_faceSize, 1, -oc_faceSize);
            GL.Vertex3(-oc_faceSize, 1, -oc_faceSize);
            GL.Vertex3(-oc_faceSize, 1, oc_faceSize);
            GL.Vertex3(oc_faceSize, 1, oc_faceSize);
            GL.Color4(Color.FromArgb(5));
            GL.Vertex3(1, -oc_faceSize, -oc_faceSize);
            GL.Vertex3(1, oc_faceSize, -oc_faceSize);
            GL.Vertex3(1, oc_faceSize, oc_faceSize);
            GL.Vertex3(1, -oc_faceSize, oc_faceSize);
            GL.Color4(Color.FromArgb(14));
            GL.Vertex3(-oc_faceSize, -1, -oc_faceSize);
            GL.Vertex3(-oc_faceSize, -1, oc_faceSize);
            GL.Vertex3(-1, -oc_faceSize, oc_faceSize);
            GL.Vertex3(-1, -oc_faceSize, -oc_faceSize);
            GL.Color4(Color.FromArgb(15));
            GL.Vertex3(oc_faceSize, -1, -oc_faceSize);
            GL.Vertex3(-oc_faceSize, -1, -oc_faceSize);
            GL.Vertex3(-oc_faceSize, -oc_faceSize, -1);
            GL.Vertex3(oc_faceSize, -oc_faceSize, -1);
            GL.Color4(Color.FromArgb(16));
            GL.Vertex3(1, oc_faceSize, -oc_faceSize);
            GL.Vertex3(1, -oc_faceSize, -oc_faceSize);
            GL.Vertex3(oc_faceSize, -oc_faceSize, -1);
            GL.Vertex3(oc_faceSize, oc_faceSize, -1);
            GL.Color4(Color.FromArgb(17));
            GL.Vertex3(-oc_faceSize, -oc_faceSize, 1);
            GL.Vertex3(-oc_faceSize, oc_faceSize, 1);
            GL.Vertex3(-1, oc_faceSize, oc_faceSize);
            GL.Vertex3(-1, -oc_faceSize, oc_faceSize);
            GL.Color4(Color.FromArgb(18));
            GL.Vertex3(oc_faceSize, oc_faceSize, 1);
            GL.Vertex3(oc_faceSize, -oc_faceSize, 1);
            GL.Vertex3(1, -oc_faceSize, oc_faceSize);
            GL.Vertex3(1, oc_faceSize, oc_faceSize);
            GL.Color4(Color.FromArgb(19));
            GL.Vertex3(-oc_faceSize, oc_faceSize, -1);
            GL.Vertex3(-oc_faceSize, -oc_faceSize, -1);
            GL.Vertex3(-1, -oc_faceSize, -oc_faceSize);
            GL.Vertex3(-1, oc_faceSize, -oc_faceSize);
            GL.Color4(Color.FromArgb(20));
            GL.Vertex3(oc_faceSize, 1, oc_faceSize);
            GL.Vertex3(-oc_faceSize, 1, oc_faceSize);
            GL.Vertex3(-oc_faceSize, oc_faceSize, 1);
            GL.Vertex3(oc_faceSize, oc_faceSize, 1);
            GL.Color4(Color.FromArgb(21));
            GL.Vertex3(oc_faceSize, 1, -oc_faceSize);
            GL.Vertex3(oc_faceSize, 1, oc_faceSize);
            GL.Vertex3(1, oc_faceSize, oc_faceSize);
            GL.Vertex3(1, oc_faceSize, -oc_faceSize);
            GL.Color4(Color.FromArgb(22));
            GL.Vertex3(-oc_faceSize, -1, oc_faceSize);
            GL.Vertex3(oc_faceSize, -1, oc_faceSize);
            GL.Vertex3(oc_faceSize, -oc_faceSize, 1);
            GL.Vertex3(-oc_faceSize, -oc_faceSize, 1);
            GL.Color4(Color.FromArgb(23));
            GL.Vertex3(-oc_faceSize, 1, oc_faceSize);
            GL.Vertex3(-oc_faceSize, 1, -oc_faceSize);
            GL.Vertex3(-1, oc_faceSize, -oc_faceSize);
            GL.Vertex3(-1, oc_faceSize, oc_faceSize);
            GL.Color4(Color.FromArgb(24));
            GL.Vertex3(-oc_faceSize, 1, -oc_faceSize);
            GL.Vertex3(oc_faceSize, 1, -oc_faceSize);
            GL.Vertex3(oc_faceSize, oc_faceSize, -1);
            GL.Vertex3(-oc_faceSize, oc_faceSize, -1);
            GL.Color4(Color.FromArgb(25));
            GL.Vertex3(oc_faceSize, -1, oc_faceSize);
            GL.Vertex3(oc_faceSize, -1, -oc_faceSize);
            GL.Vertex3(1, -oc_faceSize, -oc_faceSize);
            GL.Vertex3(1, -oc_faceSize, oc_faceSize);
            GL.Color4(Color.FromArgb(26));
            GL.Vertex3(-oc_faceSize, -oc_faceSize, -1);
            GL.Vertex3(-oc_faceSize, oc_faceSize, -1);
            GL.Vertex3(oc_faceSize, oc_faceSize, -1);
            GL.Vertex3(oc_faceSize, -oc_faceSize, -1);
            GL.End();

            GL.Begin(PrimitiveType.Triangles);
            GL.Color4(Color.FromArgb(6));
            GL.Vertex3(oc_faceSize, oc_faceSize, -1);
            GL.Vertex3(oc_faceSize, 1, -oc_faceSize);
            GL.Vertex3(1, oc_faceSize, -oc_faceSize);
            GL.Color4(Color.FromArgb(7));
            GL.Vertex3(oc_faceSize, -1, -oc_faceSize);
            GL.Vertex3(oc_faceSize, -oc_faceSize, -1);
            GL.Vertex3(1, -oc_faceSize, -oc_faceSize);
            GL.Color4(Color.FromArgb(8));
            GL.Vertex3(1, oc_faceSize, oc_faceSize);
            GL.Vertex3(oc_faceSize, 1, oc_faceSize);
            GL.Vertex3(oc_faceSize, oc_faceSize, 1);
            GL.Color4(Color.FromArgb(9));
            GL.Vertex3(1, -oc_faceSize, oc_faceSize);
            GL.Vertex3(oc_faceSize, -oc_faceSize, 1);
            GL.Vertex3(oc_faceSize, -1, oc_faceSize);
            GL.Color4(Color.FromArgb(10));
            GL.Vertex3(-oc_faceSize, oc_faceSize, -1);
            GL.Vertex3(-1, oc_faceSize, -oc_faceSize);
            GL.Vertex3(-oc_faceSize, 1, -oc_faceSize);
            GL.Color4(Color.FromArgb(11));
            GL.Vertex3(-1, -oc_faceSize, -oc_faceSize);
            GL.Vertex3(-oc_faceSize, -oc_faceSize, -1);
            GL.Vertex3(-oc_faceSize, -1, -oc_faceSize);
            GL.Color4(Color.FromArgb(12));
            GL.Vertex3(-1, oc_faceSize, oc_faceSize);
            GL.Vertex3(-oc_faceSize, oc_faceSize, 1);
            GL.Vertex3(-oc_faceSize, 1, oc_faceSize);
            GL.Color4(Color.FromArgb(13));
            GL.Vertex3(-oc_faceSize, -1, oc_faceSize);
            GL.Vertex3(-oc_faceSize, -oc_faceSize, 1);
            GL.Vertex3(-1, -oc_faceSize, oc_faceSize);
            GL.End();
            #endregion

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Texture2D);
        }

        protected void DrawGradientBG()
        {
            GL.Disable(EnableCap.Texture2D);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.Begin(PrimitiveType.TriangleStrip);
            GL.Color3(BackgroundColor2);
            GL.Vertex3(1, 1, 0.99998);
            GL.Vertex3(-1, 1, 0.99998);
            GL.Color3(BackgroundColor1);
            GL.Vertex3(1, -1, 0.99998);
            GL.Vertex3(-1, -1, 0.99998);
            GL.End();
            GL.Enable(EnableCap.Texture2D);
        }

        public override void Refresh()
        {
            if (redrawerOwners == 0) //Redrawer is deactivated?
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
