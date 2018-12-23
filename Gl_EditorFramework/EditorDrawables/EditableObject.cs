using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GL_EditorFramework.EditorDrawables
{
	public class EditableObject : AbstractGlDrawable
	{
		protected static Vector4 hoverColor = new Vector4(1, 1, 0.925f,1);
		protected static Vector4 selectColor = new Vector4(1, 1, 0.675f, 1);

		public event EventHandler PropertyChanged;

		private static bool Initialized = false;
		protected static int blockVao;
		protected static ShaderProgram defaultShaderProgram;
		protected static ShaderProgram solidColorShaderProgram;
		protected static float[][] points = new float[][]
		{
			new float[]{-1,-1,-1},
			new float[]{ 1,-1,-1},
			new float[]{-1, 1,-1},
			new float[]{ 1, 1,-1},
			new float[]{-1,-1, 1},
			new float[]{ 1,-1, 1},
			new float[]{-1, 1, 1},
			new float[]{ 1, 1, 1}
		};
		protected static int linesVao;

		public Vector3 Position = new Vector3(0, 0, 0);

		public bool Visible = true;

		public Vector4 CubeColor = new Vector4(0, 0.25f, 1, 1);

		private static void face(ref List<float> data, int p1, int p2, int p4, int p3)
		{
			data.AddRange(new float[] {
				points[p1][0], points[p1][1], points[p1][2],
				points[p2][0], points[p2][1], points[p2][2],
				points[p3][0], points[p3][1], points[p3][2],
				points[p4][0], points[p4][1], points[p4][2]
			});
		}

		private static void lineFace(ref List<float> data, int p1, int p2, int p4, int p3)
		{
			data.AddRange(new float[] {
				points[p1][0], points[p1][1], points[p1][2],
				points[p2][0], points[p2][1], points[p2][2],
				points[p2][0], points[p2][1], points[p2][2],
				points[p3][0], points[p3][1], points[p3][2],
				points[p3][0], points[p3][1], points[p3][2],
				points[p4][0], points[p4][1], points[p4][2],
				points[p4][0], points[p4][1], points[p4][2],
				points[p1][0], points[p1][1], points[p1][2]
			});
		}

		private static void line(ref List<float> data, int p1, int p2)
		{
			data.AddRange(new float[] {
				points[p1][0], points[p1][1], points[p1][2],
				points[p2][0], points[p2][1], points[p2][2]
			});
		}

		private static void faceInv(ref List<float> data, int p4, int p3, int p1, int p2)
		{
			data.AddRange(new float[] {
				points[p1][0], points[p1][1], points[p1][2],
				points[p2][0], points[p2][1], points[p2][2],
				points[p3][0], points[p3][1], points[p3][2],
				points[p4][0], points[p4][1], points[p4][2]
			});
		}

		public EditableObject()
		{

		}

		public bool isSelectable(int subObjectIndex) => true;

		public Vector3 getPosition(int subObjectIndex) => Position;
		
		public int[] getAllSelection()
		{
			return new int[] { 0 };
		}

		public override void Draw(GL_ControlModern control)
		{
			GL.LineWidth(2.0f);
			
			control.CurrentShader = defaultShaderProgram;
			GL.Uniform1(defaultShaderProgram["tex"], Framework.TextureSheet-1);
			Matrix4 mtx = Matrix4.CreateScale(0.5f);
			mtx *= Matrix4.CreateTranslation(Position);
			control.UpdateModelMatrix(mtx);

			bool hovered = EditorScene.IsHovered();
			bool selected = EditorScene.IsSelected();

			if (hovered&&selected)
				GL.Uniform4(defaultShaderProgram["color"], CubeColor * 0.5f + hoverColor * 0.5f);
			else if (hovered || selected)
				GL.Uniform4(defaultShaderProgram["color"], CubeColor * 0.5f + selectColor * 0.5f);
			else
				GL.Uniform4(defaultShaderProgram["color"], CubeColor);
			GL.BindVertexArray(blockVao);
			GL.DrawArrays(PrimitiveType.Quads, 0, 24);
			
			control.CurrentShader = solidColorShaderProgram;
			if (hovered && selected)
				GL.Uniform4(defaultShaderProgram["color"], hoverColor);
			else if (hovered || selected)
				GL.Uniform4(defaultShaderProgram["color"], selectColor);
			else
				GL.Uniform4(defaultShaderProgram["color"], CubeColor);
			GL.BindVertexArray(linesVao);
			GL.DrawArrays(PrimitiveType.Lines, 0, 24);
		}

		public override void DrawPicking(GL_ControlModern control)
		{
			control.CurrentShader = solidColorShaderProgram;
			GL.Uniform4(solidColorShaderProgram["color"], control.nextPickingColor());
			Matrix4 mtx = Matrix4.CreateScale(0.5f);
			mtx *= Matrix4.CreateTranslation(Position);
			control.UpdateModelMatrix(mtx);

			GL.BindVertexArray(blockVao);
			GL.DrawArrays(PrimitiveType.Quads, 0, 24);
		}

		public override void Draw(GL_ControlLegacy control)
		{
			control.UpdateModelMatrix(Matrix4.CreateScale(0.5f)*Matrix4.CreateTranslation(Position));
			
			GL.BindTexture(TextureTarget.Texture2D, Framework.TextureSheet);

			#region drawfaces
			GL.Begin(PrimitiveType.Quads);
			GL.Color4(CubeColor);
			GL.TexCoord2(0.71875f, 0.515625f);
			GL.Vertex3(points[7]);
			GL.TexCoord2( 0.53125f, 0.515625f);
			GL.Vertex3(points[6]);
			GL.TexCoord2( 0.53125f,   0.984375f);
			GL.Vertex3(points[2]);
			GL.TexCoord2(0.71875f,   0.984375f);
			GL.Vertex3(points[3]);

			GL.Color4(CubeColor*0.71875f);
			GL.TexCoord2( 0.53125f, 0.515625f);
			GL.Vertex3(points[4]);
			GL.TexCoord2(0.71875f, 0.515625f);
			GL.Vertex3(points[5]);
			GL.TexCoord2(0.71875f,   0.984375f);
			GL.Vertex3(points[1]);
			GL.TexCoord2( 0.53125f,   0.984375f);
			GL.Vertex3(points[0]);
			GL.End();

			GL.Begin(PrimitiveType.QuadStrip);
			GL.TexCoord2(0.71875f, 0.515625f);
			GL.Color4(CubeColor);
			GL.Vertex3(points[7]);
			GL.Color4(CubeColor * 0.71875f);
			GL.Vertex3(points[5]);
			GL.Color4(CubeColor);
			GL.Vertex3(points[6]);
			GL.Color4(CubeColor * 0.71875f);
			GL.Vertex3(points[4]);
			GL.Color4(CubeColor);
			GL.Vertex3(points[2]);
			GL.Color4(CubeColor * 0.71875f);
			GL.Vertex3(points[0]);
			GL.Color4(CubeColor);
			GL.Vertex3(points[3]);
			GL.Color4(CubeColor * 0.71875f);
			GL.Vertex3(points[1]);
			GL.Color4(CubeColor);
			GL.Vertex3(points[7]);
			GL.Color4(CubeColor * 0.71875f);
			GL.Vertex3(points[5]);
			GL.End();
			#endregion

			#region drawlines
			GL.Disable(EnableCap.Texture2D);

			GL.BindTexture(TextureTarget.Texture2D, 0);

			GL.Color4(CubeColor);

			GL.Begin(PrimitiveType.LineStrip);
			GL.Vertex3(points[6]);
			GL.Vertex3(points[2]);
			GL.Vertex3(points[3]);
			GL.Vertex3(points[7]);
			GL.Vertex3(points[6]);

			GL.Vertex3(points[4]);
			GL.Vertex3(points[5]);
			GL.Vertex3(points[1]);
			GL.Vertex3(points[0]);
			GL.Vertex3(points[4]);
			GL.End();

			GL.Begin(PrimitiveType.Lines);
			GL.Vertex3(points[2]);
			GL.Vertex3(points[0]);
			GL.Vertex3(points[3]);
			GL.Vertex3(points[1]);
			GL.Vertex3(points[7]);
			GL.Vertex3(points[5]);
			GL.End();
			#endregion
			GL.Enable(EnableCap.Texture2D);
		}

		public override void DrawPicking(GL_ControlLegacy control)
		{
			control.UpdateModelMatrix(Matrix4.CreateScale(0.5f) * Matrix4.CreateTranslation(Position));
			
			GL.Disable(EnableCap.Texture2D);

			GL.BindTexture(TextureTarget.Texture2D, 0);

			GL.Color4(control.nextPickingColor());

			
			GL.Begin(PrimitiveType.Quads);
			GL.Vertex3(points[7]);
			GL.Vertex3(points[6]);
			GL.Vertex3(points[2]);
			GL.Vertex3(points[3]);
			
			GL.Vertex3(points[4]);
			GL.Vertex3(points[5]);
			GL.Vertex3(points[1]);
			GL.Vertex3(points[0]);
			GL.End();
			
			GL.Begin(PrimitiveType.QuadStrip);
			GL.Vertex3(points[7]);
			GL.Vertex3(points[5]);
			GL.Vertex3(points[6]);
			GL.Vertex3(points[4]);
			GL.Vertex3(points[2]);
			GL.Vertex3(points[0]);
			GL.Vertex3(points[3]);
			GL.Vertex3(points[1]);
			GL.Vertex3(points[7]);
			GL.Vertex3(points[5]);
			GL.End();
			
			GL.Enable(EnableCap.Texture2D);
		}

		public override void Prepare(GL_ControlModern control)
		{
			if (!Initialized)
			{
				var defaultFrag = new FragmentShader(
				  @"#version 330
				uniform sampler2D tex;
				in vec4 fragColor;
				in vec3 fragPosition;
				in vec2 uv;
				
				void main(){
					gl_FragColor = fragColor*((fragPosition.y+2)/3)*texture(tex, uv);
				}");
				var solidColorFrag = new FragmentShader(
				  @"#version 330
				uniform vec4 color;
				void main(){
					gl_FragColor = color;
				}");
				var defaultVert = new VertexShader(
				  @"#version 330
				layout(location = 0) in vec4 position;
				uniform vec4 color;
				uniform mat4 mtxMdl;
				uniform mat4 mtxCam;
				out vec4 fragColor;
				out vec3 fragPosition;
				out vec2 uv;

				vec2 map(vec2 value, vec2 min1, vec2 max1, vec2 min2, vec2 max2) {
					return min2 + (value - min1) * (max2 - min2) / (max1 - min1);
				}

				void main(){
					fragPosition = position.xyz;
					uv = map(fragPosition.xz,vec2(-1.0625,-1.0625),vec2(1.0625,1.0625), vec2(0.5,0.5), vec2(0.75,1.0));
					gl_Position = mtxCam*mtxMdl*position;
					fragColor = color;
				}");
				var solidColorVert = new VertexShader(
				  @"#version 330
				layout(location = 0) in vec4 position;
				uniform mat4 mtxMdl;
				uniform mat4 mtxCam;
				void main(){
					gl_Position = mtxCam*mtxMdl*position;
				}");
				defaultShaderProgram = new ShaderProgram(defaultFrag, defaultVert);
				solidColorShaderProgram = new ShaderProgram(solidColorFrag, solidColorVert);

				int buffer;

				#region block
				GL.BindVertexArray(blockVao = GL.GenVertexArray());

				GL.BindBuffer(BufferTarget.ArrayBuffer, buffer = GL.GenBuffer());
				List<float> list = new List<float>();

				face(ref list, 0, 1, 2, 3);
				faceInv(ref list, 4, 5, 6, 7);
				faceInv(ref list, 0, 1, 4, 5);
				face(ref list, 2, 3, 6, 7);
				face(ref list, 0, 2, 4, 6);
				faceInv(ref list, 1, 3, 5, 7);

				float[] data = list.ToArray();
				GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * data.Length, data, BufferUsageHint.StaticDraw);

				GL.EnableVertexAttribArray(0);
				GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(float) * 3, 0);
				#endregion

				#region lines
				GL.BindVertexArray(linesVao = GL.GenVertexArray());

				GL.BindBuffer(BufferTarget.ArrayBuffer, buffer = GL.GenBuffer());
				list = new List<float>();

				lineFace(ref list, 0, 1, 2, 3);
				lineFace(ref list, 4, 5, 6, 7);
				line(ref list, 0, 4);
				line(ref list, 1, 5);
				line(ref list, 2, 6);
				line(ref list, 3, 7);

				data = list.ToArray();
				GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * data.Length, data, BufferUsageHint.StaticDraw);

				GL.EnableVertexAttribArray(0);
				GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(float) * 3, 0);
				#endregion
				

				Initialized = true;
			}
		}

		public override void Prepare(GL_ControlLegacy control)
		{
			
		}

		public virtual uint Select(int index, I3DControl control)
		{
			return 0;
		}

		public virtual uint Deselect(int index, I3DControl control)
		{
			return 0;
		}

		public void Translate(Vector3 lastPos, Vector3 translate, int subObj)
		{
			Position = lastPos + translate;
		}

		public override uint MouseUp(MouseEventArgs e, I3DControl control)
		{
			PropertyChanged?.Invoke(this, new EventArgs());
			return base.MouseUp(e, control);
		}
	}
}
