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
	public class EditorScene : AbstractGlDrawable
	{
		public static EditorScene currentScene;

		protected bool multiSelect;

		public ObjID hovered = ObjID.None;

		public static int currentObjectIndex;

		public static bool IsHovered(int subObjectIndex = 0)
		{
			if (currentScene == null)
				return false;
			return currentScene.hovered.Equals(new ObjID(currentObjectIndex, subObjectIndex));
		}

		public static bool IsSelected(int subObjectIndex = 0)
		{
			if (currentScene == null)
				return false;
			return currentScene.selectedObjects.ContainsKey(new ObjID(currentObjectIndex, subObjectIndex));
		}

		public List<EditableObject> objects = new List<EditableObject>();

		public List<AbstractGlDrawable> staticObjects = new List<AbstractGlDrawable>();

		public Dictionary<ObjID, SelectInfo> selectedObjects = new Dictionary<ObjID, SelectInfo>();

		public event EventHandler SelectionChanged;

		public ObjID dragObj = ObjID.None;
		private float draggingDepth;

		private Control control;

		public EditorScene(bool multiSelect = true)
		{
			this.multiSelect = multiSelect;
		}

		public void AddObject(EditableObject obj)
		{
			objects.Add(obj);
			foreach (ObjID selected in selectedObjects.Keys.ToArray())
			{
				objects[selected.ObjectIndex].Deselect(selected.SubObjectIndex, (I3DControl)control);
			}
			selectedObjects.Clear();

			foreach (int subObj in obj.getAllSelection())
			{
				selectedObjects.Add(new ObjID(objects.Count-1, subObj), new SelectInfo(obj.getPosition(subObj)));
				obj.Select(subObj, (I3DControl)control);
			}
			SelectionChanged?.Invoke(this, new EventArgs());

			control.Refresh();
		}

		public void DeleteObject(EditableObject obj)
		{
			objects.Remove(obj);
			foreach (ObjID selected in selectedObjects.Keys.ToArray())
			{
				objects[selected.ObjectIndex].Deselect(selected.SubObjectIndex, (I3DControl)control);
			}
			selectedObjects.Clear();
			SelectionChanged?.Invoke(this, new EventArgs());

			control.Refresh();
		}

		public void InsertAfter(int index, EditableObject obj)
		{
			objects.Insert(index, obj);

			foreach (ObjID selected in selectedObjects.Keys.ToArray())
			{
				objects[selected.ObjectIndex].Deselect(selected.SubObjectIndex, (I3DControl)control);
			}
			selectedObjects.Clear();

			foreach (int subObj in obj.getAllSelection())
			{
				selectedObjects.Add(new ObjID(index, subObj), new SelectInfo(obj.getPosition(subObj)));
				obj.Select(subObj, (I3DControl)control);
			}
			SelectionChanged?.Invoke(this, new EventArgs());

			control.Refresh();
		}

		public override void Draw(GL_ControlModern control)
		{
			currentScene = this;
			currentObjectIndex = 0;
			foreach (EditableObject o in objects)
			{
				if(o.Visible)
					o.Draw(control);
				currentObjectIndex++;
			}
			foreach (AbstractGlDrawable o in staticObjects)
			{
				o.Draw(control);
				currentObjectIndex++;
			}
		}

		public override void Draw(GL_ControlLegacy control)
		{
			currentScene = this;
			currentObjectIndex = 1;
			foreach (EditableObject o in objects)
			{
				if (o.Visible)
					o.Draw(control);
				currentObjectIndex++;
			}
			foreach (AbstractGlDrawable o in staticObjects)
			{
				o.Draw(control);
				currentObjectIndex++;
			}
		}

		public override void Prepare(GL_ControlModern control)
		{
			this.control = control;
			foreach (EditableObject o in objects)
				o.Prepare(control);
			foreach (AbstractGlDrawable o in staticObjects)
				o.Prepare(control);
		}

		public override void Prepare(GL_ControlLegacy control)
		{
			this.control = control;
			foreach (EditableObject o in objects)
				o.Prepare(control);
			foreach (AbstractGlDrawable o in staticObjects)
				o.Prepare(control);
		}

		public override void DrawPicking(GL_ControlModern control)
		{
			foreach (EditableObject o in objects)
			{
				if (o.Visible)
					o.DrawPicking(control);
				else
					control.skipPickingColors((uint)o.GetPickableSpan());
			}
			foreach (AbstractGlDrawable o in staticObjects)
				o.DrawPicking(control);
		}

		public override void DrawPicking(GL_ControlLegacy control)
		{
			foreach (EditableObject o in objects)
			{
				if (o.Visible)
					o.DrawPicking(control);
				else
					control.skipPickingColors((uint)o.GetPickableSpan());
			}
			foreach (AbstractGlDrawable o in staticObjects)
				o.DrawPicking(control);
		}

		public override uint MouseDown(MouseEventArgs e, I3DControl control)
		{
			currentScene = this;
			currentObjectIndex = 0;
			uint var = 0;
			if (dragObj.IsNone() && e.Button == MouseButtons.Left && selectedObjects.ContainsKey(hovered))
			{
				dragObj = hovered;
				draggingDepth = control.PickingDepth;
			}
			foreach (EditableObject o in objects)
			{
				var |= o.MouseDown(e, control);
				currentObjectIndex++;
			}
			foreach (AbstractGlDrawable o in staticObjects)
			{
				var |= o.MouseDown(e, control);
				currentObjectIndex++;
			}
			return var;
		}

		public override uint MouseMove(MouseEventArgs e, Point lastMousePos, I3DControl control)
		{
			currentScene = this;
			currentObjectIndex = 0;
			uint var = 0;

			foreach (EditableObject o in objects)
			{
				var |= o.MouseMove(e, lastMousePos, control);
				currentObjectIndex++;
			}
			foreach (AbstractGlDrawable o in staticObjects)
			{
				var |= o.MouseMove(e, lastMousePos, control);
				currentObjectIndex++;
			}

			if (!dragObj.IsNone())
			{
				Vector3 Translate = new Vector3();

				//code from Whitehole

				float deltaX = e.X - control.DragStartPos.X;
				float deltaY = e.Y - control.DragStartPos.Y;

				deltaX *= draggingDepth * control.FactorX;
				deltaY *= draggingDepth * control.FactorY;

				Translate += Vector3.UnitX * deltaX * (float)Math.Cos(control.CamRotX);
				Translate -= Vector3.UnitX * deltaY * (float)Math.Sin(control.CamRotX) * (float)Math.Sin(control.CamRotY);
				Translate -= Vector3.UnitY * deltaY * (float)Math.Cos(control.CamRotY);
				Translate += Vector3.UnitZ * deltaX * (float)Math.Sin(control.CamRotX);
				Translate += Vector3.UnitZ * deltaY * (float)Math.Cos(control.CamRotX) * (float)Math.Sin(control.CamRotY);
				
				foreach(KeyValuePair< ObjID, SelectInfo> o in selectedObjects)
				{
					objects[o.Key.ObjectIndex].Translate(o.Value.LastPos, Translate, o.Key.SubObjectIndex);
				}
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
			currentScene = this;
			currentObjectIndex = 0;
			uint var = 0;

			if (!dragObj.IsNone())
			{
				foreach (ObjID o in selectedObjects.Keys.ToList())
				{
					selectedObjects[o] = new SelectInfo(objects[o.ObjectIndex].getPosition(o.SubObjectIndex));
				}
			}

			foreach (EditableObject o in objects)
			{
				var |= o.MouseUp(e, control);
				currentObjectIndex++;
			}
			foreach (AbstractGlDrawable o in staticObjects)
			{
				var |= o.MouseUp(e, control);
				currentObjectIndex++;
			}
			dragObj = ObjID.None;
			return var;
		}

		public override uint MouseClick(MouseEventArgs e, I3DControl control)
		{
			currentScene = this;
			currentObjectIndex = 0;
			uint var = 0;
			foreach (EditableObject o in objects)
			{
				var |= o.MouseClick(e, control);
				currentObjectIndex++;
			}
			foreach (AbstractGlDrawable o in staticObjects)
			{
				var |= o.MouseClick(e, control);
				currentObjectIndex++;
			}

			if (!(e.Button == MouseButtons.Left))
				return var;

			if (!(multiSelect && OpenTK.Input.Keyboard.GetState().IsKeyDown(OpenTK.Input.Key.ShiftLeft)))
			{
				bool hoveringOverEditable = (!hovered.IsNone() && hovered.ObjectIndex < objects.Count);
				if (multiSelect)
				{
					if (!selectedObjects.ContainsKey(hovered))
					{
						foreach (ObjID selected in selectedObjects.Keys.ToArray())
						{
							objects[selected.ObjectIndex].Deselect(selected.SubObjectIndex, control);
						}
					}

					if (hoveringOverEditable && !selectedObjects.ContainsKey(hovered))
					{
						selectedObjects.Clear();
						selectedObjects.Add(hovered, new SelectInfo(objects[hovered.ObjectIndex].getPosition(hovered.SubObjectIndex)));
						objects[hovered.ObjectIndex].Select(hovered.SubObjectIndex, control);
						SelectionChanged?.Invoke(this, new EventArgs());
					}
					else if(!selectedObjects.ContainsKey(hovered))
					{
						selectedObjects.Clear();
						SelectionChanged?.Invoke(this, new EventArgs());
					}
				}
				else
				{
					foreach (ObjID selected in selectedObjects.Keys.ToArray())
					{
						objects[selected.ObjectIndex].Deselect(selected.SubObjectIndex, control);
					}

					if (hoveringOverEditable && !selectedObjects.ContainsKey(hovered))
					{
						selectedObjects.Clear();
						selectedObjects.Add(hovered, new SelectInfo(objects[hovered.ObjectIndex].getPosition(hovered.SubObjectIndex)));
						objects[hovered.ObjectIndex].Select(hovered.SubObjectIndex, control);
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
				if (selectedObjects.ContainsKey(hovered))
				{
					selectedObjects.Remove(hovered);
					objects[hovered.ObjectIndex].Deselect(hovered.SubObjectIndex, control);
					SelectionChanged?.Invoke(this, new EventArgs());
				}
				else if(!hovered.IsNone() && hovered.ObjectIndex < objects.Count)
				{
					selectedObjects.Add(hovered, new SelectInfo(objects[hovered.ObjectIndex].getPosition(hovered.SubObjectIndex)));
					objects[hovered.ObjectIndex].Select(hovered.SubObjectIndex, control);
					SelectionChanged?.Invoke(this, new EventArgs());
				}
			}
			var |= REDRAW;

			return var;
		}

		public override uint MouseWheel(MouseEventArgs e, I3DControl control)
		{
			currentScene = this;
			currentObjectIndex = 0;
			uint var = 0;
			foreach (EditableObject o in objects) {
				var |= o.MouseWheel(e, control);
				currentObjectIndex++;
			}
			foreach (AbstractGlDrawable o in staticObjects)
			{
				var |= o.MouseWheel(e, control);
				currentObjectIndex++;
			}
			return var;
		}

		public override int GetPickableSpan()
		{
			int var = 0;
			foreach (EditableObject o in objects)
				var += o.GetPickableSpan();
			foreach (AbstractGlDrawable o in staticObjects)
				var += o.GetPickableSpan();
			return var;
		}

		public override uint MouseEnter(int index, I3DControl control)
		{
			currentScene = this;
			currentObjectIndex = 0;

			int inObjectIndex = index;
			if(!dragObj.IsNone())
				return 0;
			
			foreach (EditableObject o in objects)
			{
				int span = o.GetPickableSpan();
				if (inObjectIndex >= 0 && inObjectIndex < span)
				{
					hovered = new ObjID(currentObjectIndex,inObjectIndex);
					return o.MouseEnter(inObjectIndex, control) | REDRAW;
				}
				inObjectIndex -= span;
				currentObjectIndex++;
			}

			foreach (AbstractGlDrawable o in staticObjects)
			{
				int span = (int)o.GetPickableSpan();
				if (inObjectIndex >= 0 && inObjectIndex < span)
				{
					hovered = new ObjID(currentObjectIndex, inObjectIndex);
					return o.MouseEnter(inObjectIndex, control);
				}
				inObjectIndex -= span;
				currentObjectIndex++;
			}
			return 0;
		}

		public override uint MouseLeave(int index, I3DControl control)
		{
			currentObjectIndex = 0;
			int inObjectIndex = index;
			foreach (EditableObject o in objects)
			{
				int span = o.GetPickableSpan();
				if (inObjectIndex >= 0 && inObjectIndex < span)
				{
					return o.MouseLeave(inObjectIndex, control);
				}
				inObjectIndex -= span;
				currentObjectIndex++;
			}

			foreach (AbstractGlDrawable o in staticObjects)
			{
				int span = (int)o.GetPickableSpan();
				if (inObjectIndex >= 0 && inObjectIndex < span)
				{
					return o.MouseLeave(inObjectIndex, control);
				}
				inObjectIndex -= span;
				currentObjectIndex++;
			}
			return 0;
		}

		public override uint MouseLeaveEntirely(I3DControl control)
		{
			hovered = ObjID.None;
			return REDRAW;
		}

		public override uint KeyDown(KeyEventArgs e, I3DControl control)
		{
			currentScene = this;
			currentObjectIndex = 0;
			uint var = 0;
			if(e.KeyCode == Keys.Z && selectedObjects.Count>0)
			{
				OpenTK.Vector3 sum = new OpenTK.Vector3();
				int index = 0;
				foreach (ObjID objID in selectedObjects.Keys)
				{
					sum -= objects[objID.ObjectIndex].getPosition(objID.SubObjectIndex);
					index++;
				}
				sum /= index;
				control.CameraTarget = sum;

				var = REDRAW_PICKING;
			}else if (e.KeyCode == Keys.H && selectedObjects.Count > 0)
			{
				foreach (ObjID objID in selectedObjects.Keys)
				{
					objects[objID.ObjectIndex].Visible = e.Shift;
				}
				var = REDRAW_PICKING;
			}
			else if (e.Control && e.KeyCode == Keys.A)
			{
				if (e.Shift)
				{
					foreach (ObjID selected in selectedObjects.Keys.ToArray())
					{
						objects[selected.ObjectIndex].Deselect(selected.SubObjectIndex, control);
					}
					selectedObjects.Clear();
					SelectionChanged?.Invoke(this, new EventArgs());
				}

				if (!e.Shift && multiSelect)
				{
					foreach (EditableObject o in objects)
					{
						foreach (int subObj in o.getAllSelection())
						{
							ObjID obj = new ObjID(currentObjectIndex, subObj);
							if (!selectedObjects.ContainsKey(obj))
							{
								selectedObjects.Add(obj, new SelectInfo(objects[currentObjectIndex].getPosition(subObj)));
								objects[obj.ObjectIndex].Select(obj.SubObjectIndex, control);
							}
						}
						currentObjectIndex++;
					}
					SelectionChanged?.Invoke(this, new EventArgs());
				}
				var = REDRAW;
			}

				currentObjectIndex = 0;

			foreach (EditableObject o in objects) {
				var |= o.KeyDown(e, control);
				currentObjectIndex++;
			}
			foreach (AbstractGlDrawable o in staticObjects)
			{
				var |= o.KeyDown(e, control);
				currentObjectIndex++;
			}
			return var;
		}

		public override uint KeyUp(KeyEventArgs e, I3DControl control)
		{
			currentScene = this;
			currentObjectIndex = 0;
			uint var = 0;
			foreach (EditableObject o in objects) {
				var |= o.KeyUp(e, control);
				currentObjectIndex++;
			}
			foreach (AbstractGlDrawable o in staticObjects)
			{
				var |= o.KeyUp(e, control);
				currentObjectIndex++;
			}
			return var;
		}

		public struct SelectInfo
		{
			public Vector3 LastPos;

			public SelectInfo(Vector3 LastPos)
			{
				this.LastPos = LastPos;
			}
		}

		public struct ObjID : IEquatable<ObjID>
		{
			public int ObjectIndex;
			public int SubObjectIndex;

			public static readonly ObjID None = new ObjID(-1, -1);

			public bool IsNone()
			{
				return (ObjectIndex == -1) || (SubObjectIndex == -1);
			}

			public ObjID(int ObjectIndex, int SubObjectIndex)
			{
				this.ObjectIndex = ObjectIndex;
				this.SubObjectIndex = SubObjectIndex;
			}

			public bool Equals(ObjID other)
			{
				return (ObjectIndex == other.ObjectIndex)&&(SubObjectIndex==other.SubObjectIndex);
			}

			public override int GetHashCode()
			{
				return (ObjectIndex << 32) + SubObjectIndex;
			}
		}
	}
}
