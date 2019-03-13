using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GL_EditorFramework.EditorDrawables;
using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using GL_EditorFramework;
using OpenTK;
using static GL_EditorFramework.EditorDrawables.EditorSceneBase;

namespace Testing
{
	class TransformableObject : SingleObject
	{
		public TransformableObject(Vector3 pos)
			: base(pos)
		{

		}

		public Quaternion rotation = Quaternion.Identity;

		public Vector3 scale = new Vector3(1, 1, 1);

		public override Quaternion Rotation { get => rotation; set => rotation = value; }

		public override Vector3 Scale { get => scale; set => scale = value; }

		public override void Draw(GL_ControlModern control, Pass pass, EditorSceneBase editorScene)
		{
			if (pass == Pass.TRANSPARENT)
				return;

			bool hovered = editorScene.hovered == this;

			control.UpdateModelMatrix(
				Matrix4.CreateScale((Selected ? editorScene.currentAction.scale * scale : scale) * 0.5f) *
				Matrix4.CreateFromQuaternion(Selected ?  editorScene.currentAction.deltaRotation * Rotation : Rotation) *
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

		public override void Draw(GL_ControlLegacy control, Pass pass, EditorSceneBase editorScene)
		{
			if (pass == Pass.TRANSPARENT)
				return;

			bool hovered = editorScene.hovered == this;

			control.UpdateModelMatrix(
				Matrix4.CreateScale(        (Selected ? editorScene.currentAction.scale * scale : scale) * 0.5f) *
				Matrix4.CreateFromQuaternion(Selected ? editorScene.currentAction.deltaRotation * Rotation : Rotation) *
				Matrix4.CreateTranslation(   Selected ? editorScene.currentAction.newPos(position) : position));

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

		public override void ApplyTransformActionToSelection(AbstractTransformAction transformAction)
		{
			position = transformAction.newPos(position);
			rotation = transformAction.deltaRotation * rotation;
			scale = transformAction.scale * scale;
		}
	}
}
