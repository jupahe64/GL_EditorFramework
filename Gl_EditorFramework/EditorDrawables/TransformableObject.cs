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
        public new class MultiEditUIContainer : IObjectUIContainer
        {
            private readonly List<IEditableObject> objs;

            private readonly EditorSceneBase scene;

            private readonly PropertyChanges propertyChangesAction = new PropertyChanges();

            Vector3 centerPosition;
            Vector3 changeStartCenterPosition;
            MultipleVector3Capture<TransformableObject> rotation;
            MultipleVector3Capture<TransformableObject> scale;

            public MultiEditUIContainer(List<IEditableObject> objs, EditorSceneBase scene)
            {
                this.objs = objs;
                this.scene = scene;

                UpdateProperties();
            }

            public void DoUI(IObjectUIControl _control)
            {
                var control = (IObjectUIControlWithMultipleSupport)_control;

                if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
                    centerPosition = _control.Vector3Input(centerPosition, "Center Position", 1, 16);
                else
                    centerPosition = _control.Vector3Input(centerPosition, "Center Position", 0.125f, 2);

                if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
                    rotation.Value = control.Vector3Input(rotation.Value, "Rotation", 45, 18, -180, 180, true, allowMixed: false);
                else
                    rotation.Value = control.Vector3Input(rotation.Value, "Rotation", 5, 2, -180, 180, true, allowMixed: false);

                if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
                    scale.Value = control.Vector3Input(scale.Value, "Scale", 1, 16, multiResolveValue: Vector3.One);
                else
                    scale.Value = control.Vector3Input(scale.Value, "Scale", 0.125f, 2, multiResolveValue: Vector3.One);
            }

            public void OnValueChanged()
            {
                propertyChangesAction.translation = centerPosition - changeStartCenterPosition;

                if (rotation.Value.IsAllShared)
                    propertyChangesAction.rotOverride = Framework.Mat3FromEulerAnglesDeg(rotation.Value.SharedVector);

                propertyChangesAction.scaleOverride = scale.Value;

                scene.Refresh();
            }

            public void OnValueChangeStart()
            {
                changeStartCenterPosition = centerPosition;
                propertyChangesAction.translation = Vector3.Zero;
                propertyChangesAction.rotOverride = null;
                propertyChangesAction.scaleOverride = new MultipleVector3();

                scene.SelectionTransformAction = propertyChangesAction;
            }

            public void OnValueSet()
            {
                OnValueChanged();
                scene.ApplyCurrentTransformAction();
                scene.Refresh();
            }

            public void UpdateProperties()
            {
                centerPosition = Vector3.Zero;

                for (int i = 0; i < objs.Count; i++)
                {
                    centerPosition += (objs[i] as SingleObject).Position;
                }

                centerPosition /= objs.Count;

                rotation = new MultipleVector3Capture<TransformableObject>(x => x.Rotation, (x, y) => x.Rotation = y, objs);
                scale = new MultipleVector3Capture<TransformableObject>(x => x.Scale, (x, y) => x.Scale = y, objs);
            }

            ~MultiEditUIContainer()
            {
                scene.SelectionTransformAction = NoAction;
            }
        }

        public static new void SetupUIForMultiEditing(EditorSceneBase scene, ObjectUIControl control, List<IEditableObject> objects)
        {
            control.AddObjectUIContainer(new MultiEditUIContainer(objects, scene), "General");
        }

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

        public virtual Vector3 GlobalScale { get => Scale; set => Scale = value; }

        public override void Draw(GL_ControlModern control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            if (!ObjectRenderState.ShouldBeDrawn(this))
                return;

            bool hovered = editorScene.Hovered == this;

            Matrix3 rotMtx = GlobalRotation;

            control.UpdateModelMatrix(
                Matrix4.CreateScale((Selected ? editorScene.SelectionTransformAction.NewScale(Scale, rotMtx) : Scale) * BoxScale) *
                new Matrix4(Selected ? editorScene.SelectionTransformAction.NewRot(rotMtx) : rotMtx) *
                Matrix4.CreateTranslation(Selected ? editorScene.SelectionTransformAction.NewPos(Position) : Position));

            Vector4 blockColor;
            Vector4 lineColor;

            if (hovered && Selected)
                lineColor = hoverSelectColor;
            else if (Selected)
                lineColor = selectColor;
            else if (hovered)
                lineColor = hoverColor;
            else
                lineColor = Color;

            if (hovered && Selected)
                blockColor = Color * 0.5f + hoverSelectColor * 0.5f;
            else if (Selected)
                blockColor = Color * 0.5f + selectColor * 0.5f;
            else if (hovered)
                blockColor = Color * 0.5f + hoverColor * 0.5f;
            else
                blockColor = Color;

            Renderers.ColorBlockRenderer.Draw(control, pass, blockColor, lineColor, control.NextPickingColor());

        }

        public override void Draw(GL_ControlLegacy control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            if (!ObjectRenderState.ShouldBeDrawn(this))
                return;

            bool hovered = editorScene.Hovered == this;

            Matrix3 rotMtx = GlobalRotation;

            control.UpdateModelMatrix(
                Matrix4.CreateScale((Selected ? editorScene.SelectionTransformAction.NewScale(Scale, rotMtx) : Scale) * BoxScale) *
                new Matrix4(Selected ? editorScene.SelectionTransformAction.NewRot(rotMtx) : rotMtx) *
                Matrix4.CreateTranslation(Selected ? editorScene.SelectionTransformAction.NewPos(Position) : Position));

            Vector4 blockColor;
            Vector4 lineColor;

            if (hovered && Selected)
                lineColor = hoverSelectColor;
            else if (Selected)
                lineColor = selectColor;
            else if (hovered)
                lineColor = hoverColor;
            else
                lineColor = Color;

            if (hovered && Selected)
                blockColor = Color * 0.5f + hoverSelectColor * 0.5f;
            else if (Selected)
                blockColor = Color * 0.5f + selectColor * 0.5f;
            else if (hovered)
                blockColor = Color * 0.5f + hoverColor * 0.5f;
            else
                blockColor = Color;

            Renderers.ColorBlockRenderer.Draw(control, pass, blockColor, lineColor, control.NextPickingColor());
        }

        public override void StartDragging(DragActionType actionType, int hoveredPart, EditorSceneBase scene)
        {
            if (Selected)
                scene.StartTransformAction(new LocalOrientation(GlobalPosition, GlobalRotation), actionType);
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

            var newPos = transformAction.NewPos(GlobalPosition, out bool posHasChanged);

            Matrix3 rotMtx = GlobalRotation;

            var newRot = transformAction.NewRot(GlobalRotation, out bool rotHasChanged);

            var newScale = transformAction.NewScale(GlobalScale, rotMtx, out bool scaleHasChanged);

            if (posHasChanged)
                GlobalPosition = newPos;
            if (rotHasChanged)
                GlobalRotation = newRot;
            if (scaleHasChanged)
                GlobalScale = newScale;

            infos.Add(this, 0,
                posHasChanged   ? new Vector3?(pp) : null, 
                rotHasChanged   ? new Vector3?(pr) : null,
                scaleHasChanged ? new Vector3?(ps) : null);
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
