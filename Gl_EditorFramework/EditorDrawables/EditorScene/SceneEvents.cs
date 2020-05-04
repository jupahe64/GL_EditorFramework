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
        public void StartTransformAction(LocalOrientation localOrientation, DragActionType dragActionType, IRevertable revertable = null)
        {
            if (CurrentAction != null || SelectionTransformAction != NoAction)
                return;

            AbstractTransformAction transformAction;

            Vector3 pivot;

            draggingDepth = control.PickingDepth;
            
            BoundingBox box = BoundingBox.Default;

            foreach (IEditableObject obj in GetObjects())
                obj.GetSelectionBox(ref box);

            pivot = box.GetCenter();

            if (box == BoundingBox.Default)
                return;
            
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

            StartTransformAction(transformAction, revertable);
        }

        public void StartTransformAction(AbstractTransformAction transformAction, IRevertable revertable = null)
        {
            if (CurrentAction != null || SelectionTransformAction != NoAction)
                return;

            if (revertable != null)
            {
                BeginUndoCollection();
                AddToUndo(revertable);
            }

            
            SelectionTransformAction = transformAction;
        }

        public void StartAction(AbstractAction action)
        {
            if (CurrentAction != null || SelectionTransformAction != NoAction)
                return;

            CurrentAction = action;
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
                if (SelectionTransformAction == NoAction && CurrentAction == null && Hovered != null && TryGetActionType(out DragActionType dragActionType))
                {
                    Hovered.StartDragging(dragActionType, HoveredPart, this);
                }
                else
                {
                    var |= REDRAW_PICKING;
                    var |= FORCE_REENTER;

                    if(SelectionTransformAction!=NoAction)
                        EndUndoCollection();

                    SelectionTransformAction = NoAction; //abort current action
                    CurrentAction?.Cancel();
                    CurrentAction = null;
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

            if (SelectionTransformAction != NoAction || CurrentAction != null)
            {
                SelectionTransformAction.UpdateMousePos(e.Location);
                CurrentAction?.UpdateMousePos(e.Location);

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

            if (SelectionTransformAction != NoAction || CurrentAction != null)
            {
                SelectionTransformAction.ApplyScrolling(e.Location, e.Delta);
                CurrentAction?.ApplyScrolling(e.Location, e.Delta);

                var |= REDRAW | NO_CAMERA_ACTION;

                var &= ~REPICK;
            }

            return var;
        }

        public override uint MouseUp(MouseEventArgs e, GL_ControlBase control)
        {
            TransformChangeInfos transformChangeInfos = new TransformChangeInfos(new List<TransformChangeInfo>());
            uint var = 0;

            if (SelectionTransformAction != NoAction && SelectionTransformAction.IsApplyOnRelease())
            {
                foreach (IEditableObject obj in GetObjects())
                {
                    obj.ApplyTransformActionToSelection(SelectionTransformAction, ref transformChangeInfos);
                }

                var |= REDRAW_PICKING | FORCE_REENTER;

                AddTransformToUndo(transformChangeInfos);
                EndUndoCollection();

                SelectionTransformAction = NoAction;
            }
            else if (CurrentAction != null)
            {
                CurrentAction?.Apply();
                CurrentAction = null;

                var |= REDRAW_PICKING;

                AddTransformToUndo(transformChangeInfos);
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
            if (SelectionTransformAction != NoAction || CurrentAction != null)
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

            uint var = 0;

            if (Hovered!=null)
                var = REDRAW;

            Hovered = null;
            HoveredPart = -1;

            foreach (AbstractGlDrawable obj in StaticObjects)
            {
                int span = obj.GetPickableSpan();
                if (inObjectIndex >= 0 && inObjectIndex < span)
                {
                    return obj.MouseEnter(inObjectIndex, control);
                }
                inObjectIndex -= span;
            }

            ObjectRenderState.ShouldBeDrawn = ObjectRenderState.ShouldBeDrawn_Default;

            return var;
        }

        public override uint MouseLeave(int inObjectIndex, GL_ControlBase control)
        {
            if (SelectionTransformAction != NoAction || CurrentAction != null)
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
            if (SelectionTransformAction != NoAction || CurrentAction != null)
                return 0;

            Hovered = null;
            return REDRAW;
        }

        public static Keys KS_Focus = Framework.KeyStroke("Z");

        public static Keys KS_Undo = Framework.KeyStroke("Ctrl + Z");

        public static Keys KS_Redo = Framework.KeyStroke("Ctrl + Y");

        public static Keys KS_Hide = Framework.KeyStroke("H");

        public static Keys KS_UnHide = Framework.KeyStroke("Shift + H");

        public static Keys KS_SnapSelected = Framework.KeyStroke("Shift + S");

        public static Keys KS_ResetRotation = Framework.KeyStroke("Shift + R");

        public static Keys KS_ResetScale = Framework.KeyStroke("Ctrl + Shift + R");

        public static Keys KS_SelectAll = Framework.KeyStroke("Ctrl + A");

        public static Keys KS_DeSelectAll = Framework.KeyStroke("Ctrl + Shift + A");

        public override uint KeyDown(KeyEventArgs e, GL_ControlBase control, bool isRepeat)
        {
            TransformChangeInfos transformChangeInfos = new TransformChangeInfos(new List<TransformChangeInfo>());
            uint var = 0;

            bool selectionHasChanged = false;

            if (!isRepeat)
            {
                if ((SelectionTransformAction != NoAction || CurrentAction != null))
                {
                    SelectionTransformAction.KeyDown(e);
                    CurrentAction?.KeyDown(e);
                    var = NO_CAMERA_ACTION | REDRAW;
                }
                else
                {
                    if      (e.KeyData == KS_Focus)
                    {
                        BoundingBox box = BoundingBox.Default;

                        foreach (IEditableObject obj in GetObjects())
                        {
                            obj.GetSelectionBox(ref box);
                        }

                        if (box != BoundingBox.Default)
                            control.CameraTarget = box.GetCenter();

                        var = REDRAW_PICKING;
                    }
                    else if (e.KeyData == KS_Undo)
                    {
                        Undo();

                        var = REDRAW_PICKING;
                    }
                    else if (e.KeyData == KS_Redo)
                    {
                        Redo();

                        var = REDRAW_PICKING;
                    }
                    else if (e.KeyData == KS_Hide)
                    {
                        foreach (IEditableObject obj in GetObjects())
                        {
                            if (obj.IsSelected())
                                obj.Visible = false;
                        }
                        var = REDRAW_PICKING;
                    }
                    else if (e.KeyData == KS_UnHide)
                    {
                        foreach (IEditableObject obj in GetObjects())
                        {
                            if (obj.IsSelected())
                                obj.Visible = true;
                        }
                        var = REDRAW_PICKING;
                    }
                    else if (e.KeyData == KS_SnapSelected)
                    {
                        SnapAction action = new SnapAction();
                        foreach (IEditableObject obj in GetObjects())
                        {
                            obj.ApplyTransformActionToSelection(action, ref transformChangeInfos);
                        }
                        var = REDRAW_PICKING;
                    }
                    else if (e.KeyData == KS_ResetRotation)
                    {
                        ResetRot action = new ResetRot();
                        foreach (IEditableObject obj in GetObjects())
                        {
                            obj.ApplyTransformActionToSelection(action, ref transformChangeInfos);
                        }
                        var = REDRAW_PICKING;
                    }
                    else if (e.KeyData == KS_ResetScale)
                    {
                        ResetScale action = new ResetScale();
                        foreach (IEditableObject obj in GetObjects())
                        {
                            obj.ApplyTransformActionToSelection(action, ref transformChangeInfos);
                        }
                        var = REDRAW_PICKING;
                    }
                    else if (e.KeyData == KS_DeSelectAll)
                    {
                        foreach (IEditableObject obj in GetObjects())
                        {
                            obj.DeselectAll(control);
                        }
                        selectionHasChanged = true;
                        var = REDRAW;
                    }
                    else if (e.KeyData == KS_SelectAll)
                    {
                        if (multiSelect)
                        {
                            foreach (IEditableObject obj in GetObjects())
                            {
                                obj.SelectAll(control);
                            }
                            selectionHasChanged = true;
                        }
                        var = REDRAW;
                    }
                }
            }
            foreach (IEditableObject obj in GetObjects())
            {
                var |= obj.KeyDown(e, control, isRepeat);
            }
            foreach (AbstractGlDrawable obj in StaticObjects)
            {
                var |= obj.KeyDown(e, control, isRepeat);
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
