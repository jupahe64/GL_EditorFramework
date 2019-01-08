using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GL_EditorFramework.GL_Core
{
	public class ShaderProgram
	{
		private int fragSh, vertSh, program;
		private Matrix4 modelMatrix;
		private Matrix4 computedCamMtx;
        private Dictionary<string, int> attributes = new Dictionary<string, int>();
        private int activeAttributeCount;

        public ShaderProgram(FragmentShader frag, VertexShader vert)
		{
			fragSh = frag.shader;
			vertSh = vert.shader;
			program = GL.CreateProgram();
			GL.AttachShader(program, vertSh);
			GL.AttachShader(program, fragSh);
			GL.LinkProgram(program);
			Console.WriteLine("fragment:");
			Console.WriteLine(GL.GetShaderInfoLog(fragSh));
			Console.WriteLine("vertex:");
			Console.WriteLine(GL.GetShaderInfoLog(vertSh));

            LoadAttributes();
        }

        public void AttachShader(Shader shader)
		{
			Console.WriteLine("shader:");
			Console.WriteLine(GL.GetShaderInfoLog(shader.shader));
			GL.AttachShader(program, shader.shader);
        }

        public void DetachShader(Shader shader)
		{
			GL.DetachShader(program, shader.shader);
		}

		public void LinkShaders()
		{
			GL.LinkProgram(program);
        }

		public void SetFragmentShader(FragmentShader shader)
		{
			GL.DetachShader(program, fragSh);
			GL.AttachShader(program, shader.shader);
			fragSh = shader.shader;
			GL.LinkProgram(program);
		}

		public void SetVertexShader(VertexShader shader)
		{
			GL.DetachShader(program, vertSh);
			GL.AttachShader(program, shader.shader);
			vertSh = shader.shader;
			GL.LinkProgram(program);

			GL.UniformMatrix4(GL.GetUniformLocation(program, "mtxMdl"), false, ref modelMatrix);
			GL.UniformMatrix4(GL.GetUniformLocation(program, "mtxCam"), false, ref computedCamMtx);
		}

		public void Setup(Matrix4 mtxMdl, Matrix4 mtxCam, Matrix4 mtxProj)
		{
			GL.UseProgram(program);
			modelMatrix = mtxMdl;
			int mtxMdl_loc = GL.GetUniformLocation(program, "mtxMdl");
			if(mtxMdl_loc!=-1)
				GL.UniformMatrix4(mtxMdl_loc, false, ref modelMatrix);

			computedCamMtx = mtxCam * mtxProj;

			int mtxCam_loc = GL.GetUniformLocation(program, "mtxCam");
			if (mtxCam_loc != -1)
				GL.UniformMatrix4(GL.GetUniformLocation(program, "mtxCam"), false, ref computedCamMtx);
		}

		public void UpdateModelMatrix(Matrix4 matrix)
		{
			modelMatrix = matrix;
			int mtxMdl_loc = GL.GetUniformLocation(program, "mtxMdl");
			if (mtxMdl_loc != -1)
				GL.UniformMatrix4(mtxMdl_loc, false, ref modelMatrix);
		}

		public void Activate()
		{
			GL.UseProgram(program);
		}

		public int this[string name]{
			get => GL.GetUniformLocation(program, name);
		}

        private void LoadAttributes()
        {
            attributes.Clear();

            GL.GetProgram(program, GetProgramParameterName.ActiveAttributes, out activeAttributeCount);
            for (int i = 0; i < activeAttributeCount; i++)
                AddAttribute(i);
        }

        private void AddAttribute(int index)
        {
            string name = GL.GetActiveAttrib(program, index, out int size, out ActiveAttribType type);
            int location = GL.GetAttribLocation(program, name);

            // Overwrite existing vertex attributes.
            attributes[name] = location;
        }

        public int GetAttribute(string name)
        {
            if (string.IsNullOrEmpty(name) || !attributes.ContainsKey(name))
                return -1;
            else
                return attributes[name];
        }

        public void EnableVertexAttributes()
        {
            foreach (KeyValuePair<string, int> attrib in attributes)
            {
                GL.EnableVertexAttribArray(attrib.Value);
            }
        }

        public void DisableVertexAttributes()
        {
            foreach (KeyValuePair<string, int> attrib in attributes)
            {
                GL.DisableVertexAttribArray(attrib.Value);
            }
        }

        public void UniformBoolToInt(string name, bool value)
        {
            if (value)
                GL.Uniform1(this[name], 1);
            else
                GL.Uniform1(this[name], 0);
        }
    }

	public class Shader
	{
		public Shader(string src, ShaderType type)
		{
			shader = GL.CreateShader(type);
			GL.ShaderSource(shader, src);
			GL.CompileShader(shader);
			this.type = type;
		}

		public ShaderType type;

		public int shader;
	}

	public class FragmentShader : Shader
	{
		public FragmentShader(string src)
			:base(src, ShaderType.FragmentShader)
		{

		}
	}

	public class VertexShader : Shader
	{
		public VertexShader(string src)
			: base(src, ShaderType.VertexShader)
		{

		}
	}
}
