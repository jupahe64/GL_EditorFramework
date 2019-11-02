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

namespace GL_EditorFramework.EditorDrawables
{
    public abstract partial class EditorSceneBase : AbstractGlDrawable
    {
        public override uint MouseDown(MouseEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            if (CurrentAction == NoAction && ExclusiveAction == NoAction && Hovered != null)
            {
                LocalOrientation rLocalOrientation;
                bool rDragExclusively;
                if (e.Button == MouseButtons.Left)
                {
                    if (Hovered.TryStartDragging(DragActionType.TRANSLATE, HoveredPart, out rLocalOrientation, out rDragExclusively))
                    {
                        draggingDepth = control.PickingDepth;
                        if (rDragExclusively)
                            ExclusiveAction = new TranslateAction(control, e.Location, rLocalOrientation.Origin, draggingDepth);
                        else
                        {
                            BoundingBox box = BoundingBox.Default;

                            foreach (IEditableObject obj in GetObjects())
                                obj.GetSelectionBox(ref box);

                            if(box!=BoundingBox.Default)
                                CurrentAction = new TranslateAction(control, e.Location, box.GetCenter(), draggingDepth);
                        }
                    }
                }
                else if (e.Button == MouseButtons.Right)
                {
                    if (Hovered.TryStartDragging(DragActionType.ROTATE, HoveredPart, out rLocalOrientation, out rDragExclusively))
                    {
                        draggingDepth = control.PickingDepth;
                        if (rDragExclusively)
                            ExclusiveAction = new RotateAction(control, e.Location, rLocalOrientation.Origin, draggingDepth);
                        else
                        {
                            BoundingBox box = BoundingBox.Default;

                            foreach (IEditableObject obj in GetObjects())
                                obj.GetSelectionBox(ref box);

                            if (box != BoundingBox.Default)
                                CurrentAction = new RotateAction(control, e.Location, box.GetCenter(), draggingDepth);
                        }
                    }
                }
                else if (e.Button == MouseButtons.Middle)
                {
                    bool shift = WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift);
                    if (Hovered.TryStartDragging(shift ? DragActionType.SCALE : DragActionType.SCALE_EXCLUSIVE, HoveredPart, out rLocalOrientation, out rDragExclusively))
                    {
                        draggingDepth = control.PickingDepth;
                        if (rDragExclusively)
                        {
                            if (shift)
                                ExclusiveAction = new ScaleAction(control, e.Location, rLocalOrientation.Origin);
                            else
                                ExclusiveAction = new ScaleActionIndividual(control, e.Location, rLocalOrientation);
                        }
                        else
                        {
                            BoundingBox box = BoundingBox.Default;

                            foreach (IEditableObject obj in GetObjects())
                                obj.GetSelectionBox(ref box);

                            if (box != BoundingBox.Default)
                            {
                                if (shift)
                                    CurrentAction = new ScaleAction(control, e.Location, box.GetCenter());
                                else
                                    CurrentAction = new ScaleActionIndividual(control, e.Location, rLocalOrientation);
                            }
                        }
                    }
                }
            }
            else
            {
                var |= REDRAW_PICKING;
                var |= FORCE_REENTER;

                CurrentAction = NoAction; //abort current action
                ExclusiveAction = NoAction;
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

            if (CurrentAction != NoAction)
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

                CurrentAction = NoAction;
            }

            if (ExclusiveAction != NoAction)
            {
                Hovered.ApplyTransformActionToPart(ExclusiveAction, HoveredPart, ref transformChangeInfos);

                var |= REDRAW_PICKING;

                ExclusiveAction = NoAction;
            }

            foreach (AbstractGlDrawable obj in StaticObjects)
            {
                var |= obj.MouseUp(e, control);
            }

            AddTransformToUndo(transformChangeInfos);

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

            foreach (AbstractGlDrawable obj in StaticObjects)
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

            CurrentAction = NoAction; //because MouseClick implies that the Mouse Button is not pressed anymore

            var |= REDRAW;
            var |= FORCE_REENTER;

            return var;
        }

        public override uint MouseEnter(int inObjectIndex, GL_ControlBase control)
        {
            if (CurrentAction != NoAction || ExclusiveAction != NoAction)
                return 0;

            foreach (IEditableObject obj in GetObjects())
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
            return 0;
        }

        public override uint MouseLeave(int inObjectIndex, GL_ControlBase control)
        {
            if (CurrentAction != NoAction || ExclusiveAction != NoAction)
                return 0;
            foreach (IEditableObject obj in GetObjects())
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

            foreach (AbstractGlDrawable obj in StaticObjects)
            {
                int span = obj.GetPickableSpan();
                if (inObjectIndex >= 0 && inObjectIndex < span)
                {
                    return obj.MouseLeave(inObjectIndex, control);
                }
                inObjectIndex -= span;
            }
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
            else if (e.KeyCode == Keys.V)
            {
                XRaySelection = true;
                control.Refresh();
                control.Repick();
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
            if (e.KeyCode == Keys.V)
            {
                XRaySelection = false;
                control.Refresh();
                control.Repick();
            }

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
