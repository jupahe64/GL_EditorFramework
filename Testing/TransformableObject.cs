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
using WinInput = System.Windows.Input;
using static GL_EditorFramework.EditorDrawables.EditorSceneBase;

namespace Testing
{
    class TransformableObject : SingleObject
    {
        public TransformableObject(Vector3 pos, Quaternion rot, Vector3 scale)
            : base(pos)
        {
            rotation = rot;
            this.scale = scale;
        }

        public override string ToString() => "block";

        public static System.Reflection.FieldInfo FI_Rotation => typeof(TransformableObject).GetField("rotation");
        public Quaternion rotation = Quaternion.Identity;

        public static System.Reflection.FieldInfo FI_Scale => typeof(TransformableObject).GetField("scale");
        public Vector3 scale = new Vector3(1, 1, 1);

        public override void Draw(GL_ControlModern control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            bool hovered = editorScene.Hovered == this;

            control.UpdateModelMatrix(
                Matrix4.CreateScale((Selected ? editorScene.CurrentAction.NewScale(scale) : scale) * 0.5f) *
                Matrix4.CreateFromQuaternion(Selected ? editorScene.CurrentAction.NewRot(rotation) : rotation) *
                Matrix4.CreateTranslation(Selected ? editorScene.CurrentAction.NewPos(Position) : Position));

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

            Renderers.ColorBlockRenderer.Draw(control, pass, blockColor, lineColor, control.NextPickingColor());

        }

        public override void Draw(GL_ControlLegacy control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            bool hovered = editorScene.Hovered == this;

            control.UpdateModelMatrix(
                Matrix4.CreateScale((Selected ? editorScene.CurrentAction.NewScale(scale) : scale) * 0.5f) *
                Matrix4.CreateFromQuaternion(Selected ? editorScene.CurrentAction.NewRot(rotation) : rotation) *
                Matrix4.CreateTranslation(Selected ? editorScene.CurrentAction.NewPos(Position) : Position));

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

            Renderers.ColorBlockRenderer.Draw(control, pass, blockColor, lineColor, control.NextPickingColor());
        }

        public override LocalOrientation GetLocalOrientation(int partIndex)
        {
            return new LocalOrientation(Position, rotation);
        }

        public override bool TryStartDragging(DragActionType actionType, int hoveredPart, out LocalOrientation localOrientation, out bool dragExclusively)
        {
            localOrientation = new LocalOrientation(Position,rotation);
            dragExclusively = false;
            return Selected;
        }

        public override void SetTransform(Vector3? pos, Quaternion? rot, Vector3? scale, int part, out Vector3? prevPos, out Quaternion? prevRot, out Vector3? prevScale)
        {
            prevPos = null;
            prevRot = null;
            prevScale = null;

            if (pos.HasValue)
            {
                prevPos = Position;
                Position = pos.Value;
            }

            if (rot.HasValue)
            {
                prevRot = rotation;
                rotation = rot.Value;
            }

            if (scale.HasValue)
            {
                prevScale = this.scale;
                this.scale = scale.Value;
            }
        }

        public override void ApplyTransformActionToSelection(AbstractTransformAction transformAction, ref TransformChangeInfos infos)
        {
            Position = transformAction.NewPos(Position, out Vector3? prevPos);
            rotation = transformAction.NewRot(rotation, out Quaternion? prevRot);
            scale = transformAction.NewScale(scale, out Vector3? prevScale);
            infos.Add(this, 0, prevPos, prevRot, prevScale);
        }

        public override IPropertyProvider GetPropertyProvider(EditorSceneBase scene) => new PropertyProvider(this, scene);

        public new class PropertyProvider : IPropertyProvider
        {
            Vector3 prevPos;
            Vector3 rot;
            Vector3 prevRot;
            Quaternion prevRotQ;
            Vector3 prevScale;

            TransformableObject obj;
            EditorSceneBase scene;
            public PropertyProvider(TransformableObject obj, EditorSceneBase scene)
            {
                this.obj = obj;
                this.scene = scene;
                rot = obj.rotation.ToEulerAnglesDeg();
            }

            public void DoUI(IObjectPropertyControl control)
            {
                if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
                    obj.Position = control.Vector3Input(obj.Position, "Position", 1, 16);
                else
                    obj.Position = control.Vector3Input(obj.Position, "Position", 0.125f, 2);

                if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
                    rot = control.Vector3Input(rot, "Rotation", 45, 18);
                else
                    rot = control.Vector3Input(rot, "Rotation", 5, 2);

                if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
                    obj.scale = control.Vector3Input(obj.scale, "Scale", 1, 16);
                else
                    obj.scale = control.Vector3Input(obj.scale, "Scale", 0.125f, 2);
            }

            public void OnValueChangeStart()
            {
                prevPos = obj.Position;
                prevRot = rot;
                prevRotQ = obj.rotation;
                prevScale = obj.scale;
            }

            public void OnValueChanged()
            {
                obj.rotation = Framework.QFromEulerAnglesDeg(rot);

                scene.Refresh();
            }

            public void OnValueSet()
            {
                obj.rotation = Framework.QFromEulerAnglesDeg(rot);

                if (prevPos != obj.Position)
                    scene.AddToUndo(new RevertableFieldChange(SingleObject.FI_Position, obj, prevPos));
                if (prevRot != rot)
                    scene.AddToUndo(new RevertableFieldChange(TransformableObject.FI_Rotation, obj, prevRotQ));
                if (prevScale != obj.scale)
                    scene.AddToUndo(new RevertableFieldChange(TransformableObject.FI_Scale, obj, prevScale));

                scene.Refresh();
            }

            public void UpdateProperties()
            {
                rot = obj.rotation.ToEulerAnglesDeg();
            }
        }
    }
}
