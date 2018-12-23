using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GL_EditorFramework;
using GL_EditorFramework.EditorDrawables;
using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Testing
{
	class AnimatedObject : EditableObject
	{
		public override void Draw(GL_ControlModern control)
		{
			GL.LineWidth(2.0f);

			control.CurrentShader = solidColorShaderProgram;
			Matrix4 mtx = Matrix4.CreateScale(1f,0.25f,1f);
			mtx *= Matrix4.CreateFromAxisAngle(Vector3.UnitY, -(float)Math.PI / 2f);
			mtx *= Matrix4.CreateTranslation(Position);
			control.UpdateModelMatrix(mtx);

			bool hovered = EditorScene.IsHovered();
			bool selected = EditorScene.IsSelected();
			if (hovered && selected)
				GL.Uniform4(defaultShaderProgram["color"], hoverColor);
			else if (hovered || selected)
				GL.Uniform4(defaultShaderProgram["color"], selectColor);
			else
				GL.Uniform4(defaultShaderProgram["color"], CubeColor);
			GL.BindVertexArray(linesVao);
			GL.DrawArrays(PrimitiveType.Lines, 0, 24);

			mtx *= Matrix4.CreateTranslation(Vector3.UnitX * 3f);
			control.UpdateModelMatrix(mtx);

			GL.DrawArrays(PrimitiveType.Lines, 0, 24);

			mtx *= Matrix4.CreateTranslation(- Vector3.UnitX * Math.Abs(control.RedrawerFrame * 0.0625f % 6f - 3f));
			control.UpdateModelMatrix(mtx);

			control.CurrentShader = defaultShaderProgram;
			GL.Uniform1(defaultShaderProgram["tex"], Framework.TextureSheet - 1);

			if (hovered && selected)
				GL.Uniform4(defaultShaderProgram["color"], CubeColor * 0.5f + hoverColor * 0.5f);
			else if (hovered || selected)
				GL.Uniform4(defaultShaderProgram["color"], CubeColor * 0.5f + selectColor * 0.5f);
			else
				GL.Uniform4(defaultShaderProgram["color"], CubeColor);
			GL.BindVertexArray(blockVao);
			GL.DrawArrays(PrimitiveType.Quads, 0, 24);
		}

		public override void DrawPicking(GL_ControlModern control)
		{
			control.CurrentShader = solidColorShaderProgram;
			GL.Uniform4(solidColorShaderProgram["color"], control.nextPickingColor());
			Matrix4 mtx = Matrix4.CreateScale(1f, 0.25f, 1f);
			mtx *= Matrix4.CreateTranslation(Position + Vector3.UnitX * (3f - Math.Abs(control.RedrawerFrame * 0.0625f % 6f - 3)));
			control.UpdateModelMatrix(mtx);

			GL.BindVertexArray(blockVao);
			GL.DrawArrays(PrimitiveType.Quads, 0, 24);
		}

		public override uint Select(int index, I3DControl control)
		{
			control.AttachPickingRedrawer();
			return base.Select(index, control);
		}

		public override uint Deselect(int index, I3DControl control)
		{
			control.DetachPickingRedrawer();
			return base.Deselect(index, control);
		}
	}
}
