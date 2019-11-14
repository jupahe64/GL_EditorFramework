using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GL_EditorFramework.GL_Core
{
    public class ShaderProgram
    {
        private Matrix4 modelMatrix;
        private Matrix4 computedCamMtx;
        private Dictionary<string, int> attributes = new Dictionary<string, int>();
        private int activeAttributeCount;
        private Dictionary<string, int> uniforms = new Dictionary<string, int>();
        public Dictionary<GLControl, int> programs = new Dictionary<GLControl, int>();
        private Dictionary<ShaderType, Shader> shaders = new Dictionary<ShaderType, Shader>();

        public ShaderProgram(Shader frag, Shader vert, GLControl control)
        {
            shaders[ShaderType.FragmentShader] = frag;
            shaders[ShaderType.VertexShader] = vert;
            Link(control);
        }

        public ShaderProgram(Shader frag, Shader vert, Shader geom, GLControl control)
        {
            shaders[ShaderType.FragmentShader] = frag;
            shaders[ShaderType.VertexShader] = vert;
            shaders[ShaderType.GeometryShader] = geom;
            Link(control);
        }

        public ShaderProgram(Shader[] shaders, GLControl control)
        {
            foreach (Shader shader in shaders)
            {
                if (!this.shaders.ContainsKey(shader.type))
                    this.shaders[shader.type] = shader;
            }
            Link(control);
        }

        public void Link(GLControl control)
        {
            if (programs.ContainsKey(control))
                return;
            
            int program = GL.CreateProgram();
            
            foreach (Shader shader in shaders.Values)
            {
                GL.AttachShader(program, shader.id);
            }

            GL.LinkProgram(program);
            foreach (KeyValuePair<ShaderType, Shader> shader in shaders)
            {
                Console.WriteLine($"{shader.Key.ToString("g")}:");

                string log = GL.GetShaderInfoLog(shader.Value.id);
                Console.WriteLine(log);
                if (Framework.ShowShaderErrors && log != "")
                    MessageBox.Show(log);
            }
            LoadAttributes(program);
            LoadUniorms(program);
            programs[control] = program;
        }

        public void AttachShader(Shader shader, bool linkImediatly)
        {
            shaders[shader.type] = shader;
            foreach (int program in programs.Values)
            {
                GL.AttachShader(program, shader.id);

                if (linkImediatly)
                    GL.LinkProgram(program);
            }

            if (linkImediatly && programs.Count > 0)
            {
                LoadAttributes(programs.First().Value);
                LoadUniorms(programs.First().Value);
            }
        }

        public void DetachShader(ShaderType type, bool linkImediatly)
        {
            if (type == ShaderType.FragmentShader || type == ShaderType.VertexShader)
                throw new Exception("You can't remove the FragmentShader or the VertexShader from this Program");
            if (!shaders.ContainsKey(type))
                return;

            foreach (int program in programs.Values)
            {
                GL.DetachShader(program, shaders[type].id);

                if (linkImediatly)
                    GL.LinkProgram(program);
            }
        }

        public void LinkAll()
        {
            foreach (int program in programs.Values)
                GL.LinkProgram(program);
        }

        public void Setup(Matrix4 mtxMdl, Matrix4 mtxCam, Matrix4 mtxProj, GLControl control)
        {
            GL.UseProgram(programs[control]);
            modelMatrix = mtxMdl;
            if (uniforms.ContainsKey("mtxMdl"))
                GL.UniformMatrix4(uniforms["mtxMdl"], false, ref modelMatrix);

            computedCamMtx = mtxCam * mtxProj;

            if (uniforms.ContainsKey("mtxCam"))
                GL.UniformMatrix4(uniforms["mtxCam"], false, ref computedCamMtx);
        }

        public void UpdateModelMatrix(Matrix4 matrix, GLControl control)
        {
            modelMatrix = matrix;
            if (uniforms.ContainsKey("mtxMdl"))
                GL.UniformMatrix4(uniforms["mtxMdl"], false, ref modelMatrix);
        }

        public void Use(GLControl control)
        {
            GL.UseProgram(programs[control]);
        }
        
        public int this[string name]
        {
            get => uniforms[name];
        }

        private void LoadUniorms(int program)
        {
            uniforms.Clear();

            GL.GetProgram(program, GetProgramParameterName.ActiveUniforms, out activeAttributeCount);
            for (int i = 0; i < activeAttributeCount; i++)
            {
                string name = GL.GetActiveUniform(program, i, out int size, out ActiveUniformType type);
                int location = GL.GetUniformLocation(program, name);

                // Overwrite existing vertex attributes.
                uniforms[name] = location;
            }
        }

        private void LoadAttributes(int program)
        {
            attributes.Clear();

            GL.GetProgram(program, GetProgramParameterName.ActiveAttributes, out activeAttributeCount);
            for (int i = 0; i < activeAttributeCount; i++)
            {
                string name = GL.GetActiveAttrib(program, i, out int size, out ActiveAttribType type);
                int location = GL.GetAttribLocation(program, name);

                // Overwrite existing vertex attributes.
                attributes[name] = location;
            }
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

        public void SetBoolToInt(string name, bool value)
        {
            if (value)
                GL.Uniform1(uniforms[name], 1);
            else
                GL.Uniform1(this[name], 0);
        }

        public void SetMatrix4x4(string name, ref Matrix4 value, bool Transpose = false)
        {
            GL.UniformMatrix4(uniforms[name], Transpose, ref value);
        }

        public void SetVector4(string name, Vector4 value)
        {
            GL.Uniform4(uniforms[name], value);
        }

        public void SetVector3(string name, Vector3 value)
        {
            GL.Uniform3(uniforms[name], value);
        }

        public void SetVector2(string name, Vector2 value)
        {
            GL.Uniform2(uniforms[name], value);
        }

        public void SetFloat(string name, float value)
        {
            GL.Uniform1(uniforms[name], value);
        }

        public void SetInt(string name, int value)
        {
            GL.Uniform1(uniforms[name], value);
        }
    }

    public class Shader
    {
        public Shader(string src, ShaderType type)
        {
            id = GL.CreateShader(type);
            GL.ShaderSource(id, src);
            GL.CompileShader(id);
            this.type = type;
        }

        public ShaderType type;

        public int id;
    }

    public class FragmentShader : Shader
    {
        public FragmentShader(string src)
            : base(src, ShaderType.FragmentShader)
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

    public class GeomertyShader : Shader
    {
        public GeomertyShader(string src)
            : base(src, ShaderType.GeometryShader)
        {

        }
    }
}
