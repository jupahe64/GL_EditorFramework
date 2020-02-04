using GL_EditorFramework.EditorDrawables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GL_EditorFramework
{
    public static class ObjectRenderState
    {
        public static Func<IEditableObject, bool> ShouldBeDrawn = ShouldBeDrawn_Default;

        public static bool ShouldBeDrawn_Default(IEditableObject obj) => obj.Visible;
    }
}
