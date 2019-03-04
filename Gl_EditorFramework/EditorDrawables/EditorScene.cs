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
		public List<EditableObject> objects = new List<EditableObject>();

		public EditorScene(bool multiSelect = true)
		{
			this.multiSelect = multiSelect;
		}
		
		public void Add(params EditableObject[] objs)
		{
			uint var = 0;

			foreach (EditableObject selected in selectedObjects)
			{
				var |= selected.DeselectAll(control);
			}
			selectedObjects.Clear();

			foreach (EditableObject obj in objs)
			{
				objects.Add(obj);

				selectedObjects.Add(obj);
				var |= obj.SelectDefault(control);
			}
			UpdateSelection(var);
		}

		public void Delete(params EditableObject[] objs)
		{
			uint var = 0;

			bool selectionHasChanged = false;

			foreach (EditableObject obj in objs)
			{
				objects.Remove(obj);
				if (selectedObjects.Contains(obj))
				{
					var |= obj.DeselectAll(control);
					selectedObjects.Remove(obj);
					selectionHasChanged = true;
				}
			}
			if (selectionHasChanged)
				UpdateSelection(var);
		}

		public void InsertAfter(int index, params EditableObject[] objs)
		{
			uint var = 0;

			foreach (EditableObject selected in selectedObjects)
			{
				var |= selected.DeselectAll(control);
			}
			selectedObjects.Clear();

			foreach (EditableObject obj in objs)
			{
				objects.Insert(index, obj);

				selectedObjects.Add(obj);
				var |= obj.SelectDefault(control);
				index++;
			}
			UpdateSelection(var);
		}

		public override void Draw(GL_ControlModern control, Pass pass)
		{
			foreach (EditableObject obj in objects)
			{
				if(obj.Visible)
					obj.Draw(control, pass, this);
			}
			foreach (AbstractGlDrawable obj in staticObjects)
			{
				obj.Draw(control, pass);
			}
		}

		public override void Draw(GL_ControlLegacy control, Pass pass)
		{
			foreach (EditableObject obj in objects)
			{
				if (obj.Visible)
					obj.Draw(control, pass, this);
			}
			foreach (AbstractGlDrawable obj in staticObjects)
			{
				obj.Draw(control, pass);
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

		public override uint MouseDown(MouseEventArgs e, I3DControl control)
		{
			uint var = 0;
			var |= base.MouseDown(e, control);
			foreach (EditableObject obj in objects)
			{
				var |= obj.MouseDown(e, control);
			}
			return var;
		}

		public override uint MouseMove(MouseEventArgs e, Point lastMousePos, I3DControl control)
		{
			uint var = 0;

			foreach (EditableObject obj in objects)
			{
				var |= obj.MouseMove(e, lastMousePos, control);
			}
			var |= base.MouseMove(e, lastMousePos, control);
			return var;
		}

		public override uint MouseUp(MouseEventArgs e, I3DControl control)
		{
			uint var = 0;

			var |= base.MouseUp(e, control);
			foreach (EditableObject obj in objects)
			{
				var |= obj.MouseUp(e, control);
			}
			return var;
		}

		public override uint MouseClick(MouseEventArgs e, I3DControl control)
		{
			uint var = 0;
			foreach (EditableObject obj in objects)
			{
				var |= obj.MouseClick(e, control);
			}

			var |= base.MouseClick(e, control);

			var |= REDRAW;

			return var;
		}

		public override uint MouseWheel(MouseEventArgs e, I3DControl control)
		{
			uint var = 0;
			foreach (EditableObject obj in objects) {
				var |= obj.MouseWheel(e, control);
			}
			foreach (AbstractGlDrawable obj in staticObjects)
			{
				var |= obj.MouseWheel(e, control);
			}
			return var;
		}

		public override int GetPickableSpan()
		{
			int var = 0;
			foreach (EditableObject obj in objects)
				var += obj.GetPickableSpan();
			foreach (AbstractGlDrawable obj in staticObjects)
				var += obj.GetPickableSpan();
			return var;
		}

		public override uint MouseEnter(int inObjectIndex, I3DControl control)
		{
			foreach (EditableObject obj in objects)
			{
				int span = obj.GetPickableSpan();
				if (inObjectIndex >= 0 && inObjectIndex < span)
				{
					hovered = obj;
					return obj.MouseEnter(inObjectIndex, control) | REDRAW;
				}
				inObjectIndex -= span;
			}

			base.MouseEnter(inObjectIndex, control); 
			return 0;
		}

		public override uint MouseLeave(int inObjectIndex, I3DControl control)
		{
			foreach (EditableObject obj in objects)
			{
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

		public override uint KeyDown(KeyEventArgs e, I3DControl control)
		{
			uint var = 0;

			bool selectionHasChanged = false;

			if (e.KeyCode == Keys.Z && selectedObjects.Count>0)
			{
				Vector3 sum = new Vector3();
				int index = 0;
				foreach (EditableObject selected in selectedObjects)
				{
					sum -= selected.GetSelectionCenter();
					index++;
				}
				sum /= index;
				control.CameraTarget = sum;

				var = REDRAW_PICKING;
			}else if (e.KeyCode == Keys.H && selectedObjects.Count > 0)
			{
				foreach (EditableObject selected in selectedObjects)
				{
					selected.Visible = e.Shift;
				}
				var = REDRAW_PICKING;
			}
			else if (e.Control && e.KeyCode == Keys.A)
			{
				if (e.Shift)
				{
					foreach (EditableObject selected in selectedObjects)
					{
						selected.DeselectAll(control);
					}
					selectedObjects.Clear();
					selectionHasChanged = true;
				}

				if (!e.Shift && multiSelect)
				{
					foreach (EditableObject obj in objects)
					{
						obj.SelectAll(control);
						selectedObjects.Add(obj);
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

			UpdateSelection(var);

			return var;
		}

		public override uint KeyUp(KeyEventArgs e, I3DControl control)
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
	}
}
