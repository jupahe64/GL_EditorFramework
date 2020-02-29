using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinInput = System.Windows.Input;
using static GL_EditorFramework.EditorDrawables.EditableObject;
using System.Drawing;
using OpenTK;

namespace GL_EditorFramework.EditorDrawables
{
    public abstract partial class EditorSceneBase : AbstractGlDrawable
    {
        public void StartTransformAction(LocalOrientation localOrientation, DragActionType dragActionType, int part = -1, IRevertable revertable = null)
        {
            AbstractTransformAction transformAction;

            Vector3 pivot;

            draggingDepth = control.PickingDepth;
            if (part!=-1)
                pivot = localOrientation.Origin;
            else
            {
                BoundingBox box = BoundingBox.Default;

                foreach (IEditableObject obj in GetObjects())
                    obj.GetSelectionBox(ref box);

                pivot = box.GetCenter();

                if (box == BoundingBox.Default)
                    return;
            }

            switch (dragActionType)
            {
                case DragActionType.TRANSLATE:
                    transformAction = new TranslateAction(control, control.GetMousePos(), pivot, draggingDepth);
                    break;
                case DragActionType.ROTATE:
                    transformAction = new RotateAction(control, control.GetMousePos(), pivot, draggingDepth);
                    break;
                case DragActionType.SCALE:
                    transformAction = new ScaleAction(control, control.GetMousePos(), pivot);
                    break;
                case DragActionType.SCALE_INDIVIDUAL:
                    transformAction = new ScaleActionIndividual(control, control.GetMousePos(), localOrientation);
                    break;

                default:
                    return;
            }

            StartTransformAction(transformAction, part, revertable);
        }

        public void StartTransformAction(AbstractTransformAction transformAction, int part = -1, IRevertable revertable = null)
        {
            if (revertable != null)
            {
                BeginUndoCollection();
                AddToUndo(revertable);
            }

            if (part != -1)
            {
                HoveredPart = part;
                ExclusiveAction = transformAction;
            }
            else
                CurrentAction = transformAction;
        }

        public override uint MouseDown(MouseEventArgs e, GL_ControlBase control)
        {
            bool TryGetActionType(out DragActionType dragActionType)
            {
                if (e.Button == MouseButtons.Left)
                {
                    dragActionType = DragActionType.TRANSLATE;
                    return true;
                }
                else if (e.Button == MouseButtons.Right)
                {
                    dragActionType = DragActionType.ROTATE;
                    return true;
                }
                else if (e.Button == MouseButtons.Middle)
                {
                    bool ctrl = WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftCtrl);

                    dragActionType = ctrl ? DragActionType.SCALE_INDIVIDUAL : DragActionType.SCALE;
                    return true;
                }
                dragActionType = DragActionType.NONE;
                return false;
            }

            uint var = 0;
            {
                if (CurrentAction == NoAction && ExclusiveAction == NoAction && Hovered != null && TryGetActionType(out DragActionType dragActionType))
                {
                    Hovered.StartDragging(dragActionType, HoveredPart, this);
                }
                else
                {
                    var |= REDRAW_PICKING;
                    var |= FORCE_REENTER;

                    CurrentAction = NoAction; //abort current action
                    ExclusiveAction = NoAction;
                }
            }

            foreach (AbstractGlDrawable obj in StaticObjects)
            {
                var |= obj.MouseDown(e, control);
            }

            foreach (IEditableObject obj in GetObjects())
            {
                var |= obj.MouseDown(e, control);
            }

            return var;
        }

        public override uint MouseMove(MouseEventArgs e, Point lastMousePos, GL_ControlBase control)
        {
            uint var = 0;

            foreach (IEditableObject obj in GetObjects())
            {
                var |= obj.MouseMove(e, lastMousePos, control);
            }

            foreach (AbstractGlDrawable obj in StaticObjects)
            {
                var |= obj.MouseMove(e, lastMousePos, control);
            }

            if (CurrentAction != NoAction || ExclusiveAction != NoAction)
            {
                CurrentAction.UpdateMousePos(e.Location);
                ExclusiveAction.UpdateMousePos(e.Location);

                var |= REDRAW | NO_CAMERA_ACTION;

                var &= ~REPICK;
            }
            else
            {
                var |= REPICK;
            }
            return var;
        }

        public override uint MouseWheel(MouseEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            foreach (IEditableObject obj in GetObjects())
            {
                var |= obj.MouseWheel(e, control);
            }

            foreach (AbstractGlDrawable obj in StaticObjects)
            {
                var |= obj.MouseWheel(e, control);
            }

            if (CurrentAction != NoAction || ExclusiveAction != NoAction)
            {
                CurrentAction.ApplyScrolling(e.Location, e.Delta);
                ExclusiveAction.ApplyScrolling(e.Location, e.Delta);

                var |= REDRAW | NO_CAMERA_ACTION;

                var &= ~REPICK;
            }

            return var;
        }

        public override uint MouseUp(MouseEventArgs e, GL_ControlBase control)
        {
            TransformChangeInfos transformChangeInfos = new TransformChangeInfos(new List<TransformChangeInfo>());
            uint var = 0;

            if (CurrentAction != NoAction && CurrentAction.IsApplyOnRelease())
            {
                foreach (IEditableObject obj in GetObjects())
                {
                    obj.ApplyTransformActionToSelection(CurrentAction, ref transformChangeInfos);
                }

                var |= REDRAW_PICKING | FORCE_REENTER;

                AddTransformToUndo(transformChangeInfos);
                EndUndoCollection();

                CurrentAction = NoAction;
            }

            if (ExclusiveAction != NoAction)
            {
                Hovered.ApplyTransformActionToPart(ExclusiveAction, HoveredPart, ref transformChangeInfos);

                var |= REDRAW_PICKING;

                AddTransformToUndo(transformChangeInfos);
                EndUndoCollection();

                ExclusiveAction = NoAction;
            }

            foreach (AbstractGlDrawable obj in StaticObjects)
            {
                var |= obj.MouseUp(e, control);
            }

            foreach (IEditableObject obj in GetObjects())
            {
                var |= obj.MouseUp(e, control);
            }

            return var;
        }

        public override uint MouseClick(MouseEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            foreach (IEditableObject obj in GetObjects())
            {
                var |= obj.MouseClick(e, control);
            }

            bool shift = WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift);
            bool hoveredIsSelected = Hovered != null && Hovered.IsSelected(HoveredPart);
            bool nothingHovered = Hovered == null;

            /*
            * Selecting Objects:
            * 
            * If the object is not selected, select it
            * If another object is selected, deselect the old object, and select the new one
            * UNLESS [SHIFT] is being pressed
            * If [SHIFT] is being pressed, select the new object, but DON'T deselect the old object
            * 
            * If an object that is already selected is clicked, deselect it
            * If multiple objects are selected, deselect all of them
            * UNLESS [SHIFT] is being pressed
            * If [SHIFt] is being pressed, only deselect the one object, leave the rest alone
            * 
            * If nothing is being selected, and [SHIFT] isn't pressed, de-select everything
            */
            if (e.Button == MouseButtons.Left)
            {
                if (nothingHovered)
                {
                    if (!shift)
                    {
                        foreach (IEditableObject obj in GetObjects())
                        {
                            obj.DeselectAll(control);
                        }
                        SelectionChanged?.Invoke(this, new EventArgs());
                    }
                }
                else if (multiSelect)
                {
                    if (hoveredIsSelected)
                    {
                        if (shift)
                        {
                            //remove from selection
                            Hovered.Deselect(HoveredPart, control);
                            SelectionChanged?.Invoke(this, new EventArgs());
                        }
                        else
                        {
                            foreach (IEditableObject obj in GetObjects())
                            {
                                obj.DeselectAll(control);
                            }
                            SelectionChanged?.Invoke(this, new EventArgs());
                        }
                    }
                    else
                    {
                        if (shift)
                        {
                            //add to selection
                            Hovered.Select(HoveredPart, control);
                            SelectionChanged?.Invoke(this, new EventArgs());
                        }
                        else
                        {
                            //change selection
                            foreach (IEditableObject obj in GetObjects())
                            {
                                obj.DeselectAll(control);
                            }
                            Hovered.Select(HoveredPart, control);
                            SelectionChanged?.Invoke(this, new EventArgs());
                        }
                    }
                }
                else
                {
                    if (hoveredIsSelected)
                    {
                        //remove from selection
                        Hovered.Deselect(HoveredPart, control);
                        SelectionChanged?.Invoke(this, new EventArgs());
                    }
                    else
                    {
                        //change selection
                        foreach (IEditableObject obj in GetObjects())
                        {
                            obj.DeselectAll(control);
                        }
                        Hovered.Select(HoveredPart, control);
                        SelectionChanged?.Invoke(this, new EventArgs());
                    }
                }
            }

            foreach (AbstractGlDrawable obj in StaticObjects)
            {
                var |= obj.MouseClick(e, control);
            }

            var |= REDRAW;
            var |= FORCE_REENTER;

            return var;
        }

        public override uint MouseEnter(int inObjectIndex, GL_ControlBase control)
        {
            if (CurrentAction != NoAction || ExclusiveAction != NoAction)
                return 0;

            ObjectRenderState.ShouldBeDrawn = ShouldBeDrawn;

            foreach (IEditableObject obj in GetObjects())
            {
                int span = obj.GetPickableSpan();
                if (inObjectIndex >= 0 && inObjectIndex < span)
                {
                    Hovered = obj;
                    HoveredPart = inObjectIndex;
                    return obj.MouseEnter(inObjectIndex, control) | REDRAW;
                }
                inObjectIndex -= span;
            }

            foreach (AbstractGlDrawable obj in StaticObjects)
            {
                int span = obj.GetPickableSpan();
                if (inObjectIndex >= 0 && inObjectIndex < span)
                {
                    Hovered = null;
                    HoveredPart = -1;
                    return obj.MouseEnter(inObjectIndex, control);
                }
                inObjectIndex -= span;
            }

            ObjectRenderState.ShouldBeDrawn = ObjectRenderState.ShouldBeDrawn_Default;

            return 0;
        }

        public override uint MouseLeave(int inObjectIndex, GL_ControlBase control)
        {
            if (CurrentAction != NoAction || ExclusiveAction != NoAction)
                return 0;

            ObjectRenderState.ShouldBeDrawn = ShouldBeDrawn;

            foreach (IEditableObject obj in GetObjects())
            {
                int span = obj.GetPickableSpan();
                if (inObjectIndex >= 0 && inObjectIndex < span)
                {
                    return obj.MouseLeave(inObjectIndex, control);
                }
                inObjectIndex -= span;
            }

            foreach (AbstractGlDrawable obj in StaticObjects)
            {
                int span = obj.GetPickableSpan();
                if (inObjectIndex >= 0 && inObjectIndex < span)
                {
                    return obj.MouseLeave(inObjectIndex, control);
                }
                inObjectIndex -= span;
            }

            ObjectRenderState.ShouldBeDrawn = ObjectRenderState.ShouldBeDrawn_Default;

            return 0;
        }

        public override uint MouseLeaveEntirely(GL_ControlBase control)
        {
            if (CurrentAction != NoAction || ExclusiveAction != NoAction)
                return 0;

            Hovered = null;
            return REDRAW;
        }

        public override uint KeyDown(KeyEventArgs e, GL_ControlBase control)
        {
            TransformChangeInfos transformChangeInfos = new TransformChangeInfos(new List<TransformChangeInfo>());
            uint var = 0;

            bool selectionHasChanged = false;

            if ((CurrentAction != NoAction || ExclusiveAction != NoAction) && e.KeyCode != Keys.V)
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
                else
                {
                    BoundingBox box = BoundingBox.Default;

                    foreach (IEditableObject obj in GetObjects())
                    {
                        obj.GetSelectionBox(ref box);
                    }

                    if(box!=BoundingBox.Default)
                        control.CameraTarget = box.GetCenter();
                }

                var = REDRAW_PICKING;
            }
            else if (e.KeyCode == Keys.Y && e.Control)
            {
                Redo();

                var = REDRAW_PICKING;
            }
            else if (e.KeyCode == Keys.H) //hide/show selected objects
            {
                foreach (IEditableObject obj in GetObjects())
                {
                    if(obj.IsSelected())
                        obj.Visible = e.Shift;
                }
                var = REDRAW_PICKING;
            }
            else if (e.KeyCode == Keys.S && e.Shift) //auto snap selected objects
            {
                SnapAction action = new SnapAction();
                foreach (IEditableObject obj in GetObjects())
                {
                    obj.ApplyTransformActionToSelection(action, ref transformChangeInfos);
                }
                var = REDRAW_PICKING;
            }
            else if (e.KeyCode == Keys.R && e.Shift && e.Control) //reset scale for selected objects
            {
                ResetScale action = new ResetScale();
                foreach (IEditableObject obj in GetObjects())
                {
                    obj.ApplyTransformActionToSelection(action, ref transformChangeInfos);
                }
                var = REDRAW_PICKING;
            }
            else if (e.KeyCode == Keys.R && e.Shift) //reset rotation for selected objects
            {
                ResetRot action = new ResetRot();
                foreach (IEditableObject obj in GetObjects())
                {
                    obj.ApplyTransformActionToSelection(action, ref transformChangeInfos);
                }
                var = REDRAW_PICKING;
            }
            else if (e.Control && e.KeyCode == Keys.A) //select/deselect all objects
            {
                if (e.Shift)
                {
                    foreach (IEditableObject obj in GetObjects())
                    {
                        obj.DeselectAll(control);
                    }
                    selectionHasChanged = true;
                }

                if (!e.Shift && multiSelect)
                {
                    foreach (IEditableObject obj in GetObjects())
                    {
                        obj.SelectAll(control);
                    }
                    selectionHasChanged = true;
                }
                var = REDRAW;
            }

            foreach (IEditableObject obj in GetObjects())
            {
                var |= obj.KeyDown(e, control);
            }
            foreach (AbstractGlDrawable obj in StaticObjects)
            {
                var |= obj.KeyDown(e, control);
            }
            if (selectionHasChanged)
                UpdateSelection(var);

            AddTransformToUndo(transformChangeInfos);

            return var;
        }

        public override uint KeyUp(KeyEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            foreach (IEditableObject obj in GetObjects())
            {
                var |= obj.KeyUp(e, control);
            }
            foreach (AbstractGlDrawable obj in StaticObjects)
            {
                var |= obj.KeyUp(e, control);
            }
            return var;
        }
    }
}
