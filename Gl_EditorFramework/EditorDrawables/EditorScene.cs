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

namespace GL_EditorFramework.EditorDrawables
{
    public class EditorScene : EditorSceneBase
    {
        public event EventHandler ListChanged;
        public List<EditableObject> objects = new List<EditableObject>();

        private float renderDistanceSquared = 1000000;
        private float renderDistance = 1000;

        public float RenderDistance{
            get => renderDistanceSquared;
            set
            {
                if (value < 1f)
                {
                    renderDistanceSquared = 1f;
                    renderDistance = 1f;
                }
                else
                {
                    renderDistanceSquared = value * value;
                    renderDistance = value;
                }
            }
        }

        public EditorScene(bool multiSelect = true)
        {
            this.multiSelect = multiSelect;
        }
        
        public void Add(params EditableObject[] objs)
        {
            uint var = 0;

            foreach (EditableObject selected in SelectedObjects)
            {
                var |= selected.DeselectAll(control);
            }
            SelectedObjects.Clear();

            foreach (EditableObject obj in objs)
            {
                objects.Add(obj);

                SelectedObjects.Add(obj);
                var |= obj.SelectDefault(control);
            }

            undoStack.Push(new RevertableAddition(objs, this));

            UpdateSelection(var);
        }

        public void Delete(params EditableObject[] objs)
        {
            uint var = 0;

            List<RevertableDeletion.DeleteInfo> infos = new List<RevertableDeletion.DeleteInfo>();

            bool selectionHasChanged = false;

            foreach (EditableObject obj in objs)
            {
                infos.Add(new RevertableDeletion.DeleteInfo(obj, objects.IndexOf(obj)));
                objects.Remove(obj);
                if (SelectedObjects.Contains(obj))
                {
                    var |= obj.DeselectAll(control);
                    SelectedObjects.Remove(obj);
                    selectionHasChanged = true;
                }
            }

            undoStack.Push(new RevertableDeletion(infos.ToArray(), this));

            if (selectionHasChanged)
                UpdateSelection(var);
        }

        public void InsertAt(int index, params EditableObject[] objs)
        {
            uint var = 0;

            foreach (EditableObject selected in SelectedObjects)
            {
                var |= selected.DeselectAll(control);
            }
            SelectedObjects.Clear();

            foreach (EditableObject obj in objs)
            {
                objects.Insert(index, obj);

                SelectedObjects.Add(obj);
                var |= obj.SelectDefault(control);
                index++;
            }

            undoStack.Push(new RevertableAddition(objs, this));

            UpdateSelection(var);
        }

        public void MoveObjectsInList(int originalIndex, int count, int offset)
        {
            List<EditableObject> objs = new List<EditableObject>();

            for (int i = 0; i < count; i++)
            {
                objs.Add(objects[originalIndex]);
                objects.RemoveAt(originalIndex);
            }

            int index = originalIndex + offset;
            foreach (EditableObject obj in objs)
            {
                objects.Insert(index, obj);
                index++;
            }

            undoStack.Push(new RevertableMovement(originalIndex + offset, count, -offset, this));
        }

        public override void Draw(GL_ControlModern control, Pass pass)
        {
            foreach (EditableObject obj in objects)
            {
                if(obj.Visible && obj.IsInRange(renderDistance, renderDistanceSquared, control.CameraPosition))
                    obj.Draw(control, pass, this);
            }
            foreach (AbstractGlDrawable obj in staticObjects)
            {
                obj.Draw(control, pass);
            }
            if (pass == Pass.OPAQUE)
            {
                CurrentAction.Draw(control);
                ExclusiveAction.Draw(control);
            }
        }

        public override void Draw(GL_ControlLegacy control, Pass pass)
        {
            foreach (EditableObject obj in objects)
            {
                if (obj.Visible && obj.IsInRange(renderDistance, renderDistanceSquared, control.CameraPosition))
                    obj.Draw(control, pass, this);
            }
            foreach (AbstractGlDrawable obj in staticObjects)
            {
                obj.Draw(control, pass);
            }
            if (pass == Pass.OPAQUE)
            {
                CurrentAction.Draw(control);
                ExclusiveAction.Draw(control);
            }
        }

        public override void Prepare(GL_ControlModern control)
        {
            this.control = control;
            foreach (EditableObject obj in objects)
                obj.Prepare(control);
            foreach (AbstractGlDrawable obj in staticObjects)
                obj.Prepare(control);
        }

        public override void Prepare(GL_ControlLegacy control)
        {
            this.control = control;
            foreach (EditableObject obj in objects)
                obj.Prepare(control);
            foreach (AbstractGlDrawable obj in staticObjects)
                obj.Prepare(control);
        }

        public override uint MouseDown(MouseEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            var |= base.MouseDown(e, control);
            foreach (EditableObject obj in objects)
            {
                var |= obj.MouseDown(e, control);
            }
            return var;
        }

        public override uint MouseMove(MouseEventArgs e, Point lastMousePos, GL_ControlBase control)
        {
            uint var = 0;

            foreach (EditableObject obj in objects)
            {
                var |= obj.MouseMove(e, lastMousePos, control);
            }
            var |= base.MouseMove(e, lastMousePos, control);
            return var;
        }

        public override uint MouseUp(MouseEventArgs e, GL_ControlBase control)
        {
            uint var = 0;

            var |= base.MouseUp(e, control);
            foreach (EditableObject obj in objects)
            {
                var |= obj.MouseUp(e, control);
            }
            return var;
        }

        public override uint MouseClick(MouseEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            foreach (EditableObject obj in objects)
            {
                var |= obj.MouseClick(e, control);
            }

            var |= base.MouseClick(e, control);

            return var;
        }

        public override uint MouseWheel(MouseEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            foreach (EditableObject obj in objects) {
                var |= obj.MouseWheel(e, control);
            }

            var |= base.MouseWheel(e, control);

            return var;
        }

        public override int GetPickableSpan()
        {
            int var = 0;
            foreach (EditableObject obj in objects)
                if (obj.Visible && obj.IsInRange(renderDistance, renderDistanceSquared, control.CameraPosition))
                    var += obj.GetPickableSpan();
            foreach (AbstractGlDrawable obj in staticObjects)
                var += obj.GetPickableSpan();
            return var;
        }

        public override uint MouseEnter(int inObjectIndex, GL_ControlBase control)
        {
            if (CurrentAction != NoAction || ExclusiveAction != NoAction)
                return 0;
            foreach (EditableObject obj in objects)
            {
                if (!(obj.Visible && obj.IsInRange(renderDistance, renderDistanceSquared, control.CameraPosition)))
                    continue;
                int span = obj.GetPickableSpan();
                if (inObjectIndex >= 0 && inObjectIndex < span)
                {
                    Hovered = obj;
                    HoveredPart = inObjectIndex;
                    return obj.MouseEnter(inObjectIndex, control) | REDRAW;
                }
                inObjectIndex -= span;
            }

            base.MouseEnter(inObjectIndex, control); 
            return 0;
        }

        public override uint MouseLeave(int inObjectIndex, GL_ControlBase control)
        {
            if (CurrentAction != NoAction || ExclusiveAction != NoAction)
                return 0;
            foreach (EditableObject obj in objects)
            {
                if (!(obj.Visible && obj.IsInRange(renderDistance, renderDistanceSquared, control.CameraPosition)))
                    continue;
                int span = obj.GetPickableSpan();
                if (inObjectIndex >= 0 && inObjectIndex < span)
                {
                    return obj.MouseLeave(inObjectIndex, control);
                }
                inObjectIndex -= span;
            }

            base.MouseLeave(inObjectIndex, control);
            return 0;
        }

        public override uint KeyDown(KeyEventArgs e, GL_ControlBase control)
        {
            TransformChangeInfos transformChangeInfos = new TransformChangeInfos(new List<TransformChangeInfo>());
            uint var = 0;

            bool selectionHasChanged = false;

            if (CurrentAction != NoAction || ExclusiveAction != NoAction)
            {
                CurrentAction.KeyDown(e);
                ExclusiveAction.KeyDown(e);
                var = NO_CAMERA_ACTION | REDRAW;
            }
            else if (e.KeyCode == Keys.Z) //focus camera on the selection
            {
                if (e.Control)
                {
                    Undo();
                }
                else if (SelectedObjects.Count > 0)
                {
                    EditableObject.BoundingBox box = EditableObject.BoundingBox.Default;

                    foreach (EditableObject selected in SelectedObjects)
                    {
                        box.Include(selected.GetSelectionBox());
                    }
                    control.CameraTarget = box.GetCenter();
                }

                var = REDRAW_PICKING;
            }
            else if (e.KeyCode == Keys.Y && e.Control)
            {
                Redo();

                var = REDRAW_PICKING;
            }
            else if (e.KeyCode == Keys.H && SelectedObjects.Count > 0) //hide/show selected objects
            {
                foreach (EditableObject selected in SelectedObjects)
                {
                    selected.Visible = e.Shift;
                }
                var = REDRAW_PICKING;
            }
            else if(e.KeyCode == Keys.S && SelectedObjects.Count > 0 && e.Shift) //auto snap selected objects
            {
                SnapAction action = new SnapAction();
                foreach (EditableObject selected in SelectedObjects)
                {
                    selected.ApplyTransformActionToSelection(action, ref transformChangeInfos);
                }
                var = REDRAW_PICKING;
            }
            else if (e.KeyCode == Keys.R && SelectedObjects.Count > 0 && e.Shift && e.Control) //reset scale for selected objects
            {
                ResetScale action = new ResetScale();
                foreach (EditableObject selected in SelectedObjects)
                {
                    selected.ApplyTransformActionToSelection(action, ref transformChangeInfos);
                }
                var = REDRAW_PICKING;
            }
            else if (e.KeyCode == Keys.R && SelectedObjects.Count > 0 && e.Shift) //reset rotation for selected objects
            {
                ResetRot action = new ResetRot();
                foreach (EditableObject selected in SelectedObjects)
                {
                    selected.ApplyTransformActionToSelection(action, ref transformChangeInfos);
                }
                var = REDRAW_PICKING;
            }
            else if (e.Control && e.KeyCode == Keys.A) //select/deselect all objects
            {
                if (e.Shift && SelectedObjects.Count > 0)
                {
                    foreach (EditableObject selected in SelectedObjects)
                    {
                        selected.DeselectAll(control);
                    }
                    SelectedObjects.Clear();
                    selectionHasChanged = true;
                }

                if (!e.Shift && multiSelect)
                {
                    foreach (EditableObject obj in objects)
                    {
                        if (!obj.IsSelected())
                        {
                            obj.SelectAll(control);
                            SelectedObjects.Add(obj);
                        }
                    }
                    selectionHasChanged = true;
                }
                var = REDRAW;
            }

            foreach (EditableObject obj in objects) {
                var |= obj.KeyDown(e, control);
            }
            foreach (AbstractGlDrawable obj in staticObjects)
            {
                var |= obj.KeyDown(e, control);
            }
            if(selectionHasChanged)
                UpdateSelection(var);

            AddTransformToUndo(transformChangeInfos);

            return var;
        }

        public override uint KeyUp(KeyEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            foreach (EditableObject obj in objects) {
                var |= obj.KeyUp(e, control);
            }
            foreach (AbstractGlDrawable obj in staticObjects)
            {
                var |= obj.KeyUp(e, control);
            }
            return var;
        }



        public struct RevertableAddition : IRevertable
        {
            EditableObject[] objects;
            EditorScene scene;

            public RevertableAddition(EditableObject[] objects, EditorScene scene)
            {
                this.objects = objects;
                this.scene = scene;
            }

            public IRevertable Revert()
            {
                uint var = 0;
                RevertableDeletion.DeleteInfo[] infos = new RevertableDeletion.DeleteInfo[objects.Length];

                bool selectionHasChanged = false;
                for (int i = 0; i < objects.Length; i++)
                {
                    infos[i] = new RevertableDeletion.DeleteInfo(objects[i], scene.objects.IndexOf(objects[i]));
                    scene.objects.Remove(objects[i]);
                    if(objects[i].IsSelected())
                        var |= objects[i].DeselectAll(scene.control);

                    selectionHasChanged |= scene.SelectedObjects.Remove(objects[i]);
                }

                if(selectionHasChanged)
                    scene.UpdateSelection(var);

                scene.ListChanged.Invoke(this, null);

                return new RevertableDeletion(infos, scene);
            }
        }

        public struct RevertableMovement : IRevertable
        {
            int originalIndex;
            int count;
            int offset;
            EditorScene scene;

            public RevertableMovement(int originalIndex, int count, int offset, EditorScene scene)
            {
                this.originalIndex = originalIndex;
                this.count = count;
                this.offset = offset;
                this.scene = scene;
            }

            public IRevertable Revert()
            {
                List<EditableObject> objs = new List<EditableObject>();

                for (int i = 0; i < count; i++)
                {
                    objs.Add(scene.objects[originalIndex]);
                    scene.objects.RemoveAt(originalIndex);
                }

                int index = originalIndex + offset;
                foreach (EditableObject obj in objs)
                {
                    scene.objects.Insert(index, obj);
                    index++;
                }

                scene.ListChanged.Invoke(this, null);

                return new RevertableMovement(originalIndex+offset, count, -offset, scene);
            }
        }

        public struct RevertableDeletion : IRevertable
        {
            DeleteInfo[] infos;
            EditorScene scene;

            public RevertableDeletion(DeleteInfo[] infos, EditorScene scene)
            {
                this.infos = infos;
                this.scene = scene;
            }

            public IRevertable Revert()
            {
                for (int i = infos.Length - 1; i >= 0; i--)
                    scene.objects.Insert(infos[i].index, infos[i].obj);

                EditableObject[] objects = new EditableObject[infos.Length];

                for (int i = 0; i < infos.Length; i++)
                    objects[i] = infos[i].obj;

                scene.control.Refresh();

                scene.ListChanged.Invoke(this, null);

                return new RevertableAddition(objects, scene);
            }

            public struct DeleteInfo
            {
                public DeleteInfo(EditableObject obj, int index)
                {
                    this.obj = obj;
                    this.index = index;
                }
                public EditableObject obj;
                public int index;
            }
        }
    }
}
