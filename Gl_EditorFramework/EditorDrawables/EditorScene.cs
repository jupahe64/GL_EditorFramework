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
        public List<IEditableObject> objects = new List<IEditableObject>();

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
        
        public void Add(params IEditableObject[] objs)
        {
            uint var = 0;

            foreach (IEditableObject selected in SelectedObjects)
            {
                var |= selected.DeselectAll(control);
            }
            SelectedObjects.Clear();

            foreach (IEditableObject obj in objs)
            {
                objects.Add(obj);

                SelectedObjects.Add(obj);
                var |= obj.SelectDefault(control);
            }

            undoStack.Push(new RevertableAddition(objs, this, objects));

            UpdateSelection(var);
        }

        public void Delete(params IEditableObject[] objs)
        {
            uint var = 0;

            List<RevertableDeletion.DeleteInfo> infos = new List<RevertableDeletion.DeleteInfo>();

            bool selectionHasChanged = false;

            foreach (IEditableObject obj in objs)
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

            undoStack.Push(new RevertableDeletion(infos.ToArray(), this, objects));

            if (selectionHasChanged)
                UpdateSelection(var);
        }

        public void Delete(IEnumerable<object> objs)
        {
            uint var = 0;

            List<RevertableDeletion.DeleteInfo> infos = new List<RevertableDeletion.DeleteInfo>();

            bool selectionHasChanged = false;

            foreach (IEditableObject obj in objs)
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

            undoStack.Push(new RevertableDeletion(infos.ToArray(), this, objects));

            if (selectionHasChanged)
                UpdateSelection(var);
        }

        public void InsertAt(int index, params IEditableObject[] objs)
        {
            uint var = 0;

            foreach (IEditableObject selected in SelectedObjects)
            {
                var |= selected.DeselectAll(control);
            }
            SelectedObjects.Clear();

            foreach (IEditableObject obj in objs)
            {
                objects.Insert(index, obj);

                SelectedObjects.Add(obj);
                var |= obj.SelectDefault(control);
                index++;
            }

            undoStack.Push(new RevertableAddition(objs, this, objects));

            UpdateSelection(var);
        }

        public void MoveObjectsInList(int originalIndex, int count, int offset)
        {
            List<IEditableObject> objs = new List<IEditableObject>();

            for (int i = 0; i < count; i++)
            {
                objs.Add(objects[originalIndex]);
                objects.RemoveAt(originalIndex);
            }

            int index = originalIndex + offset;
            foreach (IEditableObject obj in objs)
            {
                objects.Insert(index, obj);
                index++;
            }

            undoStack.Push(new RevertableMovement(originalIndex + offset, count, -offset, this, objects));
        }

        public override void Draw(GL_ControlModern control, Pass pass)
        {
            foreach (IEditableObject obj in objects)
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
            foreach (IEditableObject obj in objects)
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
            foreach (IEditableObject obj in objects)
                obj.Prepare(control);
            foreach (AbstractGlDrawable obj in staticObjects)
                obj.Prepare(control);
        }

        public override void Prepare(GL_ControlLegacy control)
        {
            this.control = control;
            foreach (IEditableObject obj in objects)
                obj.Prepare(control);
            foreach (AbstractGlDrawable obj in staticObjects)
                obj.Prepare(control);
        }

        public override uint MouseDown(MouseEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            var |= base.MouseDown(e, control);
            foreach (IEditableObject obj in objects)
            {
                var |= obj.MouseDown(e, control);
            }
            return var;
        }

        public override uint MouseMove(MouseEventArgs e, Point lastMousePos, GL_ControlBase control)
        {
            uint var = 0;

            foreach (IEditableObject obj in objects)
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
            foreach (IEditableObject obj in objects)
            {
                var |= obj.MouseUp(e, control);
            }
            return var;
        }

        public override uint MouseClick(MouseEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            foreach (IEditableObject obj in objects)
            {
                var |= obj.MouseClick(e, control);
            }

            var |= base.MouseClick(e, control);

            return var;
        }

        public override uint MouseWheel(MouseEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            foreach (IEditableObject obj in objects) {
                var |= obj.MouseWheel(e, control);
            }

            var |= base.MouseWheel(e, control);

            return var;
        }

        public override int GetPickableSpan()
        {
            int var = 0;
            foreach (IEditableObject obj in objects)
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
            foreach (IEditableObject obj in objects)
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
            foreach (IEditableObject obj in objects)
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

                    foreach (IEditableObject selected in SelectedObjects)
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
                foreach (IEditableObject selected in SelectedObjects)
                {
                    selected.Visible = e.Shift;
                }
                var = REDRAW_PICKING;
            }
            else if(e.KeyCode == Keys.S && SelectedObjects.Count > 0 && e.Shift) //auto snap selected objects
            {
                SnapAction action = new SnapAction();
                foreach (IEditableObject selected in SelectedObjects)
                {
                    selected.ApplyTransformActionToSelection(action, ref transformChangeInfos);
                }
                var = REDRAW_PICKING;
            }
            else if (e.KeyCode == Keys.R && SelectedObjects.Count > 0 && e.Shift && e.Control) //reset scale for selected objects
            {
                ResetScale action = new ResetScale();
                foreach (IEditableObject selected in SelectedObjects)
                {
                    selected.ApplyTransformActionToSelection(action, ref transformChangeInfos);
                }
                var = REDRAW_PICKING;
            }
            else if (e.KeyCode == Keys.R && SelectedObjects.Count > 0 && e.Shift) //reset rotation for selected objects
            {
                ResetRot action = new ResetRot();
                foreach (IEditableObject selected in SelectedObjects)
                {
                    selected.ApplyTransformActionToSelection(action, ref transformChangeInfos);
                }
                var = REDRAW_PICKING;
            }
            else if (e.Control && e.KeyCode == Keys.A) //select/deselect all objects
            {
                if (e.Shift && SelectedObjects.Count > 0)
                {
                    foreach (IEditableObject selected in SelectedObjects)
                    {
                        selected.DeselectAll(control);
                    }
                    SelectedObjects.Clear();
                    selectionHasChanged = true;
                }

                if (!e.Shift && multiSelect)
                {
                    foreach (IEditableObject obj in objects)
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

            foreach (IEditableObject obj in objects) {
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
            foreach (IEditableObject obj in objects) {
                var |= obj.KeyUp(e, control);
            }
            foreach (AbstractGlDrawable obj in staticObjects)
            {
                var |= obj.KeyUp(e, control);
            }
            return var;
        }
    }
}
