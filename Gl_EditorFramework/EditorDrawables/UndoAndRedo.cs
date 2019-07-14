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
        public void Undo()
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push(undoStack.Pop().Revert());
                ObjectsMoved.Invoke(this,null);
            }

        }

        public void Redo()
        {
            if(redoStack.Count > 0)
            {
                undoStack.Push(redoStack.Pop().Revert());
                ObjectsMoved.Invoke(this, null);
            }
        }

        public interface IRevertable
        {
            IRevertable Revert();
        }
        
        public void AddTransformToUndo(TransformChangeInfos transformChangeInfos)
        {
            if(transformChangeInfos.changedRotations > 0)
            {
                undoStack.Push(new RevertableRotChange(transformChangeInfos));
                redoStack.Clear();
            }
            else if (transformChangeInfos.changedScales > 0)
            {
                undoStack.Push(new RevertableScaleChange(transformChangeInfos));
                redoStack.Clear();
            }
            else if (transformChangeInfos.changedPositions > 0)
            {
                undoStack.Push(new RevertablePosChange(transformChangeInfos));
                redoStack.Clear();
            }

            if(transformChangeInfos.changedPositions+transformChangeInfos.changedRotations+transformChangeInfos.changedScales>0)
                ObjectsMoved.Invoke(this, null);
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

            public IRevertable Revert()
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

            public IRevertable Revert()
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

            public IRevertable Revert()
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

        public struct RevertableAddition : IRevertable
        {
            IEditableObject[] objects;
            EditorSceneBase scene;
            IList list;

            public RevertableAddition(IEditableObject[] objects, EditorSceneBase scene, IList list)
            {
                this.objects = objects;
                this.scene = scene;
                this.list = list;
            }

            public IRevertable Revert()
            {
                uint var = 0;
                RevertableDeletion.DeleteInfo[] infos = new RevertableDeletion.DeleteInfo[objects.Length];

                bool selectionHasChanged = false;
                for (int i = 0; i < objects.Length; i++)
                {
                    infos[i] = new RevertableDeletion.DeleteInfo(objects[i], list.IndexOf(objects[i]));
                    list.Remove(objects[i]);
                    if (objects[i].IsSelected())
                        var |= objects[i].DeselectAll(scene.control);

                    selectionHasChanged |= scene.SelectedObjects.Remove(objects[i]);
                }

                if (selectionHasChanged)
                    scene.UpdateSelection(var);

                scene.ListChanged.Invoke(this, new ListChangedEventArgs(list));

                return new RevertableDeletion(infos, scene, list);
            }
        }

        public struct RevertableReordering : IRevertable
        {
            int originalIndex;
            int count;
            int offset;
            EditorSceneBase scene;
            IList list;

            public RevertableReordering(int originalIndex, int count, int offset, EditorSceneBase scene, IList list)
            {
                this.originalIndex = originalIndex;
                this.count = count;
                this.offset = offset;
                this.scene = scene;
                this.list = list;
            }

            public IRevertable Revert()
            {
                List<object> objs = new List<object>();

                for (int i = 0; i < count; i++)
                {
                    objs.Add(list[originalIndex]);
                    list.RemoveAt(originalIndex);
                }

                int index = originalIndex + offset;
                foreach (IEditableObject obj in objs)
                {
                    list.Insert(index, obj);
                    index++;
                }

                scene.ListChanged.Invoke(this, null);

                return new RevertableReordering(originalIndex + offset, count, -offset, scene, list);
            }
        }

        public struct RevertableDeletion : IRevertable
        {
            DeleteInfo[] infos;
            EditorSceneBase scene;
            IList list;

            public RevertableDeletion(DeleteInfo[] infos, EditorSceneBase scene, IList list)
            {
                this.infos = infos;
                this.scene = scene;
                this.list = list;
            }

            public IRevertable Revert()
            {
                for (int i = infos.Length - 1; i >= 0; i--)
                    list.Insert(infos[i].index, infos[i].obj);

                IEditableObject[] objects = new IEditableObject[infos.Length];

                for (int i = 0; i < infos.Length; i++)
                    objects[i] = infos[i].obj;

                scene.control.Refresh();

                scene.ListChanged.Invoke(this, new ListChangedEventArgs(list));

                return new RevertableAddition(objects, scene, list);
            }

            public struct DeleteInfo
            {
                public DeleteInfo(IEditableObject obj, int index)
                {
                    this.obj = obj;
                    this.index = index;
                }
                public IEditableObject obj;
                public int index;
            }
        }
    }
}
