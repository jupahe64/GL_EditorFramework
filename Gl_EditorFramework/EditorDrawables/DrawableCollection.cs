using GL_EditorFramework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GL_EditorFramework.EditorDrawables
{
    public class DrawableCollection : AbstractDrawableCollection
    {
        public List<AbstractGlDrawable> Drawables = new List<AbstractGlDrawable>();

        protected override IEnumerable<AbstractGlDrawable> GetDrawables() => Drawables;
    }
}
