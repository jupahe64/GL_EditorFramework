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

namespace GL_EditorFramework.EditorDrawables
{
    public class TransformableObject : SingleObject
    {
        public TransformableObject(Vector3 pos, Vector3 rot, Vector3 scale)
            : base(pos)
        {
            Rotation = rot;
            this.Scale = scale;
        }

        public override string ToString() => "block";

        [PropertyCapture.Undoable]
        public Vector3 Rotation { get; set; } = Vector3.Zero;

        [PropertyCapture.Undoable]
        public Vector3 Scale { get; set; } = new Vector3(1, 1, 1);

        public override void Draw(GL_ControlModern control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            if (!editorScene.ShouldBeDrawn(this))
                return;

            bool hovered = editorScene.Hovered == this;

            Matrix3 rotMtx = Framework.Mat3FromEulerAnglesDeg(Rotation);

            control.UpdateModelMatrix(
                Matrix4.CreateScale((Selected ? editorScene.CurrentAction.NewScale(Scale, rotMtx) : Scale) * 0.5f) *
                new Matrix4(Selected ? editorScene.CurrentAction.NewRot(rotMtx) : rotMtx) *
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

            if (!editorScene.ShouldBeDrawn(this))
                return;

            bool hovered = editorScene.Hovered == this;

            Matrix3 rotMtx = Framework.Mat3FromEulerAnglesDeg(Rotation);

            control.UpdateModelMatrix(
                Matrix4.CreateScale((Selected ? editorScene.CurrentAction.NewScale(Scale, rotMtx) : Scale) * 0.5f) *
                new Matrix4(Selected ? editorScene.CurrentAction.NewRot(rotMtx) : rotMtx) *
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
            return new LocalOrientation(Position, Framework.Mat3FromEulerAnglesDeg(Rotation));
        }

        public override bool TryStartDragging(DragActionType actionType, int hoveredPart, out LocalOrientation localOrientation, out bool dragExclusively)
        {
            localOrientation = new LocalOrientation(Position, Framework.Mat3FromEulerAnglesDeg(Rotation));
            dragExclusively = false;
            return Selected;
        }

        public override void SetTransform(Vector3? pos, Vector3? rot, Vector3? scale, int part, out Vector3? prevPos, out Vector3? prevRot, out Vector3? prevScale)
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
                prevRot = Rotation;
                Rotation = rot.Value;
            }

            if (scale.HasValue)
            {
                prevScale = this.Scale;
                this.Scale = scale.Value;
            }
        }

        public override void ApplyTransformActionToSelection(AbstractTransformAction transformAction, ref TransformChangeInfos infos)
        {
            if (!Selected)
                return;

            Position = transformAction.NewPos(Position, out Vector3? prevPos);

            Matrix3 rotMtx = Framework.Mat3FromEulerAnglesDeg(Rotation);

            Rotation = transformAction.NewRot(Rotation, out Vector3? prevRot);

            Scale = transformAction.NewScale(Scale, rotMtx, out Vector3 ? prevScale);
            infos.Add(this, 0, prevPos, prevRot, prevScale);
        }

        public override bool TrySetupObjectUIControl(EditorSceneBase scene, ObjectUIControl objectUIControl)
        {
            if (!Selected)
                return false;
            objectUIControl.AddObjectUIContainer(new PropertyProvider(this, scene), "Transform");
            return true;
        }

        public new class PropertyProvider : IObjectUIContainer
        {
            PropertyCapture? capture = null;

            TransformableObject obj;
            EditorSceneBase scene;
            public PropertyProvider(TransformableObject obj, EditorSceneBase scene)
            {
                this.obj = obj;
                this.scene = scene;
            }

            public void DoUI(IObjectUIControl control)
            {
                if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
                    obj.Position = control.Vector3Input(obj.Position, "Position", 1, 16);
                else
                    obj.Position = control.Vector3Input(obj.Position, "Position", 0.125f, 2);

                if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
                    obj.Rotation = control.Vector3Input(obj.Rotation, "Rotation", 45, 18, -180, 180, true);
                else
                    obj.Rotation = control.Vector3Input(obj.Rotation, "Rotation", 5, 2, -180, 180, true);

                if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
                    obj.Scale = control.Vector3Input(obj.Scale, "Scale", 1, 16);
                else
                    obj.Scale = control.Vector3Input(obj.Scale, "Scale", 0.125f, 2);
            }

            public void OnValueChangeStart()
            {
                capture = new PropertyCapture(obj);
            }

            public void OnValueChanged()
            {
                scene.Refresh();
            }

            public void OnValueSet()
            {
                capture?.HandleUndo(scene);
                capture = null;
                scene.Refresh();
            }

            public void UpdateProperties()
            {
                
            }
        }
    }
}
