using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GL_EditorFramework.EditorDrawables
{
    public class CategorizedScene : EditorSceneBase
    {
        public CategorizedScene(bool multiSelect = true)
        {
            this.multiSelect = multiSelect;
        }

        protected override IEnumerable<IEditableObject> GetObjects()
        {
            foreach(List<IEditableObject> objects in categories.Values)
            {
                foreach (IEditableObject obj in objects)
                    yield return obj;
            }
        }

        public Dictionary<string, List<IEditableObject>> categories = new Dictionary<string, List<IEditableObject>>();
        
        public override void DeleteSelected()
        {
            DeletionManager manager = new DeletionManager();

            foreach (List<IEditableObject> objects in categories.Values)
            {
                foreach (IEditableObject obj in objects)
                    obj.DeleteSelected(this, manager, objects);
            }

            _ExecuteDeletion(manager);
        }
    }
}
