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

namespace GL_EditorFramework.EditorDrawables
{
	public abstract partial class EditorSceneBase : AbstractGlDrawable
	{
		protected bool multiSelect;

		public EditableObject hovered = null;

		public int hoveredPart = 0;

		protected List<EditableObject> selectedObjects = new List<EditableObject>();

		public List<AbstractGlDrawable> staticObjects = new List<AbstractGlDrawable>();

		public event EventHandler SelectionChanged;

		protected float draggingDepth;

		protected GL_ControlBase control;

		public AbstractTransformAction currentAction = noAction;

		protected static NoAction noAction = new NoAction();

		protected void UpdateSelection(uint var)
		{
			SelectionChanged?.Invoke(this, new EventArgs());

			if ((var & AbstractGlDrawable.REDRAW) > 0)
				control.Refresh();
			if ((var & AbstractGlDrawable.REDRAW_PICKING) > 0)
				control.DrawPicking();
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
					SelectionChanged?.Invoke(this, new EventArgs());

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
			if(currentAction==noAction)
			{
				if (selectedObjects.Contains(hovered) && hovered.CanStartDragging())
				{
					if (e.Button == MouseButtons.Left)
					{
						draggingDepth = control.PickingDepth;
						currentAction = new TranslateAction(control, e.Location, draggingDepth);
					}
					else if (e.Button == MouseButtons.Right)
					{
						draggingDepth = control.PickingDepth;
						Vector3 center = new Vector3();
						foreach (EditableObject obj in selectedObjects)
						{
							center += obj.GetSelectionCenter();
						}
						center /= selectedObjects.Count;

						currentAction = new RotateAction(control, e.Location, center, draggingDepth);
					}
					else if (e.Button == MouseButtons.Middle)
					{
						draggingDepth = control.PickingDepth;
						Vector3 center = new Vector3();
						foreach (EditableObject obj in selectedObjects)
						{
							center += obj.GetSelectionCenter();
						}
						center /= selectedObjects.Count;
						if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
							currentAction = new ScaleAction(control, e.Location, center);
						else
							currentAction = new ScaleActionIndividual(control, e.Location, hovered);
					}
				}
			}
			else
			{
				currentAction = noAction; //abort current action
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

			if (currentAction != noAction)
			{
				currentAction.UpdateMousePos(e.Location);

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

			if (currentAction != noAction)
			{
				currentAction.ApplyScrolling(e.Location, e.Delta);

				var |= REDRAW | NO_CAMERA_ACTION;

				var &= ~REPICK;
			}

			return var;
		}

		public override uint MouseUp(MouseEventArgs e, GL_ControlBase control)
		{
			uint var = 0;

			if (currentAction != noAction)
			{
				foreach (EditableObject obj in selectedObjects)
				{
					obj.ApplyTransformActionToSelection(currentAction);
				}

				var |= REDRAW;

				currentAction = noAction;
			}

			foreach (AbstractGlDrawable obj in staticObjects)
			{
				var |= obj.MouseUp(e, control);
			}
			
			return var;
		}

		public override uint MouseClick(MouseEventArgs e, GL_ControlBase control)
		{
			uint var = 0;
			foreach (AbstractGlDrawable obj in staticObjects)
			{
				var |= obj.MouseClick(e, control);
			}

			if (e.Button == MouseButtons.Left)
			{
				if (!(multiSelect && WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift)))
				{
					if (multiSelect)
					{
						if (!selectedObjects.Contains(hovered))
						{
							foreach (EditableObject selected in selectedObjects)
							{
								selected.DeselectAll(control);
							}
						}

						if (hovered != null && !selectedObjects.Contains(hovered))
						{
							selectedObjects.Clear();
							selectedObjects.Add(hovered);
							hovered.Select(hoveredPart, control);
							SelectionChanged?.Invoke(this, new EventArgs());
						}
						else if (hovered == null)
						{
							selectedObjects.Clear();
							SelectionChanged?.Invoke(this, new EventArgs());
						}
					}
					else
					{
						foreach (EditableObject selected in selectedObjects)
						{
							selected.DeselectAll(control);
						}

						if (hovered != null && !selectedObjects.Contains(hovered))
						{
							selectedObjects.Clear();
							selectedObjects.Add(hovered);
							hovered.Select(hoveredPart, control);
							SelectionChanged?.Invoke(this, new EventArgs());
						}
						else
						{
							selectedObjects.Clear();
							SelectionChanged?.Invoke(this, new EventArgs());
						}
					}
				}
				else
				{
					if (selectedObjects.Contains(hovered))
					{
						selectedObjects.Remove(hovered);
						hovered.Deselect(hoveredPart, control);
						SelectionChanged?.Invoke(this, new EventArgs());
					}
					else if (hovered != null)
					{
						selectedObjects.Add(hovered);
						hovered.Select(hoveredPart, control);
						SelectionChanged?.Invoke(this, new EventArgs());
					}
				}
			}

			currentAction = noAction; //because MouseClick implies that the Mouse Button is not pressed anymore

			var |= REDRAW;

			return var;
		}

		public override uint MouseEnter(int inObjectIndex, GL_ControlBase control)
		{
			if (currentAction != noAction)
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
			hovered = null;
			return REDRAW;
		}
	}
}
