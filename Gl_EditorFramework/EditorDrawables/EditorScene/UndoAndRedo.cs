using GL_EditorFramework.Interfaces;
using OpenTK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GL_EditorFramework.EditorDrawables
{
    public abstract partial class EditorSceneBase : AbstractGlDrawable
    {
        protected Stack<IRevertable> undoStack = new Stack<IRevertable>();
        protected Stack<IRevertable> redoStack = new Stack<IRevertable>();

        public void Undo()
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push(undoStack.Pop().Revert(this));
                ObjectsMoved?.Invoke(this,null);
            }

        }

        public void Redo()
        {
            if(redoStack.Count > 0)
            {
                undoStack.Push(redoStack.Pop().Revert(this));
                ObjectsMoved?.Invoke(this, null);
            }
        }

        public interface IRevertable
        {
            IRevertable Revert(EditorSceneBase scene);
        }
        
        public void AddTransformToUndo(TransformChangeInfos transformChangeInfos)
        {
            if(transformChangeInfos.changedRotations > 0)
                AddToUndo(new RevertableRotChange(transformChangeInfos));

            else if (transformChangeInfos.changedScales > 0)
                AddToUndo(new RevertableScaleChange(transformChangeInfos));

            else if (transformChangeInfos.changedPositions > 0)
                AddToUndo(new RevertablePosChange(transformChangeInfos));

            if(transformChangeInfos.changedPositions+transformChangeInfos.changedRotations+transformChangeInfos.changedScales>0)
                ObjectsMoved?.Invoke(this, null);
        }

        public void AddToUndo(IRevertable revertable)
        {
            undoStack.Push(revertable);
            redoStack.Clear();
        }

        public struct RevertablePosChange : IRevertable
        {
            private PosInfo[] posInfos;

            public RevertablePosChange(TransformChangeInfos transformChangeInfos)
            {
                posInfos = new PosInfo[transformChangeInfos.changedPositions];
                int i = 0;
                foreach(TransformChangeInfo info in transformChangeInfos.infos)
                {
                    if (info.position.HasValue)
                        posInfos[i++] = new PosInfo(info.obj, info.part, info.position.Value);
                }
            }

            public IRevertable Revert(EditorSceneBase scene)
            {
                RevertablePosChange revertable = new RevertablePosChange
                {
                    posInfos = new PosInfo[posInfos.Length]
                };

                for (int i = 0; i < posInfos.Length; i++)
                {
                    posInfos[i].obj.SetTransform(posInfos[i].pos, null, null, posInfos[i].part, 
                        out Vector3? prevPos, out Quaternion? prevRot, out Vector3? prevScale);

                    revertable.posInfos[i] = new PosInfo(
                        posInfos[i].obj,
                        posInfos[i].part,
                        prevPos.Value);
                }

                return revertable;
            }

            struct PosInfo
            {
                public PosInfo(IEditableObject obj, int part, Vector3 pos)
                {
                    this.obj = obj;
                    this.part = part;
                    this.pos = pos;
                }

                public readonly IEditableObject obj;
                public readonly int part;
                public readonly Vector3 pos;
            }
        }

        public struct RevertableRotChange : IRevertable
        {
            private RotInfo[] rotInfos;

            public RevertableRotChange(TransformChangeInfos transformChangeInfos)
            {
                rotInfos = new RotInfo[transformChangeInfos.changedPositions + transformChangeInfos.changedRotations - transformChangeInfos.changedPosRots];
                int i = 0;
                foreach (TransformChangeInfo info in transformChangeInfos.infos)
                {
                    if (info.position.HasValue || info.rotation.HasValue)
                        rotInfos[i++] = new RotInfo(info.obj, info.part, info.position, info.rotation);
                }
            }

            public IRevertable Revert(EditorSceneBase scene)
            {
                RevertableRotChange revertable = new RevertableRotChange
                {
                    rotInfos = new RotInfo[rotInfos.Length]
                };

                for (int i = 0; i < rotInfos.Length; i++)
                {
                    rotInfos[i].obj.SetTransform(rotInfos[i].pos, rotInfos[i].rot, null, rotInfos[i].part,
                        out Vector3? prevPos, out Quaternion? prevRot, out Vector3? prevScale);

                    revertable.rotInfos[i] = new RotInfo(
                        rotInfos[i].obj,
                        rotInfos[i].part,
                        prevPos, prevRot);
                }

                return revertable;
            }

            struct RotInfo
            {
                public RotInfo(IEditableObject obj, int part, Vector3? pos, Quaternion? rot)
                {
                    this.obj = obj;
                    this.part = part;
                    this.pos = pos;
                    this.rot = rot;
                }

                public readonly IEditableObject obj;
                public readonly int part;
                public readonly Vector3? pos;
                public readonly Quaternion? rot;
            }
        }

        public struct RevertableScaleChange : IRevertable
        {
            private ScaleInfo[] scaleInfos;

            public RevertableScaleChange(TransformChangeInfos transformChangeInfos)
            {
                scaleInfos = new ScaleInfo[transformChangeInfos.changedPositions + transformChangeInfos.changedScales - transformChangeInfos.changedPosScales];
                int i = 0;
                foreach (TransformChangeInfo info in transformChangeInfos.infos)
                {
                    if (info.position.HasValue || info.scale.HasValue)
                        scaleInfos[i++] = new ScaleInfo(info.obj, info.part, info.position, info.scale);
                }
            }

            public IRevertable Revert(EditorSceneBase scene)
            {
                RevertableScaleChange revertable = new RevertableScaleChange
                {
                    scaleInfos = new ScaleInfo[scaleInfos.Length]
                };

                for (int i = 0; i < scaleInfos.Length; i++)
                {
                    scaleInfos[i].obj.SetTransform(scaleInfos[i].pos, null, scaleInfos[i].scale, scaleInfos[i].part,
                        out Vector3? prevPos, out Quaternion? prevRot, out Vector3? prevScale);

                    revertable.scaleInfos[i] = new ScaleInfo(
                        scaleInfos[i].obj,
                        scaleInfos[i].part,
                        prevPos, prevScale);
                }

                return revertable;
            }

            struct ScaleInfo
            {
                public ScaleInfo(IEditableObject obj, int part, Vector3? pos, Vector3? scale)
                {
                    this.obj = obj;
                    this.part = part;
                    this.pos = pos;
                    this.scale = scale;
                }

                public readonly IEditableObject obj;
                public readonly int part;
                public readonly Vector3? pos;
                public readonly Vector3? scale;
            }
        }

        public struct TransformChangeInfo
        {
            public readonly IEditableObject obj;
            public readonly int part;
            public readonly Vector3? position;
            public readonly Quaternion? rotation;
            public readonly Vector3? scale;
            public TransformChangeInfo(IEditableObject obj, int part, Vector3? position, Quaternion? rotation, Vector3? scale)
            {
                this.obj = obj;
                this.part = part;
                this.position = position;
                this.rotation = rotation;
                this.scale = scale;
            }
        }

        public struct TransformChangeInfos
        {
            public readonly List<TransformChangeInfo> infos;
            public int changedPositions, changedRotations, changedScales, changedPosRots, changedPosScales;
            public TransformChangeInfos(List<TransformChangeInfo> infos)
            {
                this.infos = infos;

                changedPositions = 0;
                changedRotations = 0;
                changedScales = 0;
                changedPosRots = 0;
                changedPosScales = 0;
            }

            public void Add(IEditableObject obj, int part, Vector3? position, Quaternion? rotation, Vector3? scale)
            {
                if (position.HasValue)
                    changedPositions++;
                if (rotation.HasValue)
                    changedRotations++;
                if (scale.HasValue)
                    changedScales++;
                if (position.HasValue && rotation.HasValue)
                    changedPosRots++;
                if (position.HasValue && scale.HasValue)
                    changedPosScales++;

                if (position.HasValue || rotation.HasValue || scale.HasValue)
                    infos.Add(new TransformChangeInfo(obj, part, position, rotation, scale));
            }
        }

        #region List Operations
        public struct RevertableAddition : IRevertable
        {
            AddInListInfo[] infos;
            SingleAddInListInfo[] singleInfos;

            public RevertableAddition(AddInListInfo[] infos, SingleAddInListInfo[] singleInfos)
            {
                this.infos = infos;
                this.singleInfos = singleInfos;
            }

            public IRevertable Revert(EditorSceneBase scene)
            {
                uint var = 0;
                IList[] lists = new IList[infos.Length + singleInfos.Length];
                int i_lists = 0;


                //Revert Lists
                RevertableDeletion.DeleteInListInfo[] deleteInfos = new RevertableDeletion.DeleteInListInfo[infos.Length];
                int i_deleteInfos = 0;

                foreach (AddInListInfo info in infos)
                {
                    deleteInfos[i_deleteInfos] = new RevertableDeletion.DeleteInListInfo(new RevertableDeletion.DeleteInfo[info.objs.Length], info.list);
                    int i_info = 0;
                    for (int i = info.objs.Length - 1; i >= 0; i--)
                    {
                        deleteInfos[i_deleteInfos].infos[i_info++] = new RevertableDeletion.DeleteInfo(info.objs[i], info.list.IndexOf(info.objs[i]));
                        info.list.Remove(info.objs[i]);
                        (info.objs[i] as IEditableObject)?.DeselectAll(scene.control, scene.SelectedObjects);
                    }
                    lists[i_lists++] = info.list;
                    i_deleteInfos++;
                }

                //Revert Singles
                RevertableDeletion.SingleDeleteInListInfo[] deleteSingleInfos = new RevertableDeletion.SingleDeleteInListInfo[singleInfos.Length];
                i_deleteInfos = 0;

                foreach (SingleAddInListInfo info in singleInfos)
                {
                    deleteSingleInfos[i_deleteInfos++] = new RevertableDeletion.SingleDeleteInListInfo(info.obj, info.list.IndexOf(info.obj), info.list);
                    info.list.Remove(info.obj);
                    lists[i_lists++] = info.list;
                    (info.obj as IEditableObject)?.DeselectAll(scene.control, scene.SelectedObjects);
                }



                scene.UpdateSelection(var);

                scene.ListChanged?.Invoke(this, new ListChangedEventArgs(lists));

                return new RevertableDeletion(deleteInfos, deleteSingleInfos);
            }

            public struct AddInListInfo
            {
                public AddInListInfo(object[] objs, IList list)
                {
                    this.objs = objs;
                    this.list = list;
                }
                public object[] objs;
                public IList list;
            }

            public struct SingleAddInListInfo
            {
                public SingleAddInListInfo(object obj, IList list)
                {
                    this.obj = obj;
                    this.list = list;
                }
                public object obj;
                public IList list;
            }
        }

        public struct RevertableReordering : IRevertable
        {
            readonly int originalIndex;
            readonly int count;
            readonly int offset;
            IList list;

            public RevertableReordering(int originalIndex, int count, int offset, IList list)
            {
                this.originalIndex = originalIndex;
                this.count = count;
                this.offset = offset;
                this.list = list;
            }

            public IRevertable Revert(EditorSceneBase scene)
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

                scene.ListChanged?.Invoke(this, new ListChangedEventArgs(new IList[] { list }));

                return new RevertableReordering(originalIndex + offset, count, -offset, list);
            }
        }

        public struct RevertableDeletion : IRevertable
        {
            DeleteInListInfo[] infos;
            SingleDeleteInListInfo[] singleInfos;

            public RevertableDeletion(DeleteInListInfo[] infos, SingleDeleteInListInfo[] singleInfos)
            {
                this.infos = infos;
                this.singleInfos = singleInfos;
            }

            public IRevertable Revert(EditorSceneBase scene)
            {
                IList[] lists = new IList[infos.Length + singleInfos.Length];
                int i_lists = 0;


                //Revert Lists
                RevertableAddition.AddInListInfo[] addInfos = new RevertableAddition.AddInListInfo[infos.Length];
                int i_addInfos = 0;

                foreach (DeleteInListInfo info in infos)
                {
                    addInfos[i_addInfos] = new RevertableAddition.AddInListInfo(new object[info.infos.Length], info.list);
                    int i_info = 0;
                    for (int i = info.infos.Length - 1; i >= 0; i--)
                    {
                        addInfos[i_addInfos].objs[i_info++] = info.infos[i].obj;
                        info.list.Insert(info.infos[i].index, info.infos[i].obj);
                    }
                    lists[i_lists++] = info.list;
                    i_addInfos++;
                }

                //Revert Singles
                RevertableAddition.SingleAddInListInfo[] addSingleInfos = new RevertableAddition.SingleAddInListInfo[singleInfos.Length];
                i_addInfos = 0;

                foreach (SingleDeleteInListInfo info in singleInfos)
                {
                    addSingleInfos[i_addInfos++] = new RevertableAddition.SingleAddInListInfo(info.obj, info.list);
                    info.list.Insert(info.index, info.obj);
                    lists[i_lists++] = info.list;
                }



                scene.control.Refresh();

                scene.ListChanged?.Invoke(this, new ListChangedEventArgs(lists));

                return new RevertableAddition(addInfos, addSingleInfos);
            }

            public struct DeleteInfo
            {
                public DeleteInfo(object obj, int index)
                {
                    this.obj = obj;
                    this.index = index;
                }
                public object obj;
                public int index;
            }

            public struct DeleteInListInfo
            {
                public DeleteInListInfo(DeleteInfo[] infos, IList list)
                {
                    this.infos = infos;
                    this.list = list;
                }
                public DeleteInfo[] infos;
                public IList list;
            }

            public struct SingleDeleteInListInfo
            {
                public SingleDeleteInListInfo(object obj, int index, IList list)
                {
                    this.obj = obj;
                    this.index = index;
                    this.list = list;
                }
                public object obj;
                public int index;
                public IList list;
            }
        }
        #endregion

        #region Dictionary Operations
        public struct RevertableDictAddition : IRevertable
        {
            AddInDictInfo[] infos;
            SingleAddInDictInfo[] singleInfos;

            public RevertableDictAddition(AddInDictInfo[] infos, SingleAddInDictInfo[] singleInfos)
            {
                this.infos = infos;
                this.singleInfos = singleInfos;
            }

            public IRevertable Revert(EditorSceneBase scene)
            {
                uint var = 0;
                IDictionary[] dicts = new IDictionary[infos.Length + singleInfos.Length];
                int i_dicts = 0;


                //Revert Lists
                RevertableDictDeletion.DeleteInDictInfo[] deleteInfos = new RevertableDictDeletion.DeleteInDictInfo[infos.Length];
                int i_deleteInfos = 0;

                foreach (AddInDictInfo info in infos)
                {
                    deleteInfos[i_deleteInfos] = new RevertableDictDeletion.DeleteInDictInfo(new RevertableDictDeletion.DeleteInfo[info.infos.Length], info.dict);
                    int i_info = 0;
                    for (int i = info.infos.Length - 1; i >= 0; i--)
                    {
                        deleteInfos[i_deleteInfos].infos[i_info++] = new RevertableDictDeletion.DeleteInfo(info.infos[i].obj, info.infos[i].key);
                        info.dict.Remove(info.infos[i].key);
                        (info.infos[i].obj as IEditableObject)?.DeselectAll(scene.control, scene.SelectedObjects);
                    }
                    dicts[i_dicts++] = info.dict;
                    i_deleteInfos++;
                }

                //Revert Singles
                RevertableDictDeletion.SingleDeleteInDictInfo[] deleteSingleInfos = new RevertableDictDeletion.SingleDeleteInDictInfo[singleInfos.Length];
                i_deleteInfos = 0;

                foreach (SingleAddInDictInfo info in singleInfos)
                {
                    deleteSingleInfos[i_deleteInfos++] = new RevertableDictDeletion.SingleDeleteInDictInfo(info.obj, info.key, info.dict);
                    info.dict.Remove(info.key);
                    dicts[i_dicts++] = info.dict;
                    (info.obj as IEditableObject)?.DeselectAll(scene.control, scene.SelectedObjects);
                }



                scene.UpdateSelection(var);

                scene.DictChanged?.Invoke(this, new DictChangedEventArgs(dicts));

                return new RevertableDictDeletion(deleteInfos, deleteSingleInfos);
            }

            public struct AddInfo
            {
                public AddInfo(object obj, string key)
                {
                    this.obj = obj;
                    this.key = key;
                }
                public object obj;
                public string key;
            }

            public struct AddInDictInfo
            {
                public AddInDictInfo(AddInfo[] infos, IDictionary dict)
                {
                    this.infos = infos;
                    this.dict = dict;
                }
                public AddInfo[] infos;
                public IDictionary dict;
            }

            public struct SingleAddInDictInfo
            {
                public SingleAddInDictInfo(object obj, string key, IDictionary dict)
                {
                    this.obj = obj;
                    this.key = key;
                    this.dict = dict;
                }
                public object obj;
                public string key;
                public IDictionary dict;
            }
        }

        public struct RevertableDictDeletion : IRevertable
        {
            DeleteInDictInfo[] infos;
            SingleDeleteInDictInfo[] singleInfos;

            public RevertableDictDeletion(DeleteInDictInfo[] infos, SingleDeleteInDictInfo[] singleInfos)
            {
                this.infos = infos;
                this.singleInfos = singleInfos;
            }

            public IRevertable Revert(EditorSceneBase scene)
            {
                IDictionary[] dicts = new IDictionary[infos.Length + singleInfos.Length];
                int i_dicts = 0;


                //Revert Lists
                RevertableDictAddition.AddInDictInfo[] addInfos = new RevertableDictAddition.AddInDictInfo[infos.Length];
                int i_addInfos = 0;

                foreach (DeleteInDictInfo info in infos)
                {
                    addInfos[i_addInfos] = new RevertableDictAddition.AddInDictInfo(new RevertableDictAddition.AddInfo[info.infos.Length], info.dict);
                    int i_info = 0;
                    for (int i = info.infos.Length - 1; i >= 0; i--)
                    {
                        addInfos[i_addInfos].infos[i_info++] = new RevertableDictAddition.AddInfo(info.infos[i].obj, info.infos[i].key);
                        info.dict.Add(info.infos[i].key, info.infos[i].obj);
                    }
                    dicts[i_dicts++] = info.dict;
                    i_addInfos++;
                }

                //Revert Singles
                RevertableDictAddition.SingleAddInDictInfo[] addSingleInfos = new RevertableDictAddition.SingleAddInDictInfo[singleInfos.Length];
                i_addInfos = 0;

                foreach (SingleDeleteInDictInfo info in singleInfos)
                {
                    addSingleInfos[i_addInfos++] = new RevertableDictAddition.SingleAddInDictInfo(info.obj, info.key, info.dict);
                    info.dict.Add(info.key, info.obj);
                    dicts[i_dicts++] = info.dict;
                }



                scene.control.Refresh();

                scene.DictChanged?.Invoke(this, new DictChangedEventArgs(dicts));

                return new RevertableDictAddition(addInfos, addSingleInfos);
            }

            public struct DeleteInfo
            {
                public DeleteInfo(object obj, string key)
                {
                    this.obj = obj;
                    this.key = key;
                }
                public object obj;
                public string key;
            }

            public struct DeleteInDictInfo
            {
                public DeleteInDictInfo(DeleteInfo[] infos, IDictionary dict)
                {
                    this.infos = infos;
                    this.dict = dict;
                }
                public DeleteInfo[] infos;
                public IDictionary dict;
            }

            public struct SingleDeleteInDictInfo
            {
                public SingleDeleteInDictInfo(object obj, string key, IDictionary dict)
                {
                    this.obj = obj;
                    this.key = key;
                    this.dict = dict;
                }
                public object obj;
                public string key;
                public IDictionary dict;
            }
        }
        #endregion


        public struct RevertableFieldChange : IRevertable
        {
            readonly System.Reflection.FieldInfo field;
            readonly object obj;
            readonly object prevValue;
            public RevertableFieldChange(System.Reflection.FieldInfo field, object obj, object prevValue)
            {
                this.field = field;
                this.obj = obj;
                this.prevValue = prevValue;
            }

            public IRevertable Revert(EditorSceneBase scene)
            {
                object currentValue = field.GetValue(obj);
                field.SetValue(obj, prevValue);
                return new RevertableFieldChange(field, obj, currentValue);
            }
        }
    }
}
