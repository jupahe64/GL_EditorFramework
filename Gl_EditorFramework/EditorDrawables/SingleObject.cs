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

        public static System.Reflection.FieldInfo FI_Position => typeof(SingleObject).GetField("Position");
        public Vector3 Position = new Vector3(0, 0, 0);

        protected bool Selected = false;

        public override bool IsSelected() => Selected;

        public override bool IsSelected(int partIndex) => Selected;

        protected static Vector4 Color = new Vector4(0f, 0.25f, 1f, 1f);

        

        public SingleObject(Vector3 pos)
        {
            Position = pos;
        }

        public override void Draw(GL_ControlModern control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            if (!editorScene.ShouldBeDrawn(this))
                return;

            bool hovered = editorScene.Hovered == this;

            control.UpdateModelMatrix(Matrix4.CreateScale(0.5f) *
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

        public override void Draw(GL_ControlModern control, Pass pass)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            control.UpdateModelMatrix(Matrix4.CreateScale(0.5f) *
                Matrix4.CreateTranslation(Position));

            Renderers.ColorBlockRenderer.Draw(control, pass, Color, Color, control.NextPickingColor());

        }

        public override void Draw(GL_ControlLegacy control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            if (!editorScene.ShouldBeDrawn(this))
                return;

            bool hovered = editorScene.Hovered == this;

            control.UpdateModelMatrix(Matrix4.CreateScale(0.5f) *
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

        public override void Draw(GL_ControlLegacy control, Pass pass)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            control.UpdateModelMatrix(Matrix4.CreateScale(0.5f) *
                Matrix4.CreateTranslation(Position));

            Renderers.ColorBlockRenderer.Draw(control, pass, Color, Color, control.NextPickingColor());

        }

        public override void Prepare(GL_ControlModern control)
        {
            Renderers.ColorBlockRenderer.Initialize(control);
        }

        public override void Prepare(GL_ControlLegacy control)
        {
            Renderers.ColorBlockRenderer.Initialize(control);
        }

        public virtual void Translate(Vector3 lastPos, Vector3 translate, int subObj)
        {
            Position = lastPos + translate;
        }

        public virtual void UpdatePosition(int subObj)
        {
            
        }
        
        public override bool TryStartDragging(DragActionType actionType, int hoveredPart, out LocalOrientation localOrientation, out bool dragExclusively)
        {
            localOrientation = new LocalOrientation(Position);
            dragExclusively = false;
            return Selected;
        }

        public override void GetSelectionBox(ref BoundingBox boundingBox)
        {
            if (!Selected)
                return;

            boundingBox.Include(new BoundingBox(
                Position.X - 0.5f,
                Position.X + 0.5f,
                Position.Y - 0.5f,
                Position.Y + 0.5f,
                Position.Z - 0.5f,
                Position.Z + 0.5f
            ));
        }

        public override LocalOrientation GetLocalOrientation(int partIndex)
        {
            return new LocalOrientation(Position);
        }

        public override bool IsInRange(float range, float rangeSquared, Vector3 pos)
        {
            return (pos - Position).LengthSquared < rangeSquared;
        }
        
        public override uint SelectAll(GL_ControlBase control, ISet<object> selectedObjects)
        {
            Selected = true;
            selectedObjects?.Add(this);
            return REDRAW;
        }

        public override uint SelectDefault(GL_ControlBase control, ISet<object> selectedObjects)
        {
            Selected = true;
            selectedObjects?.Add(this);
            return REDRAW;
        }

        public override uint Select(int partIndex, GL_ControlBase control, ISet<object> selectedObjects)
        {
            Selected = true;
            selectedObjects?.Add(this);
            return REDRAW;
        }

        public override uint Deselect(int partIndex, GL_ControlBase control, ISet<object> selectedObjects)
        {
            Selected = false;
            selectedObjects?.Remove(this);
            return REDRAW;
        }

        public override uint DeselectAll(GL_ControlBase control, ISet<object> selectedObjects)
        {
            Selected = false;
            selectedObjects?.Remove(this);
            return REDRAW;
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
        }

        public override void ApplyTransformActionToSelection(AbstractTransformAction transformAction, ref TransformChangeInfos infos)
        {
            Position = transformAction.NewPos(Position, out Vector3? prevPos);
            infos.Add(this, 0, prevPos, null, null);
        }

        public override void DeleteSelected(DeletionManager manager, IList list, IList currentList)
        {
            if (Selected)
                manager.Add(list, this);
        }

        public override bool ProvidesProperty(EditorSceneBase scene) => Selected;

        public override IObjectUIProvider GetPropertyProvider(EditorSceneBase scene) => new PropertyProvider(this,scene);

        public class PropertyProvider : IObjectUIProvider
        {
            Vector3 prevPos;

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
                prevPos = obj.Position;
            }

            public void OnValueChanged()
            {
                scene.Refresh();
            }

            public void OnValueSet()
            {
                if (prevPos != obj.Position)
                    scene.AddToUndo(new RevertableFieldChange(SingleObject.FI_Position, obj, prevPos));

                scene.Refresh();
            }

            public void UpdateProperties()
            {
                
            }
        }
    }
}
