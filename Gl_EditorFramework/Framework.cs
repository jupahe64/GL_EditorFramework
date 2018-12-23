using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GL_EditorFramework.GL_Core;
using OpenTK.Graphics.OpenGL;

namespace GL_EditorFramework
{
	public sealed class Framework {
		public static void Initialize()
		{
			if (initialized)
				return;

			//texture sheet
			TextureSheet = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, TextureSheet);

			var bmp = OpenGl_EditorFramework.Properties.Resources.TextureSheet;
			var bmpData = bmp.LockBits(
				new System.Drawing.Rectangle(0, 0, 128*4, 128*2),
				System.Drawing.Imaging.ImageLockMode.ReadOnly,
				System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, 128*4, 128*2, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
			bmp.UnlockBits(bmpData);
			
			//orientation cube

			orientationCubeShader = new ShaderProgram(
				new FragmentShader(
				  @"#version 330
				uniform sampler2D tex;
				layout(location = 1) in vec2 uv;
				
				void main(){
					gl_FragColor = texture(tex, uv);
				}"), 
				new VertexShader(
				  @"#version 330
				layout(location = 0) in vec4 position;
				
				uniform mat4 mtx;
				void main(){
					gl_Position = mtx*position;
				}"));

			int buffer;
			
			GL.BindVertexArray(orientationCubeVao = GL.GenVertexArray());

			GL.BindBuffer(BufferTarget.ArrayBuffer, buffer = GL.GenBuffer());

			float[] data = new float[]
			{
				-1, 1, 0,    0,   0,
				 1, 1, 0,0.25f,   0,
				 1,-1, 0,0.25f,0.5f,
				-1,-1, 0,    0,0.5f,
			};
			GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * data.Length, data, BufferUsageHint.StaticDraw);

			GL.EnableVertexAttribArray(0);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(float) * 5, 0);
			GL.EnableVertexAttribArray(1);
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(float) * 5, sizeof(float) * 3);

			initialized = true;
		}
		private static bool initialized = false;
		public static int TextureSheet;

		public static ShaderProgram orientationCubeShader;

		public static int orientationCubeVao;
	}
}
