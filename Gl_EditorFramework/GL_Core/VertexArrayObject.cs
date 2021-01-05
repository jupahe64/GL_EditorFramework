using System.Collections.Generic;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GL_EditorFramework.GL_Core
{
    public class VertexArrayObject
    {
        public Dictionary<GLControl, int> vaos;
        public readonly int buffer;
        public readonly int? indexBuffer;
        private readonly Dictionary<int, VertexAttribute> attributes;

        /// <summary>
        /// Creates an object to which you can add Attributes. When you are done call Submit()!
        /// </summary>
        /// <param name="buffer">The opengl buffer where all the vertexdata is/will be stored</param>
        /// <param name="indexBuffer">The opengl buffer where all the indices are/will be stored</param>
        public VertexArrayObject(int buffer, int? indexBuffer = null)
        {
            vaos = new Dictionary<GLControl, int>();
            this.buffer = buffer;
            this.indexBuffer = indexBuffer;
            attributes = new Dictionary<int, VertexAttribute>();
        }

        public void AddAttribute(int index, int size, VertexAttribPointerType type, bool normalized, int stride, int offset)
        {
            attributes[index] = new VertexAttribute(size, type, normalized, stride, offset);
        }

        public void Submit()
        {
            foreach (GL_ControlModern control in Framework.modernGlControls)
            {
                control.MakeCurrent();
                Initialize(control);
            }

            Framework.vaos.Add(this);
        }

        public void Delete(GLControl control)
        {
            GL.DeleteVertexArray(vaos[control]);
            vaos.Remove(control);
        }

        internal void Initialize(GL_ControlModern control)
        {
            if (vaos.ContainsKey(control))
                return;


            GL.GenVertexArrays(1, out int vao);
            if (GL.GetError() == ErrorCode.InvalidOperation) Debugger.Break();
            GL.BindVertexArray(vao);
            if (GL.GetError() == ErrorCode.InvalidOperation) Debugger.Break();
            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);

            foreach (KeyValuePair<int, VertexAttribute> a in attributes)
            {
                GL.EnableVertexAttribArray(a.Key);
                GL.VertexAttribPointer(a.Key, a.Value.size, a.Value.type, a.Value.normalized, a.Value.stride, a.Value.offset);
            }
            vaos[control] = vao;
        }

        /// <summary>
        /// Binds the vertex data buffer
        /// </summary>
        public void Bind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
        }

        /// <summary>
        /// Binds this VertexArrayObject and the associated IndexBuffer if there is one
        /// </summary>
        /// <param name="control"></param>
        public void Use(GLControl control)
        {
            GL.BindVertexArray(vaos[control]);
            if (GL.GetError() == ErrorCode.InvalidOperation) Debugger.Break();

            if (indexBuffer.HasValue)
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer.Value);
        }

        public struct VertexAttribute
        {
            public int size;
            public VertexAttribPointerType type;
            public bool normalized;
            public int stride;
            public int offset;
            public VertexAttribute(int size, VertexAttribPointerType type, bool normalized, int stride, int offset)
            {
                this.size = size;
                this.type = type;
                this.normalized = normalized;
                this.stride = stride;
                this.offset = offset;
            }
        }
    }
}
