using Gl_EditorFramework;
using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static GL_EditorFramework.EditorDrawables.EditorSceneBase;

namespace GL_EditorFramework.EditorDrawables
{
	//
	// Summary:
	//     An EditableObject that has only one selectable Part. It's represented by a blue block
	public class SingleObject : EditableObject
	{
		protected Vector3 position = new Vector3(0, 0, 0);

		protected bool Selected = false;

		public override bool IsSelected() => Selected;

		protected static Vector4 Color = new Vector4(0f, 0.25f, 1f, 1f);

		

		public SingleObject(Vector3 pos)
		{
			position = pos;
		}

		public override void Draw(GL_ControlModern control, Pass pass, EditorSceneBase editorScene)
		{
			if (pass == Pass.TRANSPARENT)
				return;

			bool hovered = editorScene.hovered == this;

			control.UpdateModelMatrix(Matrix4.CreateScale(0.5f) *
				Matrix4.CreateTranslation(Selected ? editorScene.currentAction.newPos(position) : position));

			Vector4 blockColor;
			Vector4 lineColor;

			if (hovered && Selected)
				lineColor = hoverColor;
			else if (hovered || Selected)
				lineColor = selectColor;
			else
				lineColor = Color;

			if (hovered && Selected)
				blockColor = Color * 0.5f + hoverColor * 0.5f;
			else if (hovered || Selected)
				blockColor = Color * 0.5f + selectColor * 0.5f;
			else
				blockColor = Color;

			Renderers.ColorBlockRenderer.Draw(control, pass, blockColor, lineColor, control.nextPickingColor());

		}

		public override void Draw(GL_ControlModern control, Pass pass)
		{
			if (pass == Pass.TRANSPARENT)
				return;

			control.UpdateModelMatrix(Matrix4.CreateScale(0.5f) *
				Matrix4.CreateTranslation(position));

			Renderers.ColorBlockRenderer.Draw(control, pass, Color, Color, control.nextPickingColor());

		}

		public override void Draw(GL_ControlLegacy control, Pass pass, EditorSceneBase editorScene)
		{
			if (pass == Pass.TRANSPARENT)
				return;

			bool hovered = editorScene.hovered == this;

			control.UpdateModelMatrix(Matrix4.CreateScale(0.5f) *
				Matrix4.CreateTranslation(Selected ? editorScene.currentAction.newPos(position) : position));

			Vector4 blockColor;
			Vector4 lineColor;

			if (hovered && Selected)
				lineColor = hoverColor;
			else if (hovered || Selected)
				lineColor = selectColor;
			else
				lineColor = Color;

			if (hovered && Selected)
				blockColor = Color * 0.5f + hoverColor * 0.5f;
			else if (hovered || Selected)
				blockColor = Color * 0.5f + selectColor * 0.5f;
			else
				blockColor = Color;

			Renderers.ColorBlockRenderer.Draw(control, pass, blockColor, lineColor, control.nextPickingColor());
		}

		public override void Draw(GL_ControlLegacy control, Pass pass)
		{
			if (pass == Pass.TRANSPARENT)
				return;

			control.UpdateModelMatrix(Matrix4.CreateScale(0.5f) *
				Matrix4.CreateTranslation(position));

			Renderers.ColorBlockRenderer.Draw(control, pass, Color, Color, control.nextPickingColor());

		}

		public override void Prepare(GL_ControlModern control)
		{
			Renderers.ColorBlockRenderer.Initialize();
		}

		public override void Prepare(GL_ControlLegacy control)
		{
			
		}

		public virtual void Translate(Vector3 lastPos, Vector3 translate, int subObj)
		{
			position = lastPos + translate;
		}

		public virtual void UpdatePosition(int subObj)
		{
			
		}

		public override bool CanStartDragging() => true;

		public override Vector3 GetSelectionCenter()
		{
			return position;
		}

		public override uint SelectAll(GL_ControlBase control)
		{
			Selected = true;
			return REDRAW;
		}

		public override uint SelectDefault(GL_ControlBase control)
		{
			Selected = true;
			return REDRAW;
		}

		public override uint Select(int partIndex, GL_ControlBase control)
		{
			Selected = true;
			return REDRAW;
		}

		public override uint Deselect(int partIndex, GL_ControlBase control)
		{
			Selected = false;
			return REDRAW;
		}

		public override uint DeselectAll(GL_ControlBase control)
		{
			Selected = false;
			return REDRAW;
		}

		public override void ApplyTransformActionToSelection(AbstractTransformAction transformAction)
		{
			position = transformAction.newPos(position);
		}

		public override Vector3 Position
		{
			get
			{
				return position;
			}
			set
			{
				position = value;
			}
		}
	}
}
