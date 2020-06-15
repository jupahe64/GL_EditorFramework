using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinInput = System.Windows.Input;
using static GL_EditorFramework.EditorDrawables.EditorSceneBase;

namespace GL_EditorFramework.EditorDrawables
{
    /// <summary>
    /// An EditableObject that has only one selectable Part. It's represented by a blue block
    /// </summary>
    public class SingleObject : EditableObject
    {

        [PropertyCapture.Undoable]
        public Vector3 Position { get; set; } = new Vector3(0, 0, 0);

        public virtual Vector3 GlobalPosition { get => Position; set => Position = value; }

        protected bool Selected = false;

        public override bool IsSelected(int partIndex) => Selected;

        protected virtual Vector4 Color => new Vector4(0f, 0.25f, 1f, 1f);

        protected virtual float BoxScale => 0.5f;

        public SingleObject(Vector3 pos)
        {
            Renderers.ColorBlockRenderer.Initialize();

            Position = pos;
        }

        public override void Draw(GL_ControlModern control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            if (!ObjectRenderState.ShouldBeDrawn(this))
                return;

            bool hovered = editorScene.Hovered == this;

            control.UpdateModelMatrix(Matrix4.CreateScale(BoxScale) *
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

        public override void Draw(GL_ControlModern control, Pass pass)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            control.UpdateModelMatrix(Matrix4.CreateScale(BoxScale) *
                Matrix4.CreateTranslation(Position));

            Renderers.ColorBlockRenderer.Draw(control, pass, Color, Color, control.NextPickingColor());

        }

        public override void Draw(GL_ControlLegacy control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            if (!ObjectRenderState.ShouldBeDrawn(this))
                return;

            bool hovered = editorScene.Hovered == this;

            control.UpdateModelMatrix(Matrix4.CreateScale(BoxScale) *
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

        public override void Draw(GL_ControlLegacy control, Pass pass)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            control.UpdateModelMatrix(Matrix4.CreateScale(BoxScale) *
                Matrix4.CreateTranslation(Position));

            Renderers.ColorBlockRenderer.Draw(control, pass, Color, Color, control.NextPickingColor());

        }

        public virtual void Translate(Vector3 lastPos, Vector3 translate, int subObj)
        {
            Position = lastPos + translate;
        }

        public virtual void UpdatePosition(int subObj)
        {
            
        }

        public override void StartDragging(DragActionType actionType, int hoveredPart, EditorSceneBase scene)
        {
            if(Selected)
                scene.StartTransformAction(new LocalOrientation(GlobalPosition, Matrix3.Identity), actionType);
        }

        public override void GetSelectionBox(ref BoundingBox boundingBox)
        {
            if (!Selected)
                return;

            boundingBox.Include(new BoundingBox(
                GlobalPosition.X - BoxScale,
                GlobalPosition.X + BoxScale,
                GlobalPosition.Y - BoxScale,
                GlobalPosition.Y + BoxScale,
                GlobalPosition.Z - BoxScale,
                GlobalPosition.Z + BoxScale
            ));
        }

        public override bool IsInRange(float range, float rangeSquared, Vector3 pos)
        {
            return (pos - GlobalPosition).LengthSquared < rangeSquared;
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
        }

        public override void ApplyTransformActionToSelection(AbstractTransformAction transformAction, ref TransformChangeInfos infos)
        {
            if (!Selected)
                return;

            Vector3 pp = Position;

            var newPos = transformAction.NewPos(GlobalPosition, out bool posHasChanged);

            if(posHasChanged)
            {
                GlobalPosition = newPos;
                infos.Add(this, 0, pp, null, null);
            }
        }

        public override void DeleteSelected(EditorSceneBase scene, DeletionManager manager, IList list)
        {
            if (Selected)
                manager.Add(list, this);
        }

        public override bool TrySetupObjectUIControl(EditorSceneBase scene, ObjectUIControl objectUIControl)
        {
            if (!Selected)
                return false;
            objectUIControl.AddObjectUIContainer(new PropertyProvider(this, scene), "Transform");
            return true;
        }

        public override bool IsSelectedAll()
        {
            return Selected;
        }

        public override bool IsSelected()
        {
            return Selected;
        }

        public override Vector3 GetFocusPoint()
        {
            return GlobalPosition;
        }

        public class PropertyProvider : IObjectUIContainer
        {
            PropertyCapture? capture = null;

            SingleObject obj;
            EditorSceneBase scene;
            public PropertyProvider(SingleObject obj, EditorSceneBase scene)
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
