using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GL_EditorFramework.EditorDrawables
{
    public class EditorScene : EditorSceneBase
    {
        protected override IEnumerable<IEditableObject> GetObjects() => objects;

        public List<IEditableObject> objects = new List<IEditableObject>();
        
        public EditorScene(bool multiSelect = true)
        {
            this.multiSelect = multiSelect;
            CurrentList = objects;
        }

        public void Add(IList list, params IEditableObject[] objs)
        {
            Add(list, list == CurrentList, objs);
        }

        public void Delete(IList list, params IEditableObject[] objs)
        {
            Delete(list, list == CurrentList, objs);
        }

        public void InsertAt(IList list, int index, params IEditableObject[] objs)
        {
            InsertAt(list, list == objects, index, objs);
        }

        public override void DeleteSelected()
        {
            DeletionManager manager = new DeletionManager();

            foreach (IEditableObject obj in objects)
                obj.DeleteSelected(manager, objects, CurrentList);

            _ExecuteDeletion(manager);
        }
    }
}
