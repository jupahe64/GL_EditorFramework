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
			base.OnLoad(e);
			if (DesignMode) return;
			MakeCurrent();
			Framework.Initialize();
		}

		private ShaderProgram shader;
		public ShaderProgram CurrentShader
		{
			get => shader;
			set
			{
				if (value == null || DesignMode) return;
				shader = value;

				shader.Setup(mtxMdl, mtxCam, mtxProj);
			}
		}
		
		public override AbstractGlDrawable MainDrawable
		{
			get => mainDrawable;
			set
			{
				if (value == null || DesignMode) return;
				mainDrawable = value;
				MakeCurrent();
				mainDrawable.Prepare(this);
				Refresh();
			}
		}

		Matrix4 orientationCubeMtx;

		public override void UpdateModelMatrix(Matrix4 matrix)
		{
			if (DesignMode) return;
			shader.UpdateModelMatrix(mtxMdl = matrix);
		}

		public override void ApplyModelTransform(Matrix4 matrix)
		{
			if (DesignMode) return;
			shader.UpdateModelMatrix(mtxMdl *= matrix);
		}

		public override void ResetModelMatrix()
		{
			if (DesignMode) return;
			shader.UpdateModelMatrix(mtxMdl = Matrix4.Identity);
		}

		protected override void OnResize(EventArgs e)
		{
			if (DesignMode)
			{
				base.OnResize(e);
				return;
			}
			MakeCurrent();

			float aspect_ratio;
			if (stereoscopy)
				aspect_ratio = Width / 2 / (float)Height;
			else
				aspect_ratio = Width / (float)Height;

			mtxProj = Matrix4.CreatePerspectiveFieldOfView(fov, aspect_ratio, znear, zfar);

			orientationCubeMtx = Matrix4.CreateOrthographic(Width, Height, 0.125f, 2f) * Matrix4.CreateTranslation(0,0,1);

			//using the calculation from whitehole
			factorX = (2f * (float)Math.Tan(fov * 0.5f) * aspect_ratio) / Width;

			factorY = (2f * (float)Math.Tan(fov * 0.5f)) / Height;

			base.OnResize(e);
		}
		
		protected override void OnPaint(PaintEventArgs e)
		{
			
			if (mainDrawable == null || DesignMode)
			{
				base.OnPaint(e);
				e.Graphics.Clear(Color.Black);
				e.Graphics.DrawString("Modern Gl" + (stereoscopy ? " stereoscopy" : ""), SystemFonts.DefaultFont, SystemBrushes.ControlLight, 10f, 10f);
				return;
			}
			MakeCurrent();

			base.OnPaint(e);

			GL.ClearColor(0.125f, 0.125f, 0.125f, 1.0f);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			if (stereoscopy)
			{
				#region left eye
				GL.Viewport(0, 0, Width / 2, Height);

				mtxMdl = Matrix4.Identity;
				mtxCam =
					Matrix4.CreateTranslation(camTarget) *
					Matrix4.CreateRotationY(camRotX) *
					Matrix4.CreateRotationX(camRotY) *
					Matrix4.CreateTranslation(0.25f, 0, camDistance) *
					Matrix4.CreateRotationY(0.02f);

				mainDrawable.Draw(this);

				if (showFakeCursor)
				{
					GL.UseProgram(0);
					GL.Disable(EnableCap.Texture2D);
					GL.LoadIdentity();
					GL.Translate(lastMouseLoc.X * 2 / (float)Width - 1, -(lastMouseLoc.Y * 2 / (float)Height - 1), 0);
					GL.Scale(80f / Width, 40f / Height, 1);
					GL.Begin(PrimitiveType.Polygon);
					GL.Vertex2(0, 0);
					GL.Vertex2(0.75, -0.25);
					GL.Vertex2(0.375, -0.375);
					GL.Vertex2(0.25, -0.875);
					GL.End();
					GL.Enable(EnableCap.Texture2D);
				}
				#endregion

				#region right eye
				GL.Viewport(Width / 2, 0, Width / 2, Height);

				mtxMdl = Matrix4.Identity;
				mtxCam =
					Matrix4.CreateTranslation(camTarget) *
					Matrix4.CreateRotationY(camRotX) *
					Matrix4.CreateRotationX(camRotY) *
					Matrix4.CreateTranslation(-0.25f, 0, camDistance) *
					Matrix4.CreateRotationY(-0.02f);

				mainDrawable.Draw(this);

				if (showFakeCursor)
				{
					GL.UseProgram(0);
					GL.Disable(EnableCap.Texture2D);
					GL.LoadIdentity();
					GL.Translate(lastMouseLoc.X * 2 / (float)Width - 1, -(lastMouseLoc.Y * 2 / (float)Height - 1), 0);
					GL.Scale(40f / ViewWidth, 40f / Height, 1);
					GL.Begin(PrimitiveType.Polygon);
					GL.Vertex2(0, 0);
					GL.Vertex2(0.75, -0.25);
					GL.Vertex2(0.375, -0.375);
					GL.Vertex2(0.25, -0.875);
					GL.End();
					GL.Enable(EnableCap.Texture2D);
				}
				#endregion


			}
			else
			{
				GL.Viewport(0, 0, Width, Height);
				
				mtxMdl = Matrix4.Identity;
				mtxCam =
					Matrix4.CreateTranslation(camTarget) *
					Matrix4.CreateRotationY(camRotX) *
					Matrix4.CreateRotationX(camRotY) *
					Matrix4.CreateTranslation(0, 0, camDistance);

				mainDrawable.Draw(this);

				//can't get this to work right now...

				//shader = Framework.orientationCubeShader;

				//shader.Setup(mtxMdl, mtxCam, mtxProj);

				//GL.Uniform1(Framework.orientationCubeShader["tex"], Framework.TextureSheet - 1);
				//GL.UniformMatrix4(Framework.orientationCubeShader["mtx"], false, ref orientationCubeMtx);

				//GL.BindVertexArray(Framework.orientationCubeVao);
				//GL.DrawArrays(PrimitiveType.Quads, 0, 4);

			}
			SwapBuffers();

			GL.UseProgram(0);
			
		}

		public override void DrawPicking()
		{
			if (DesignMode) return;
			MakeCurrent();
			GL.ClearColor(0f, 0f, 0f, 0f);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			if (stereoscopy)
				GL.Viewport(0, 0, Width / 2, Height);
			else
				GL.Viewport(0, 0, Width, Height);

			mainDrawable.DrawPicking(this);
		}
		
	}
}
