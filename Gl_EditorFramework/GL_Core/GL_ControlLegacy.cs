using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using GL_EditorFramework.Interfaces;
using GL_EditorFramework.StandardCameras;

namespace GL_EditorFramework.GL_Core
{
    public class GL_ControlLegacy : GL_ControlBase
    {
        public GL_ControlLegacy(int redrawerInterval) : base(1, redrawerInterval)
        {

        }

        public GL_ControlLegacy() : base(1, 16)
        {
            
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (DesignMode) return;
            MakeCurrent();
            Framework.Initialize();
        }

        public override AbstractGlDrawable MainDrawable
        {
            get => mainDrawable;
            set
            {
                if (value == null || DesignMode) return;

                if (mainDrawable != null)
                    mainDrawable.Disconnect(this);

                mainDrawable = value;
                mainDrawable.Connect(this);
                Refresh();
            }
        }

        public override void UpdateModelMatrix(Matrix4 matrix)
        {
            if (DesignMode) return;
            mtxMdl = matrix;
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref mtxMdl);
        }

        public override void ApplyModelTransform(Matrix4 matrix)
        {
            if (DesignMode) return;
            mtxMdl *= matrix;
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref mtxMdl);
        }

        public override void ResetModelMatrix()
        {
            if (DesignMode) return;
            mtxMdl = Matrix4.Identity;
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref mtxMdl);
        }

        protected override void OnPaint(PaintEventArgs e)
        {

            if (mainDrawable == null || DesignMode)
            {
                e.Graphics.Clear(BackgroundColor1);
                if(DesignMode)
                    e.Graphics.DrawString("Legacy Gl" + (crossEye ? " stereoscopy" : ""), SystemFonts.DefaultFont, SystemBrushes.ControlLight, 10f, 10f);
                return;
            }

            MakeCurrent();

            Matrix4 mtxOrientation = GetAnimOrientationMatrix();

            Vector3 camTarget = GetAnimCameraTarget();

            float camDistance = GetAnimCameraDistance();

            GL.ClearColor(BackgroundColor1);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (crossEye)
            {
                #region left eye
                GL.Viewport((stereoscopyType==StereoscopyType.CROSS_EYE) ? Width / 2 : 0, 0, Width / 2, Height);

                ResetModelMatrix();
                mtxCam =
                    Matrix4.CreateTranslation(-camTarget) *
                    mtxOrientation *
                    Matrix4.CreateTranslation(0.25f, 0, -camDistance) *
                    Matrix4.CreateRotationY(0.02f) *
                    Matrix4.CreateScale(0.03125f);

                GL.MatrixMode(MatrixMode.Projection);
                Matrix4 computedMatrix = mtxCam * mtxProj;
                GL.LoadMatrix(ref computedMatrix);

                RNG = new Random(0);
                mainDrawable.Draw(this, Pass.OPAQUE);

                RNG = new Random(0);
                mainDrawable.Draw(this, Pass.TRANSPARENT);

                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();

                if (GradientBackground)
                    DrawGradientBG();

                if (showOrientationCube)
                {
                    GL.MatrixMode(MatrixMode.Modelview);
                    orientationCubeMtx =
                        Matrix4.CreateRotationY(camRotX) *
                        Matrix4.CreateRotationX(camRotY) *
                        Matrix4.CreateScale(80f / Width, 40f / Height, 0.25f) *
                        Matrix4.CreateTranslation(1 - 160f / Width, 1 - 80f / Height, 0) *
                        Matrix4.CreateRotationY(0.03125f);
                    GL.LoadMatrix(ref orientationCubeMtx);

                    GL.Enable(EnableCap.Texture2D);
                    DrawOrientationCube();
                    GL.Disable(EnableCap.Texture2D);
                }

                if (showFakeCursor)
                    DrawFakeCursor();

                #endregion

                #region right eye
                GL.Viewport((stereoscopyType == StereoscopyType.CROSS_EYE) ? 0 : Width / 2, 0, Width / 2, Height);

                ResetModelMatrix();
                mtxCam =
                    Matrix4.CreateTranslation(-camTarget) *
                    mtxOrientation *
                    Matrix4.CreateTranslation(-0.25f, 0, -camDistance) *
                    Matrix4.CreateRotationY(-0.02f) *
                    Matrix4.CreateScale(0.03125f);

                GL.MatrixMode(MatrixMode.Projection);
                computedMatrix = mtxCam * mtxProj;
                GL.LoadMatrix(ref computedMatrix);

                RNG = new Random(0);
                mainDrawable.Draw(this, Pass.OPAQUE);

                RNG = new Random(0);
                mainDrawable.Draw(this, Pass.TRANSPARENT);

                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();

                if (GradientBackground)
                    DrawGradientBG();

                if (showOrientationCube)
                {
                    GL.MatrixMode(MatrixMode.Modelview);
                    orientationCubeMtx =
                        Matrix4.CreateRotationY(camRotX) *
                        Matrix4.CreateRotationX(camRotY) *
                        Matrix4.CreateScale(80f / Width, 40f / Height, 0.25f) *
                        Matrix4.CreateTranslation(1 - 160f / Width, 1 - 80f / Height, 0) *
                        Matrix4.CreateRotationY(-0.03125f);
                    GL.LoadMatrix(ref orientationCubeMtx);

                    GL.Enable(EnableCap.Texture2D);
                    DrawOrientationCube();
                    GL.Disable(EnableCap.Texture2D);
                }

                if (showFakeCursor)
                    DrawFakeCursor();

                #endregion
            }
            else
            {
                GL.Viewport(0, 0, Width, Height);

                ResetModelMatrix();
                mtxCam =
                    Matrix4.CreateTranslation(-camTarget) *
                    mtxOrientation *
                    Matrix4.CreateTranslation(0, 0, -CameraDistance) *
                    Matrix4.CreateScale(0.03125f);

                GL.MatrixMode(MatrixMode.Projection);
                Matrix4 computedMatrix = mtxCam * mtxProj;
                GL.LoadMatrix(ref computedMatrix);

                RNG = new Random(0);
                mainDrawable.Draw(this, Pass.OPAQUE);

                RNG = new Random(0);
                mainDrawable.Draw(this, Pass.TRANSPARENT);

                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();

                if (GradientBackground)
                    DrawGradientBG();

                if (showOrientationCube)
                {
                    GL.MatrixMode(MatrixMode.Modelview);
                    orientationCubeMtx =
                        mtxOrientation *
                        Matrix4.CreateScale(40f / Width, 40f / Height, 0.25f) *
                        Matrix4.CreateTranslation(1 - 80f / Width, 1 - 80f / Height, 0);
                    GL.LoadMatrix(ref orientationCubeMtx);

                    GL.Enable(EnableCap.Texture2D);
                    DrawOrientationCube();
                    GL.Disable(EnableCap.Texture2D);
                }
            }

            SwapBuffers();

        }

        public override void DrawPicking()
        {
            if (DesignMode) return;
            MakeCurrent();
            GL.ClearColor(0f, 0f, 0f, 0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (crossEye)
                GL.Viewport(0, 0, Width / 2, Height);
            else
                GL.Viewport(0, 0, Width, Height);

            ResetModelMatrix();
            mtxCam =
                Matrix4.CreateTranslation(-CameraTarget) *
                GetAnimOrientationMatrix() *
                Matrix4.CreateTranslation(0, 0, -GetAnimCameraDistance()) *
                Matrix4.CreateScale(0.03125f);

            GL.MatrixMode(MatrixMode.Projection);
            Matrix4 computedMatrix = mtxCam * mtxProj;
            GL.LoadMatrix(ref computedMatrix);

            if (showOrientationCube)
                SkipPickingColors(orientationCubePickingColors); //the orientation cube faces

            mainDrawable.Draw(this, Pass.PICKING);

            if (showOrientationCube)
            {
                DrawOrientationCubePicking();
            }
        }
    }
}
