using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GL_EditorFramework.GL_Core
{
    public struct VertexArrayObject
    {
        private Dictionary<GLControl, int> vaos;
        private readonly int buffer;
        private readonly int? indexBuffer;
        private readonly Dictionary<int, VertexAttribute> attributes;

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

        public void Initialize(GLControl control)
        {
            if (vaos.ContainsKey(control))
                return;

            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
            

            foreach (KeyValuePair<int, VertexAttribute> a in attributes)
            {
                GL.EnableVertexAttribArray(a.Key);
                GL.VertexAttribPointer(a.Key, a.Value.size, a.Value.type, a.Value.normalized, a.Value.stride, a.Value.offset);
            }
            vaos[control] = vao;
        }

        public void Use(GLControl control)
        {
            GL.BindVertexArray(vaos[control]);

            if (indexBuffer.HasValue)
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer.Value);
        }

        private struct VertexAttribute
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
