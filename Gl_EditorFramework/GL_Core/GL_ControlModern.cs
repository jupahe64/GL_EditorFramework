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
using OpenTK.Graphics;

namespace GL_EditorFramework.GL_Core
{
    public class GL_ControlModern : GL_ControlBase
    {
        public GL_ControlModern(int redrawerInterval) : base(3, redrawerInterval)
        {

        }

        public GL_ControlModern() : base(3, 16)
        {

        }

        protected override void OnLoad(EventArgs e)
        {
            if (DesignMode) return;
            MakeCurrent();
            Framework.Initialize();

            foreach (var program in Framework.shaderPrograms)
            {
                program.Initialize(this);
            }

            foreach (var vao in Framework.vaos)
            {
                vao.Initialize(this);
            }

            Framework.modernGlControls.Add(this);

            base.OnLoad(e);
        }

        private ShaderProgram shader;
        public ShaderProgram CurrentShader
        {
            get => shader;
            set
            {
                if (value == shader || DesignMode) return;
                shader = value;

                if (shader == null)
                    GL.UseProgram(0);
                else
                    shader.Setup(mtxMdl, mtxCam, mtxProj, this);
            }
        }

        public override AbstractGlDrawable MainDrawable
        {
            get => mainDrawable;
            set
            {
                if (DesignMode) return;

                mainDrawable?.Disconnect(this);

                mainDrawable = value;
                mainDrawable?.Connect(this);
                Refresh();
            }
        }

        public override void UpdateModelMatrix(Matrix4 matrix)
        {
            if (DesignMode) return;
            mtxMdl = matrix;
            if (shader!=null)
                shader.UpdateModelMatrix(matrix, this);
        }

        public override void ApplyModelTransform(Matrix4 matrix)
        {
            if (DesignMode) return;
            if (shader != null)
                shader.UpdateModelMatrix(mtxMdl *= matrix, this);
        }

        public override void ResetModelMatrix()
        {
            if (DesignMode) return;
            if (shader != null)
                shader.UpdateModelMatrix(mtxMdl = Matrix4.Identity, this);
        }

        public void ReloadShaders()
        {

        }

        protected override void OnResize(EventArgs e)
        {
            if (DesignMode)
            {
                base.OnResize(e);
                return;
            }

            float aspect_ratio;
            if (crossEye)
                aspect_ratio = Width / 2 / (float)Height;
            else
                aspect_ratio = Width / (float)Height;

            mtxProj = Matrix4.CreatePerspectiveFieldOfView(fov, aspect_ratio, znear, zfar);

            orientationCubeMtx = Matrix4.CreateOrthographic(Width, Height, 0.125f, 2f) * Matrix4.CreateTranslation(0, 0, 1);

            //using the calculation from whitehole
            FactorX = (2f * (float)Math.Tan(fov * 0.5f) * aspect_ratio) / Width;

            FactorY = (2f * (float)Math.Tan(fov * 0.5f)) / Height;

            base.OnResize(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {

            if (mainDrawable == null || DesignMode)
            {
                base.OnPaint(e);
                e.Graphics.Clear(BackgroundColor1);
                if(DesignMode)
                    e.Graphics.DrawString("Modern Gl" + (crossEye ? " stereoscopy" : ""), SystemFonts.DefaultFont, SystemBrushes.ControlLight, 10f, 10f);
                return;
            }
            MakeCurrent();

            base.OnPaint(e);
            
            GL.ClearColor(BackgroundColor1);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();

            GL.MatrixMode(MatrixMode.Modelview);

            Matrix4 mtxOrientation = GetAnimOrientationMatrix();

            Vector3 camTarget = GetAnimCameraTarget();

            float camDistance = GetAnimCameraDistance();

            if (crossEye)
            {
                #region left eye
                GL.Viewport((stereoscopyType == StereoscopyType.CROSS_EYE) ? Width / 2 : 0, 0, Width / 2, Height);

                mtxMdl = Matrix4.Identity;
                mtxCam =
                    Matrix4.CreateTranslation(-camTarget) *
                    mtxOrientation *
                    Matrix4.CreateTranslation(0.25f, 0, -camDistance) *
                    Matrix4.CreateRotationY(0.02f) *
                    Matrix4.CreateScale(0.03125f);

                RNG = new Random(0);
                mainDrawable.Draw(this, Pass.OPAQUE);

                RNG = new Random(0);
                mainDrawable.Draw(this, Pass.TRANSPARENT);

                shader = null;
                GL.UseProgram(0);

                if (GradientBackground)
                    DrawGradientBG();

                if (showOrientationCube)
                {
                    orientationCubeMtx =
                    Matrix4.CreateRotationY(camRotX) *
                    Matrix4.CreateRotationX(camRotY) *
                    Matrix4.CreateScale(80f / Width, 40f / Height, 0.25f) *
                    Matrix4.CreateTranslation(1 - 160f / Width, 1 - 80f / Height, 0) *
                    Matrix4.CreateRotationY(0.03125f);
                    GL.LoadMatrix(ref orientationCubeMtx);
                    
                    DrawOrientationCube();
                }

                if (showFakeCursor)
                    DrawFakeCursor();
                #endregion

                #region right eye
                GL.Viewport((stereoscopyType == StereoscopyType.CROSS_EYE) ? 0 : Width / 2, 0, Width / 2, Height);

                mtxMdl = Matrix4.Identity;
                mtxCam =
                    Matrix4.CreateTranslation(-camTarget) *
                    mtxOrientation *
                    Matrix4.CreateTranslation(-0.25f, 0, -camDistance) *
                    Matrix4.CreateRotationY(-0.02f) *
                    Matrix4.CreateScale(0.03125f);

                RNG = new Random(0);
                mainDrawable.Draw(this, Pass.OPAQUE);

                RNG = new Random(0);
                mainDrawable.Draw(this, Pass.TRANSPARENT);

                shader = null;
                GL.UseProgram(0);

                if (GradientBackground)
                    DrawGradientBG();

                if (showOrientationCube)
                {
                    orientationCubeMtx =
                    mtxOrientation *
                    Matrix4.CreateScale(80f / Width, 40f / Height, 0.25f) *
                    Matrix4.CreateTranslation(1 - 160f / Width, 1 - 80f / Height, 0) *
                    Matrix4.CreateRotationY(-0.03125f);
                    GL.LoadMatrix(ref orientationCubeMtx);
                    
                    DrawOrientationCube();
                }

                if (showFakeCursor)
                    DrawFakeCursor();

                #endregion
            }
            else
            {
                GL.Viewport(0, 0, Width, Height);

                mtxMdl = Matrix4.Identity;
                mtxCam =
                    Matrix4.CreateTranslation(-camTarget) *
                    mtxOrientation *
                    Matrix4.CreateTranslation(0, 0, -camDistance) *
                    Matrix4.CreateScale(0.03125f);

                RNG = new Random(0);
                mainDrawable.Draw(this, Pass.OPAQUE);

                RNG = new Random(0);
                mainDrawable.Draw(this, Pass.TRANSPARENT);

                shader = null;
                GL.UseProgram(0);

                if (GradientBackground)
                    DrawGradientBG();

                if (showOrientationCube)
                {
                    orientationCubeMtx =
                    mtxOrientation *
                    Matrix4.CreateScale(40f / Width, 40f / Height, 0.25f) *
                    Matrix4.CreateTranslation(1 - 80f / Width, 1 - 80f / Height, 0);
                    GL.LoadMatrix(ref orientationCubeMtx);
                    
                    DrawOrientationCube();
                }
            }
            
            SwapBuffers();
        }

        public override void DrawPicking()
        {
            if (DesignMode || MainDrawable==null) return;
            MakeCurrent();
            GL.ClearColor(0f, 0f, 0f, 0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (crossEye)
                GL.Viewport(0, 0, Width / 2, Height);
            else
                GL.Viewport(0, 0, Width, Height);

            if (showOrientationCube)
                SkipPickingColors(orientationCubePickingColors); //the orientation cube faces

            mtxCam =
                Matrix4.CreateTranslation(-GetAnimCameraTarget()) *
                GetAnimOrientationMatrix() *
                Matrix4.CreateTranslation(0, 0, -GetAnimCameraDistance()) *
                Matrix4.CreateScale(0.03125f);

            mainDrawable.Draw(this, Pass.PICKING);

            shader = null;
            GL.UseProgram(0);

            if (showOrientationCube)
            {
                GL.UseProgram(0);
                DrawOrientationCubePicking();
            }
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var program in Framework.shaderPrograms)
            {
                program.Delete(this);
            }

            foreach (var vao in Framework.vaos)
            {
                vao.Delete(this);
            }

            Framework.modernGlControls.Remove(this);

            base.Dispose(disposing);
        }
    }
}
