using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinInput = System.Windows.Input;
using static GL_EditorFramework.EditorDrawables.EditableObject;
using System.Collections;

namespace GL_EditorFramework.EditorDrawables
{
    public delegate void ListChangedEventHandler(object sender, ListChangedEventArgs e);

    public class ListChangedEventArgs : EventArgs
    {
        public IList[] Lists;
        public ListChangedEventArgs(IList[] lists)
        {
            Lists = lists;
        }
    }

    public delegate void DictChangedEventHandler(object sender, DictChangedEventArgs e);

    public class DictChangedEventArgs : EventArgs
    {
        public IDictionary[] Dicts;
        public DictChangedEventArgs(IDictionary[] dicts)
        {
            Dicts = dicts;
        }
    }

    public delegate void CurrentListChangedEventHandler(object sender, CurrentListChangedEventArgs e);

    public class CurrentListChangedEventArgs : EventArgs
    {
        public IList List;
        public CurrentListChangedEventArgs(IList list)
        {
            List = list;
        }
    }

    public abstract partial class EditorSceneBase : AbstractGlDrawable
    {
        protected abstract IEnumerable<IEditableObject> GetObjects();

        protected bool multiSelect;

        public IEditableObject Hovered { get; protected set; } = null;

        public int HoveredPart { get; protected set; } = 0;

        public readonly HashSet<object> SelectedObjects = new HashSet<object>();
        public IList CurrentList;

        public List<AbstractGlDrawable> staticObjects = new List<AbstractGlDrawable>();

        public event EventHandler SelectionChanged;
        public event EventHandler ObjectsMoved;
        public event ListChangedEventHandler ListChanged;
        public event DictChangedEventHandler DictChanged;
        public event CurrentListChangedEventHandler CurrentListChanged;

        protected float draggingDepth;

        protected GL_ControlBase control;

        public enum DragActionType
        {
            TRANSLATE,
            ROTATE,
            SCALE,
            SCALE_EXCLUSIVE
        }

        public AbstractTransformAction CurrentAction = NoAction;

        public AbstractTransformAction ExclusiveAction = NoAction;

        public static NoTransformAction NoAction {get; private set;} = new NoTransformAction();

        public void SetCurrentList(IList list)
        {
            CurrentList = list;
            CurrentListChanged?.Invoke(this, new CurrentListChangedEventArgs(list));
        }

        public void Refresh() => control.Refresh();
        public void DrawPicking() => control.DrawPicking();

        protected void UpdateSelection(uint var)
        {
            SelectionChanged?.Invoke(this, new EventArgs());

            control.Refresh();

            if ((var & REDRAW_PICKING) > 0)
                control.DrawPicking();
        }

        public abstract void DeleteSelected();

        public IPropertyProvider GetPropertyProvider()
        {
            IPropertyProvider provider = null;
            foreach (IEditableObject obj in GetObjects())
            {

                if (obj.ProvidesProperty(this))
                {
                    if (provider == null)
                        provider = obj.GetPropertyProvider(this);
                    else
                    {
                        provider = null;
                        break;
                    }
                }
            }

            return provider;
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
            InsertAt(list, list == CurrentList, index, objs);
        }

        public void Add(IList list, bool updateSelection, params IEditableObject[] objs)
        {
            if (updateSelection)
            {
                uint var = 0;

                foreach (IEditableObject selected in SelectedObjects)
                {
                    var |= selected.DeselectAll(control, null);
                }
                SelectedObjects.Clear();

                foreach (IEditableObject obj in objs)
                    list.Add(obj);

                foreach (IEditableObject obj in GetObjects())
                    obj.ListChanged(list);

                foreach (IEditableObject obj in objs)
                    var |= obj.SelectDefault(control, SelectedObjects);

                AddToUndo(new RevertableAddition(new RevertableAddition.AddInListInfo []{new RevertableAddition.AddInListInfo(objs, list)}, new RevertableAddition.SingleAddInListInfo[0]));

                UpdateSelection(var);
            }
            else
            {
                foreach (IEditableObject obj in objs)
                    list.Add(obj);

                foreach (IEditableObject obj in GetObjects())
                    obj.ListChanged(list);

                AddToUndo(new RevertableAddition(new RevertableAddition.AddInListInfo[] { new RevertableAddition.AddInListInfo(objs, list) }, new RevertableAddition.SingleAddInListInfo[0]));
            }
        }

        public void Delete(IList list, bool updateSelection, params IEditableObject[] objs)
        {
            if (updateSelection)
            {
                uint var = 0;

                RevertableDeletion.DeleteInfo[] infos = new RevertableDeletion.DeleteInfo[objs.Length];

                int index = 0;

                foreach (IEditableObject obj in objs)
                {
                    infos[index] = new RevertableDeletion.DeleteInfo(obj, list.IndexOf(obj));
                    list.Remove(obj);
                    var |= obj.DeselectAll(control, SelectedObjects);
                    index++;
                }

                AddToUndo(new RevertableDeletion(new RevertableDeletion.DeleteInListInfo[] { new RevertableDeletion.DeleteInListInfo(infos, list) }, new RevertableDeletion.SingleDeleteInListInfo[0]));

                UpdateSelection(var);
            }
            else
            {
                RevertableDeletion.DeleteInfo[] infos = new RevertableDeletion.DeleteInfo[objs.Length];

                int index = 0;

                foreach (IEditableObject obj in objs)
                {
                    infos[index] = new RevertableDeletion.DeleteInfo(obj, list.IndexOf(obj));
                    list.Remove(obj);
                    index++;
                }

                AddToUndo(new RevertableDeletion(new RevertableDeletion.DeleteInListInfo[] { new RevertableDeletion.DeleteInListInfo(infos, list) }, new RevertableDeletion.SingleDeleteInListInfo[0]));
            }

            foreach (IEditableObject obj in GetObjects())
                obj.ListChanged(list);
        }

        public class DeletionManager
        {
            internal Dictionary<IList, List<IEditableObject>> dict = new Dictionary<IList, List<IEditableObject>>();

            public void Add(IList list, params IEditableObject[] objs)
            {
                if (!dict.ContainsKey(list))
                    dict[list] = new List<IEditableObject>();

                dict[list].AddRange(objs);
            }
        }

        public void DeleteSelected(IList list)
        {
            DeletionManager manager = new DeletionManager();

            foreach (IEditableObject obj in list)
                obj.DeleteSelected(manager, list, CurrentList);

            _ExecuteDeletion(manager);
        }

        protected void _ExecuteDeletion(DeletionManager manager)
        {
            List<RevertableDeletion.DeleteInListInfo> infos = new List<RevertableDeletion.DeleteInListInfo>();
            List<RevertableDeletion.SingleDeleteInListInfo> singleInfos = new List<RevertableDeletion.SingleDeleteInListInfo>();

            uint var = 0;

            foreach (KeyValuePair<IList, List<IEditableObject>> entry in manager.dict)
            {
                if (entry.Value.Count < 1)
                    throw new Exception("entry has no objects");

                if (entry.Value.Count == 1)
                {
                    singleInfos.Add(new RevertableDeletion.SingleDeleteInListInfo(entry.Value[0], entry.Key.IndexOf(entry.Value[0]), entry.Key));
                    var |= entry.Value[0].DeselectAll(control, SelectedObjects);
                    entry.Key.Remove(entry.Value[0]);
                }
                else
                {
                    RevertableDeletion.DeleteInfo[] deleteInfos = new RevertableDeletion.DeleteInfo[entry.Value.Count];
                    int i = 0;
                    foreach (IEditableObject obj in entry.Value)
                    {
                        deleteInfos[i++] = new RevertableDeletion.DeleteInfo(obj, entry.Key.IndexOf(obj));
                        var |= obj.DeselectAll(control, SelectedObjects);
                        entry.Key.Remove(obj);
                    }
                    infos.Add(new RevertableDeletion.DeleteInListInfo(deleteInfos, entry.Key));
                }
            }

            UpdateSelection(var);

            AddToUndo(new RevertableDeletion(infos.ToArray(), singleInfos.ToArray()));
        }

        public void InsertAt(IList list, bool updateSelection, int index, params IEditableObject[] objs)
        {
            if (updateSelection)
            {
                uint var = 0;

                foreach (IEditableObject selected in SelectedObjects)
                {
                    var |= selected.DeselectAll(control, null);
                }
                SelectedObjects.Clear();

                foreach (IEditableObject obj in objs)
                {
                    list.Insert(index, obj);
                    
                    var |= obj.SelectDefault(control, SelectedObjects);
                    index++;
                }

                AddToUndo(new RevertableAddition(new RevertableAddition.AddInListInfo[] { new RevertableAddition.AddInListInfo(objs, list) }, new RevertableAddition.SingleAddInListInfo[0]));

                UpdateSelection(var);
            }
            else
            {
                foreach (IEditableObject obj in objs)
                {
                    list.Insert(index, obj);
                    index++;
                }

                AddToUndo(new RevertableAddition(new RevertableAddition.AddInListInfo[] { new RevertableAddition.AddInListInfo(objs, list) }, new RevertableAddition.SingleAddInListInfo[0]));
            }
        }

        public void ReorderObjects(IList list, int originalIndex, int count, int offset)
        {
            List<object> objs = new List<object>();

            for (int i = 0; i < count; i++)
            {
                objs.Add(list[originalIndex]);
                list.RemoveAt(originalIndex);
            }

            int index = originalIndex + offset;
            foreach (object obj in objs)
            {
                list.Insert(index, obj);
                index++;
            }

            AddToUndo(new RevertableReordering(originalIndex + offset, count, -offset, list));
        }

        public void ApplyCurrentTransformAction()
        {
            TransformChangeInfos transformChangeInfos = new TransformChangeInfos(new List<TransformChangeInfo>());

            foreach (IEditableObject obj in SelectedObjects)
            {
                obj.ApplyTransformActionToSelection(CurrentAction, ref transformChangeInfos);
            }

            CurrentAction = NoAction;

            AddTransformToUndo(transformChangeInfos);
        }

        public void ToogleSelected(IEditableObject obj, bool isSelected)
        {
            uint var = 0;
            if (isSelected)
            {
                var |= obj.SelectDefault(control, SelectedObjects);
            }
            else
            {
                var |= obj.DeselectAll(control, SelectedObjects);
            }
        }
    }
}
