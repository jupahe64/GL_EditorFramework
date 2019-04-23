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
        private int fragSh, vertSh, geomSh = -1;
        private Matrix4 modelMatrix;
        private Matrix4 computedCamMtx;
        private Dictionary<string, int> attributes = new Dictionary<string, int>();
        private int activeAttributeCount;
        public int program;

        public ShaderProgram(FragmentShader frag, VertexShader vert)
        {
            LoadShaders(new Shader[] { vert, frag });
        }

        public ShaderProgram(FragmentShader frag, VertexShader vert, GeomertyShader geom)
        {
            LoadShaders(new Shader[] { vert, frag, geom });
        }

        public ShaderProgram(Shader[] shaders)
        {
            LoadShaders(shaders);
        }

        private void LoadShaders(Shader[] shaders)
        {
            program = GL.CreateProgram();

            foreach (Shader shader in shaders)
            {
                AttachShader(shader);
            }

            GL.LinkProgram(program);
            foreach (Shader shader in shaders)
            {
                if (shader.type == ShaderType.VertexShader)
                {
                    Console.WriteLine("vertex:");
                    vertSh = shader.id;
                }
                if (shader.type == ShaderType.FragmentShader)
                {
                    Console.WriteLine("fragment:");
                    fragSh = shader.id;
                }
                if (shader.type == ShaderType.GeometryShader)
                {
                    Console.WriteLine("geometry:");
                    geomSh = shader.id;
                }

                string log = GL.GetShaderInfoLog(shader.id);
                Console.WriteLine(log);
                if (log != "")
                    MessageBox.Show(log);
            }
            LoadAttributes();
        }

        public void AttachShader(Shader shader)
        {
            Console.WriteLine("shader:");

            string log = GL.GetShaderInfoLog(shader.id);
            Console.WriteLine(log);
            if (log != "")
                MessageBox.Show(log);

            GL.AttachShader(program, shader.id);
        }

        public void DetachShader(Shader shader)
        {
            GL.DetachShader(program, shader.id);
        }

        public void LinkShaders()
        {
            GL.LinkProgram(program);
        }

        public void SetFragmentShader(FragmentShader shader)
        {
            GL.DetachShader(program, fragSh);
            GL.AttachShader(program, shader.id);
            fragSh = shader.id;
            GL.LinkProgram(program);
        }

        public void SetVertexShader(VertexShader shader)
        {
            GL.DetachShader(program, vertSh);
            GL.AttachShader(program, shader.id);
            vertSh = shader.id;
            GL.LinkProgram(program);

            GL.UniformMatrix4(GL.GetUniformLocation(program, "mtxMdl"), false, ref modelMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(program, "mtxCam"), false, ref computedCamMtx);
        }

        public void SetGeometryShader(VertexShader shader)
        {
            if(geomSh != -1)
                GL.DetachShader(program, geomSh);

            GL.AttachShader(program, shader.id);
            geomSh = shader.id;
            GL.LinkProgram(program);

            GL.UniformMatrix4(GL.GetUniformLocation(program, "mtxMdl"), false, ref modelMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(program, "mtxCam"), false, ref computedCamMtx);
        }

        public void Setup(Matrix4 mtxMdl, Matrix4 mtxCam, Matrix4 mtxProj)
        {
            GL.UseProgram(program);
            modelMatrix = mtxMdl;
            int mtxMdl_loc = GL.GetUniformLocation(program, "mtxMdl");
            if (mtxMdl_loc != -1)
                GL.UniformMatrix4(mtxMdl_loc, false, ref modelMatrix);

            computedCamMtx = mtxCam * mtxProj;

            int mtxCam_loc = GL.GetUniformLocation(program, "mtxCam");
            if (mtxCam_loc != -1)
                GL.UniformMatrix4(mtxCam_loc, false, ref computedCamMtx);
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

        public int this[string name]
        {
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

        public void SetBoolToInt(string name, bool value)
        {
            if (value)
                GL.Uniform1(this[name], 1);
            else
                GL.Uniform1(this[name], 0);
        }

        public void SetMatrix4x4(string name, ref Matrix4 value, bool Transpose = false)
        {
            GL.UniformMatrix4(this[name], Transpose, ref value);
        }

        public void SetVector4(string name, Vector4 value)
        {
            GL.Uniform4(this[name], value);
        }

        public void SetVector3(string name, Vector3 value)
        {
            GL.Uniform3(this[name], value);
        }

        public void SetVector2(string name, Vector2 value)
        {
            GL.Uniform2(this[name], value);
        }

        public void SetFloat(string name, float value)
        {
            GL.Uniform1(this[name], value);
        }

        public void SetInt(string name, int value)
        {
            GL.Uniform1(this[name], value);
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
