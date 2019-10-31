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
using static GL_EditorFramework.Framework;

namespace GL_EditorFramework.EditorDrawables
{
    public delegate void ListChangedEventHandler(object sender, ListChangedEventArgs e);

    public class ListChangedEventArgs : EventArgs
    {
        public IList[] Lists { get; set; }
        public ListChangedEventArgs(IList[] lists)
        {
            Lists = lists;
        }
    }

    public delegate void DictChangedEventHandler(object sender, DictChangedEventArgs e);

    public class DictChangedEventArgs : EventArgs
    {
        public IDictionary[] Dicts { get; set; }
        public DictChangedEventArgs(IDictionary[] dicts)
        {
            Dicts = dicts;
        }
    }

    public abstract partial class EditorSceneBase : AbstractGlDrawable
    {
        protected abstract IEnumerable<IEditableObject> GetObjects();

        protected bool multiSelect;

        public IEditableObject Hovered { get; protected set; } = null;

        public int HoveredPart { get; protected set; } = 0;

        public readonly SelectionSet SelectedObjects;


        public class SelectionSet : ISet<object>
        {
            EditorSceneBase scene;

            public SelectionSet(EditorSceneBase scene)
            {
                this.scene = scene;
            }

            #region unimplemented methods

            public bool IsReadOnly => throw new NotImplementedException();

            public bool Add(object item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public void ExceptWith(IEnumerable<object> other)
            {
                throw new NotImplementedException();
            }

            public void IntersectWith(IEnumerable<object> other)
            {
                throw new NotImplementedException();
            }

            public bool IsProperSubsetOf(IEnumerable<object> other)
            {
                throw new NotImplementedException();
            }

            public bool IsProperSupersetOf(IEnumerable<object> other)
            {
                throw new NotImplementedException();
            }

            public bool IsSubsetOf(IEnumerable<object> other)
            {
                throw new NotImplementedException();
            }

            public bool IsSupersetOf(IEnumerable<object> other)
            {
                throw new NotImplementedException();
            }

            public bool Overlaps(IEnumerable<object> other)
            {
                throw new NotImplementedException();
            }

            public bool Remove(object item)
            {
                throw new NotImplementedException();
            }

            public bool SetEquals(IEnumerable<object> other)
            {
                throw new NotImplementedException();
            }

            public void SymmetricExceptWith(IEnumerable<object> other)
            {
                throw new NotImplementedException();
            }

            public void UnionWith(IEnumerable<object> other)
            {
                throw new NotImplementedException();
            }

            void ICollection<object>.Add(object item)
            {
                throw new NotImplementedException();
            }
            #endregion

            public void CopyTo(object[] array, int arrayIndex)
            {
                foreach (object obj in scene.GetObjects().Where(x => x.IsSelected()))
                    array[arrayIndex++] = obj;
            }

            public bool Contains(object item)
            {
                return ((ISelectable)item).IsSelected();
            }

            public IEnumerator<object> GetEnumerator()
            {
                return scene.GetAllSelected().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return scene.GetAllSelected().GetEnumerator();
            }

            public int Count => scene.GetObjects().Count(x => x.IsSelected());
        }


        public EditorSceneBase()
        {
            SelectedObjects = new SelectionSet(this);
        }

        public IList CurrentList { get; set; }

        public List<AbstractGlDrawable> StaticObjects { get; set; } = new List<AbstractGlDrawable>();

        public event EventHandler SelectionChanged;
        public event EventHandler ObjectsMoved;
        public event ListChangedEventHandler ListChanged;
        public event DictChangedEventHandler DictChanged;
        public event ListEventHandler ListEntered;
        public event ListEventHandler ListInvalidated;

        protected float draggingDepth;

        public GL_ControlBase GL_Control => control;

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

        public static NoTransformAction NoAction {get;} = new NoTransformAction();

        /// <summary>
        /// Sets the CurrentList to <paramref name="list"/> and triggers the <see cref="ListEntered"/> event
        /// </summary>
        /// <param name="list"></param>
        public void EnterList(IList list)
        {
            CurrentList = list;
            ListEntered?.Invoke(this, new ListEventArgs(list));
        }

        public void InvalidateList(IList list)
        {
            if(list==CurrentList)
                CurrentList = null;

            ListInvalidated?.Invoke(this, new ListEventArgs(list));
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

        private IEnumerable<ISelectable> GetAllSelected()
        {
            foreach (IEditableObject point in GetObjects())
            {
                foreach (ISelectable selected in point.GetSelected())
                {
                    yield return selected;
                }
            }
        }

        /// <summary>
        /// Deletes all selected objects if they can be deleted, this action is undoable
        /// </summary>
        public abstract void DeleteSelected();

        /// <summary>
        /// Sets up an <see cref="ObjectUIControl"/> based on the currently selected objects
        /// </summary>
        public void SetupObjectUIControl(ObjectUIControl objectUIControl)
        {
            objectUIControl.ClearObjectUIContainers();

            bool atleastOne = false;
            foreach (IEditableObject obj in GetObjects())
            {

                if (obj.TrySetupObjectUIControl(this, objectUIControl))
                {
                    if (atleastOne)
                    {
                        objectUIControl.ClearObjectUIContainers();
                        return;
                    }
                    else
                        atleastOne = true;
                }
            }
            objectUIControl.Refresh();
        }

        /// <summary>
        /// Defines how the selection behaves to just added objects
        /// </summary>
        public enum SelectionBehavior
        {
            /// <summary>
            /// Adds all added objects to the selection
            /// </summary>
            ADD,
            /// <summary>
            /// Only Selects the Added objects
            /// </summary>
            CHANGE,
            /// <summary>
            /// Keeps all added objects unselected
            /// </summary>
            KEEP
        }

        /// <summary>
        /// Adds one or more objects to a list, this action is undoable
        /// </summary>
        /// <param name="list">The list the objects are added to</param>
        /// <param name="objs">The objects which are added to the list</param>
        public void Add(IList list, params IEditableObject[] objs)
        {
            Add(list, SelectionBehavior.CHANGE, objs);
        }

        /// <summary>
        /// Adds one or more objects to a list, this action is undoable
        /// </summary>
        /// <param name="list">The list the objects are added to</param>
        /// <param name="objs">The objects which are added to the list</param>
        /// /// <param name="selectionBehavior">Defines how the selection behaves after adding the objects</param>
        public void Add(IList list, SelectionBehavior selectionBehavior, params IEditableObject[] objs)
        {
            switch (selectionBehavior)
            {
                case SelectionBehavior.ADD:
                    uint var = REDRAW_PICKING;

                    foreach (IEditableObject obj in objs)
                    {
                        list.Add(obj);
                        var |= obj.SelectDefault(control);
                    }

                    UpdateSelection(var);
                    break;

                case SelectionBehavior.CHANGE:
                    var = REDRAW_PICKING;
                    foreach (IEditableObject obj in GetObjects())
                    {
                        var |= obj.DeselectAll(control);
                    }

                    foreach (IEditableObject obj in objs)
                    {
                        list.Add(obj);
                        var |= obj.SelectDefault(control);
                    }

                    UpdateSelection(var);
                    break;

                case SelectionBehavior.KEEP:
                    foreach (IEditableObject obj in objs)
                        list.Add(obj);

                    control.Refresh();
                    control.DrawPicking();
                    break;
            }

            foreach (IEditableObject obj in GetObjects())
                obj.ListChanged(list);

            AddToUndo(new RevertableAddition(new RevertableAddition.AddInListInfo[] { new RevertableAddition.AddInListInfo(objs, list) }, new RevertableAddition.SingleAddInListInfo[0]));
        }

        /// <summary>
        /// Inserts one or more objects into a list at a specified index, this action is undoable
        /// </summary>
        /// <param name="list">The list the objects are inserted into</param>
        /// <param name="objs">The objects which are inserted into the list</param>
        /// <param name="index">The index at which the objects are inserted</param>
        /// <param name="selectionBehavior">Defines how the selection behaves after inserting the objects</param>
        public void InsertAt(IList list, int index, params IEditableObject[] objs)
        {
            InsertAt(list, index, SelectionBehavior.CHANGE, objs);
        }

        /// <summary>
        /// Inserts one or more objects into a list at a specified index, this action is undoable
        /// </summary>
        /// <param name="list">The list the objects are inserted into</param>
        /// <param name="objs">The objects which are inserted into the list</param>
        /// <param name="index">The index at which the objects are inserted</param>
        /// <param name="selectionBehavior">Defines how the selection behaves after inserting the objects</param>
        public void InsertAt(IList list, int index, SelectionBehavior selectionBehavior, params IEditableObject[] objs)
        {
            switch (selectionBehavior)
            {
                case SelectionBehavior.ADD:
                    uint var = REDRAW_PICKING;
                    foreach (IEditableObject obj in objs)
                    {
                        list.Insert(index, obj);

                        var |= obj.SelectDefault(control);
                        index++;
                    }

                    UpdateSelection(var);
                    break;

                case SelectionBehavior.CHANGE:
                    var = REDRAW_PICKING;
                    foreach (IEditableObject obj in GetObjects())
                    {
                        var |= obj.DeselectAll(control);
                    }

                    foreach (IEditableObject obj in objs)
                    {
                        list.Insert(index, obj);

                        var |= obj.SelectDefault(control);
                        index++;
                    }

                    UpdateSelection(var);
                    break;

                case SelectionBehavior.KEEP:
                    foreach (IEditableObject obj in objs)
                    {
                        list.Insert(index, obj);
                        index++;
                    }
                    control.Refresh();
                    control.DrawPicking();
                    break;
            }

            foreach (IEditableObject obj in GetObjects())
                obj.ListChanged(list);

            AddToUndo(new RevertableAddition(new RevertableAddition.AddInListInfo[] { new RevertableAddition.AddInListInfo(objs, list) }, new RevertableAddition.SingleAddInListInfo[0]));
        }

        /// <summary>
        /// Deletes one or more objects from a list, this action is undoable
        /// </summary>
        /// <param name="list">The list the objects are deleted from</param>
        /// <param name="objs">The objects which are deleted from the list</param>
        public void Delete(IList list, params IEditableObject[] objs)
        {
            uint var = 0;

            RevertableDeletion.DeleteInfo[] infos = new RevertableDeletion.DeleteInfo[objs.Length];

            int index = 0;

            foreach (IEditableObject obj in objs)
            {
                infos[index] = new RevertableDeletion.DeleteInfo(obj, list.IndexOf(obj));
                list.Remove(obj);
                var |= obj.DeselectAll(control);
                index++;
            }

            AddToUndo(new RevertableDeletion(new RevertableDeletion.DeleteInListInfo[] { new RevertableDeletion.DeleteInListInfo(infos, list) }, new RevertableDeletion.SingleDeleteInListInfo[0]));

            UpdateSelection(var);
        }

        public class DeletionManager
        {
            public Dictionary<IList, List<IEditableObject>> Dictionary { get; private set; } = new Dictionary<IList, List<IEditableObject>>();

            public void Add(IList list, params IEditableObject[] objs)
            {
                if (!Dictionary.ContainsKey(list))
                    Dictionary[list] = new List<IEditableObject>();

                Dictionary[list].AddRange(objs);
            }
        }

        public void DeleteSelected(IList list)
        {
            DeletionManager manager = new DeletionManager();

            foreach (IEditableObject obj in list)
                obj.DeleteSelected(this, manager, list);

            _ExecuteDeletion(manager);
        }

        protected void _ExecuteDeletion(DeletionManager manager)
        {
            if (manager.Dictionary.Count == 0)
                return;

            List<RevertableDeletion.DeleteInListInfo> infos = new List<RevertableDeletion.DeleteInListInfo>();
            List<RevertableDeletion.SingleDeleteInListInfo> singleInfos = new List<RevertableDeletion.SingleDeleteInListInfo>();

            uint var = 0;

            foreach (KeyValuePair<IList, List<IEditableObject>> entry in manager.Dictionary)
            {
                if (entry.Value.Count < 1)
                    throw new Exception("entry has no objects");

                if (entry.Value.Count == 1)
                {
                    singleInfos.Add(new RevertableDeletion.SingleDeleteInListInfo(entry.Value[0], entry.Key.IndexOf(entry.Value[0]), entry.Key));
                    var |= entry.Value[0].DeselectAll(control);
                    entry.Key.Remove(entry.Value[0]);
                }
                else
                {
                    RevertableDeletion.DeleteInfo[] deleteInfos = new RevertableDeletion.DeleteInfo[entry.Value.Count];
                    int i = 0;
                    foreach (IEditableObject obj in entry.Value)
                    {
                        deleteInfos[i++] = new RevertableDeletion.DeleteInfo(obj, entry.Key.IndexOf(obj));
                        var |= obj.DeselectAll(control);
                        entry.Key.Remove(obj);
                    }
                    infos.Add(new RevertableDeletion.DeleteInListInfo(deleteInfos, entry.Key));
                }
            }

            UpdateSelection(var);

            AddToUndo(new RevertableDeletion(infos.ToArray(), singleInfos.ToArray()));
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

            foreach (IEditableObject obj in GetObjects())
                obj.ApplyTransformActionToSelection(CurrentAction, ref transformChangeInfos);

            CurrentAction = NoAction;

            AddTransformToUndo(transformChangeInfos);
        }

        public void ToogleSelected(IEditableObject obj, bool isSelected)
        {
            uint var = 0;
            if (isSelected)
            {
                var |= obj.SelectDefault(control);
            }
            else
            {
                var |= obj.DeselectAll(control);
            }
        }
    }
}
