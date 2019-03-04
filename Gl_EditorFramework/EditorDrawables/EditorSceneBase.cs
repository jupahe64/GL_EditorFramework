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
	public abstract class EditorSceneBase : AbstractGlDrawable
	{
		protected bool multiSelect;

		public EditableObject hovered = null;

		public int hoveredPart = 0;

		protected List<EditableObject> selectedObjects = new List<EditableObject>();

		public List<AbstractGlDrawable> staticObjects = new List<AbstractGlDrawable>();

		public event EventHandler SelectionChanged;

		private float draggingDepth;

		protected GL_ControlBase control;

		public EditableObject.DeltaTransform deltaTransform = new EditableObject.DeltaTransform(new Vector3(), new Quaternion(), new Vector3(1, 1, 1));

		AbstractTransformAction currentAction;

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

					Console.WriteLine(obj);
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

		public override uint MouseDown(MouseEventArgs e, I3DControl control)
		{
			uint var = 0;
			if (draggingDepth == -1 && e.Button == MouseButtons.Left && selectedObjects.Contains(hovered))
			{
				if (hovered.CanStartDragging())
					draggingDepth = control.PickingDepth;
			}
			foreach (AbstractGlDrawable obj in staticObjects)
			{
				var |= obj.MouseDown(e, control);
			}
			return var;
		}

		public override uint MouseMove(MouseEventArgs e, Point lastMousePos, I3DControl control)
		{
			uint var = 0;

			foreach (AbstractGlDrawable obj in staticObjects)
			{
				var |= obj.MouseMove(e, lastMousePos, control);
			}

			if (draggingDepth != -1)
			{
				Vector3 translation = new Vector3();

				//code from Whitehole

				float deltaX = e.X - control.DragStartPos.X;
				float deltaY = e.Y - control.DragStartPos.Y;

				deltaX *= draggingDepth * control.FactorX;
				deltaY *= draggingDepth * control.FactorY;

				translation += Vector3.UnitX * deltaX * (float)Math.Cos(control.CamRotX);
				translation -= Vector3.UnitX * deltaY * (float)Math.Sin(control.CamRotX) * (float)Math.Sin(control.CamRotY);
				translation -= Vector3.UnitY * deltaY * (float)Math.Cos(control.CamRotY);
				translation += Vector3.UnitZ * deltaX * (float)Math.Sin(control.CamRotX);
				translation += Vector3.UnitZ * deltaY * (float)Math.Cos(control.CamRotX) * (float)Math.Sin(control.CamRotY);

				deltaTransform.Translation = translation;

				var |= REDRAW | NO_CAMERA_ACTION;

				var &= ~REPICK;
			}
			else
			{
				var |= REPICK;
			}
			return var;
		}

		public override uint MouseUp(MouseEventArgs e, I3DControl control)
		{
			uint var = 0;

			if (!(draggingDepth == -1) && e.Button == MouseButtons.Left)
			{
				foreach (EditableObject obj in selectedObjects)
				{
					obj.ApplyTransformationToSelection(deltaTransform);
				}

				deltaTransform = new EditableObject.DeltaTransform(new Vector3(), new Quaternion(), new Vector3(1, 1, 1));
			}

			draggingDepth = -1;

			foreach (AbstractGlDrawable obj in staticObjects)
			{
				var |= obj.MouseUp(e, control);
			}
			
			return var;
		}

		public override uint MouseClick(MouseEventArgs e, I3DControl control)
		{
			uint var = 0;
			foreach (AbstractGlDrawable obj in staticObjects)
			{
				var |= obj.MouseClick(e, control);
			}

			if (!(e.Button == MouseButtons.Left))
				return var;

			if (!(multiSelect && OpenTK.Input.Keyboard.GetState().IsKeyDown(OpenTK.Input.Key.ShiftLeft)))
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

			draggingDepth = -1; //because MouseClick implies that the Mouse Button is not pressed anymore

			var |= REDRAW;

			return var;
		}

		public override uint MouseEnter(int inObjectIndex, I3DControl control)
		{
			if (!(draggingDepth == -1))
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

		public override uint MouseLeave(int inObjectIndex, I3DControl control)
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

		public override uint MouseLeaveEntirely(I3DControl control)
		{
			hovered = null;
			return REDRAW;
		}

		public abstract class AbstractTransformAction
		{
			public abstract Vector3 newPos(Vector3 pos);

			public Quaternion deltaRotation;

			public Vector3 scale;

			public void SetDragDist(Point point) { }
			public void SetScrollDragDist(Point point, Point projX, Point projY, Point projZ) { }
		}

		public class TranslateAction : AbstractTransformAction
		{
			Vector3 translation = new Vector3();
			public override Vector3 newPos(Vector3 pos)
			{
				return pos + translation;
			}
		}
	}
}
