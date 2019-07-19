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
        public IList List;
        public ListChangedEventArgs(IList list)
        {
            List = list;
        }
    }

    public abstract partial class EditorSceneBase : AbstractGlDrawable
    {
        protected bool multiSelect;

        public IEditableObject Hovered { get; protected set; } = null;

        public int HoveredPart { get; protected set; } = 0;

        public readonly HashSet<object> SelectedObjects = new HashSet<object>();

        public List<AbstractGlDrawable> staticObjects = new List<AbstractGlDrawable>();

        public event EventHandler SelectionChanged;
        public event EventHandler ObjectsMoved;
        public event ListChangedEventHandler ListChanged;
        
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
                {
                    list.Add(obj);
                    var |= obj.SelectDefault(control, SelectedObjects);
                }

                undoStack.Push(new RevertableAddition(objs, list));

                UpdateSelection(var);
            }
            else
            {
                foreach (IEditableObject obj in objs)
                    list.Add(obj);

                undoStack.Push(new RevertableAddition(objs, list));
            }
        }

        public void Delete(IList list, bool updateSelection, params IEditableObject[] objs)
        {
            if (updateSelection)
            {
                uint var = 0;

                List<RevertableDeletion.DeleteInfo> infos = new List<RevertableDeletion.DeleteInfo>();

                foreach (IEditableObject obj in objs)
                {
                    infos.Add(new RevertableDeletion.DeleteInfo(obj, list.IndexOf(obj)));
                    list.Remove(obj);
                    var |= obj.DeselectAll(control, SelectedObjects);
                }

                undoStack.Push(new RevertableDeletion(infos.ToArray(), list));

                UpdateSelection(var);
            }
            else
            {
                List<RevertableDeletion.DeleteInfo> infos = new List<RevertableDeletion.DeleteInfo>();

                foreach (IEditableObject obj in objs)
                {
                    infos.Add(new RevertableDeletion.DeleteInfo(obj, list.IndexOf(obj)));
                    list.Remove(obj);
                }

                undoStack.Push(new RevertableDeletion(infos.ToArray(), list));
            }
        }

        public void Delete(IList list, bool updateSelection, IEnumerable<object> objs)
        {
            if (updateSelection)
            {
                uint var = 0;

                List<RevertableDeletion.DeleteInfo> infos = new List<RevertableDeletion.DeleteInfo>();

                foreach (IEditableObject obj in objs)
                {
                    infos.Add(new RevertableDeletion.DeleteInfo(obj, list.IndexOf(obj)));
                    list.Remove(obj);
                    var |= obj.DeselectAll(control, SelectedObjects);
                }

                undoStack.Push(new RevertableDeletion(infos.ToArray(), list));

                UpdateSelection(var);
            }
            else
            {
                List<RevertableDeletion.DeleteInfo> infos = new List<RevertableDeletion.DeleteInfo>();

                foreach (IEditableObject obj in objs)
                {
                    infos.Add(new RevertableDeletion.DeleteInfo(obj, list.IndexOf(obj)));
                    list.Remove(obj);
                }

                undoStack.Push(new RevertableDeletion(infos.ToArray(), list));
            }
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

                undoStack.Push(new RevertableAddition(objs, list));

                UpdateSelection(var);
            }
            else
            {
                foreach (IEditableObject obj in objs)
                {
                    list.Insert(index, obj);
                    index++;
                }

                undoStack.Push(new RevertableAddition(objs, list));
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

            undoStack.Push(new RevertableReordering(originalIndex + offset, count, -offset, list));
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

            bool alreadySelected = obj.IsSelected();
            if (alreadySelected && !isSelected)
            {
                var |= obj.DeselectAll(control, SelectedObjects);
            }
            else if (!alreadySelected && isSelected)
            {
                var |= obj.SelectDefault(control, SelectedObjects);
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

                            foreach (IEditableObject selected in SelectedObjects)
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

                            foreach (IEditableObject selected in SelectedObjects)
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

                            foreach (IEditableObject selected in SelectedObjects)
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
                foreach (IEditableObject obj in SelectedObjects)
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
                    if (!shift)
                    {
                        foreach (IEditableObject selected in SelectedObjects)
                        {
                            selected.DeselectAll(control,null);
                        }
                        SelectedObjects.Clear();
                        SelectionChanged?.Invoke(this, new EventArgs());
                    }
                }
                else if (multiSelect)
                {
                    if (shift && hoveredIsSelected)
                    {
                        //remove from selection
                        Hovered.Deselect(HoveredPart, control, SelectedObjects);
                        SelectionChanged?.Invoke(this, new EventArgs());
                    }
                    else if (shift)
                    {
                        //add to selection
                        Hovered.Select(HoveredPart, control, SelectedObjects);
                        SelectionChanged?.Invoke(this, new EventArgs());
                    }
                    else if (!hoveredIsSelected)
                    {
                        //change selection
                        foreach (IEditableObject selected in SelectedObjects)
                        {
                            selected.DeselectAll(control, null);
                        }
                        SelectedObjects.Clear();
                        Hovered.Select(HoveredPart, control, SelectedObjects);
                        SelectionChanged?.Invoke(this, new EventArgs());
                    }
                }
                else
                {
                    if (shift && hoveredIsSelected)
                    {
                        //remove from selection
                        Hovered.Deselect(HoveredPart, control, SelectedObjects);
                        SelectionChanged?.Invoke(this, new EventArgs());
                    }
                    else if (!hoveredIsSelected)
                    {
                        //change selection
                        foreach (IEditableObject selected in SelectedObjects)
                        {
                            selected.DeselectAll(control, null);
                        }
                        SelectedObjects.Clear();
                        Hovered.Select(HoveredPart, control, SelectedObjects);
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
