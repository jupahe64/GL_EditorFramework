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
            Scale = scale;
        }

        public override string ToString() => "block";

        [PropertyCapture.Undoable]
        public Vector3 Rotation { get; set; } = Vector3.Zero;

        public virtual Matrix3 GlobalRotation
        {
            get => Framework.Mat3FromEulerAnglesDeg(Rotation);
            set => Rotation = value.ExtractDegreeEulerAngles() + new Vector3(
                        (float)Math.Round(Rotation.X / 360f) * 360,
                        (float)Math.Round(Rotation.Y / 360f) * 360,
                        (float)Math.Round(Rotation.Z / 360f) * 360
                        );
        }

        [PropertyCapture.Undoable]
        public Vector3 Scale { get; set; } = new Vector3(1, 1, 1);

        public virtual Vector3 GlobalScale { get => Scale; set => value = Scale; }

        public override void Draw(GL_ControlModern control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            if (!editorScene.ShouldBeDrawn(this))
                return;

            bool hovered = editorScene.Hovered == this;

            Matrix3 rotMtx = GlobalRotation;

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

            Matrix3 rotMtx = GlobalRotation;

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
            return new LocalOrientation(Position, GlobalRotation);
        }

        public override bool TryStartDragging(DragActionType actionType, int hoveredPart, out LocalOrientation localOrientation, out bool dragExclusively)
        {
            localOrientation = new LocalOrientation(Position, GlobalRotation);
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
                Scale = scale.Value;
            }
        }

        public override void ApplyTransformActionToSelection(AbstractTransformAction transformAction, ref TransformChangeInfos infos)
        {
            if (!Selected)
                return;

            Vector3 pp = Position, pr = Rotation, ps = Scale;

            GlobalPosition = transformAction.NewPos(GlobalPosition, out bool posHasChanged);

            Matrix3 rotMtx = GlobalRotation;

            GlobalRotation = transformAction.NewRot(GlobalRotation, out bool rotHasChanged);

            GlobalScale = transformAction.NewScale(GlobalScale, rotMtx, out bool scaleHasChanged);

            infos.Add(this, 0,
                posHasChanged   ? new Vector3?(pp) : new Vector3?(), 
                rotHasChanged   ? new Vector3?(pr) : new Vector3?(),
                scaleHasChanged ? new Vector3?(ps) : new Vector3?());
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
