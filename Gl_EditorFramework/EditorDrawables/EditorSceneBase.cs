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

namespace GL_EditorFramework.EditorDrawables
{
    public abstract partial class EditorSceneBase : AbstractGlDrawable
    {
        protected bool multiSelect;

        public EditableObject Hovered { get; protected set; } = null;

        public int HoveredPart { get; protected set; } = 0;

        protected List<EditableObject> selectedObjects = new List<EditableObject>();

        public List<AbstractGlDrawable> staticObjects = new List<AbstractGlDrawable>();

        public event EventHandler SelectionChanged;
        public event EventHandler ObjectsMoved;

        protected float draggingDepth;

        protected GL_ControlBase control;

        protected Stack<IRevertable> undoStack = new Stack<IRevertable>();
        protected Stack<IRevertable> redoStack = new Stack<IRevertable>();

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

        protected void UpdateSelection(uint var)
        {
            SelectionChanged?.Invoke(this, new EventArgs());

            if ((var & REDRAW) > 0)
                control.Refresh();
            if ((var & REDRAW_PICKING) > 0)
                control.DrawPicking();
        }

        public void ApplyCurrentTransformAction()
        {
            TransformChangeInfos transformChangeInfos = new TransformChangeInfos(new List<TransformChangeInfo>());

            foreach (EditableObject obj in selectedObjects)
            {
                obj.ApplyTransformActionToSelection(CurrentAction, ref transformChangeInfos);
            }

            CurrentAction = NoAction;

            AddTransformToUndo(transformChangeInfos);
        }

        public List<EditableObject> SelectedObjects
        {
            get => selectedObjects;
            set
            {
                uint var = 0;

                bool selectionHasChanged = false;

                foreach (EditableObject obj in value)
                {
                    if (!selectedObjects.Contains(obj)) //object wasn't selected before
                    {
                        var |= obj.SelectDefault(control); //select it
                        selectionHasChanged = true;
                    }
                    else //object stays selected 
                    {
                        selectedObjects.Remove(obj); //filter out these to find all objects which are not selected anymore
                        selectionHasChanged = true;
                    }
                }

                foreach (EditableObject obj in selectedObjects) //now the selected objects are a list of objects to deselect
                                                                //which is fine because in the end they get overwriten anyway
                {
                    var |= obj.DeselectAll(control); //Deselect them all
                    selectionHasChanged = true;
                }
                selectedObjects = value;

                if (selectionHasChanged)
                {
                    if ((var & AbstractGlDrawable.REDRAW) > 0)
                        control.Refresh();
                    if ((var & AbstractGlDrawable.REDRAW_PICKING) > 0)
                        control.DrawPicking();
                }
            }
        }

        public void ToogleSelected(EditableObject obj, bool isSelected)
        {
            uint var = 0;

            bool selectionHasChanged = false;

            bool alreadySelected = selectedObjects.Contains(obj);
            if (alreadySelected && !isSelected)
            {
                var |= obj.DeselectAll(control);
                selectedObjects.Remove(obj);

                selectionHasChanged = true;
            }
            else if (!alreadySelected && isSelected)
            {
                var |= obj.SelectDefault(control);
                selectedObjects.Add(obj);

                selectionHasChanged = true;
            }

            if (selectionHasChanged)
            {
                SelectionChanged?.Invoke(this, new EventArgs());

                if ((var & AbstractGlDrawable.REDRAW) > 0)
                    control.Refresh();
                if ((var & AbstractGlDrawable.REDRAW_PICKING) > 0)
                    control.DrawPicking();
            }
        }

        public override uint MouseDown(MouseEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            if(CurrentAction==NoAction && ExclusiveAction==NoAction && Hovered!=null)
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

                            foreach (EditableObject selected in selectedObjects)
                            {
                                box.Include(selected.GetSelectionBox());
                            }

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

                            foreach (EditableObject selected in selectedObjects)
                            {
                                box.Include(selected.GetSelectionBox());
                            }

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

                            foreach (EditableObject selected in selectedObjects)
                            {
                                box.Include(selected.GetSelectionBox());
                            }
                            if (shift)
                                CurrentAction = new ScaleAction(control, e.Location, box.GetCenter());
                            else
                                CurrentAction = new ScaleActionIndividual(control, e.Location, rLocalOrientation);
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
            
            foreach (AbstractGlDrawable obj in staticObjects)
            {
                var |= obj.MouseDown(e, control);
            }
            return var;
        }

        public override uint MouseMove(MouseEventArgs e, Point lastMousePos, GL_ControlBase control)
        {
            uint var = 0;

            foreach (AbstractGlDrawable obj in staticObjects)
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

            foreach (AbstractGlDrawable obj in staticObjects)
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
                foreach (EditableObject obj in selectedObjects)
                {
                    obj.ApplyTransformActionToSelection(CurrentAction, ref transformChangeInfos);
                }

                var |= REDRAW_PICKING;

                CurrentAction = NoAction;
            }

            if (ExclusiveAction != NoAction)
            {
                Hovered.ApplyTransformActionToPart(ExclusiveAction, HoveredPart, ref transformChangeInfos);

                var |= REDRAW_PICKING;

                ExclusiveAction = NoAction;
            }

            foreach (AbstractGlDrawable obj in staticObjects)
            {
                var |= obj.MouseUp(e, control);
            }

            AddTransformToUndo(transformChangeInfos);
            return var;
        }

        public override uint MouseClick(MouseEventArgs e, GL_ControlBase control)
        {
            uint var = 0;
            foreach (AbstractGlDrawable obj in staticObjects)
            {
                var |= obj.MouseClick(e, control);
            }

            bool shift = WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift);
            bool hoveredIsSelected = Hovered != null && Hovered.IsSelected(HoveredPart);
            bool nothingHovered = Hovered == null;

            if (e.Button == MouseButtons.Left)
            {
                if (nothingHovered)
                {
                    selectedObjects.Clear();
                    foreach (EditableObject selected in selectedObjects)
                    {
                        selected.DeselectAll(control);
                    }
                }
                else if (multiSelect)
                {
                    if (shift && hoveredIsSelected)
                    {
                        //remove from selection
                        selectedObjects.Remove(Hovered);
                        Hovered.Deselect(HoveredPart, control);
                        SelectionChanged?.Invoke(this, new EventArgs());
                    }
                    else if(shift)
                    {
                        //add to selection
                        selectedObjects.Add(Hovered);
                        Hovered.Select(HoveredPart, control);
                        SelectionChanged?.Invoke(this, new EventArgs());
                    }
                    else if (!hoveredIsSelected)
                    {
                        //change selection
                        selectedObjects.Clear();
                        foreach (EditableObject selected in selectedObjects)
                        {
                            selected.DeselectAll(control);
                        }
                        selectedObjects.Add(Hovered);
                        Hovered.Select(HoveredPart, control);
                        SelectionChanged?.Invoke(this, new EventArgs());
                    }
                }
                else
                {
                    if (shift && hoveredIsSelected)
                    {
                        //remove from selection
                        selectedObjects.Remove(Hovered);
                        Hovered.Deselect(HoveredPart, control);
                        SelectionChanged?.Invoke(this, new EventArgs());
                    }
                    else if (!hoveredIsSelected)
                    {
                        //change selection
                        selectedObjects.Clear();
                        foreach (EditableObject selected in selectedObjects)
                        {
                            selected.DeselectAll(control);
                        }
                        selectedObjects.Add(Hovered);
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
            
            foreach (AbstractGlDrawable obj in staticObjects)
            {
                int span = obj.GetPickableSpan();
                if (inObjectIndex >= 0 && inObjectIndex < span)
                {
                    return obj.MouseEnter(inObjectIndex, control);
                }
                inObjectIndex -= span;
            }
            return 0;
        }

        public override uint MouseLeave(int inObjectIndex, GL_ControlBase control)
        {
            foreach (AbstractGlDrawable obj in staticObjects)
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
    }
}
