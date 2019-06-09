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

        public override bool IsSelected(int partIndex) => Selected;

        protected static Vector4 Color = new Vector4(0f, 0.25f, 1f, 1f);

        

        public SingleObject(Vector3 pos)
        {
            position = pos;
        }

        public override void Draw(GL_ControlModern control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            bool hovered = editorScene.Hovered == this;

            control.UpdateModelMatrix(Matrix4.CreateScale(0.5f) *
                Matrix4.CreateTranslation(Selected ? editorScene.CurrentAction.NewPos(position) : position));

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
                Matrix4.CreateTranslation(position));

            Renderers.ColorBlockRenderer.Draw(control, pass, Color, Color, control.NextPickingColor());

        }

        public override void Draw(GL_ControlLegacy control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            bool hovered = editorScene.Hovered == this;

            control.UpdateModelMatrix(Matrix4.CreateScale(0.5f) *
                Matrix4.CreateTranslation(Selected ? editorScene.CurrentAction.NewPos(position) : position));

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
                Matrix4.CreateTranslation(position));

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
            position = lastPos + translate;
        }

        public virtual void UpdatePosition(int subObj)
        {
            
        }
        
        public override bool TryStartDragging(DragActionType actionType, int hoveredPart, out LocalOrientation localOrientation, out bool dragExclusively)
        {
            localOrientation = new LocalOrientation(position);
            dragExclusively = false;
            return Selected;
        }

        public override BoundingBox GetSelectionBox() => new BoundingBox(
            position.X - 0.5f,
            position.X + 0.5f,
            position.Y - 0.5f,
            position.Y + 0.5f,
            position.Z - 0.5f,
            position.Z + 0.5f
            );

        public override LocalOrientation GetLocalOrientation(int partIndex)
        {
            return new LocalOrientation(position);
        }

        public override bool IsInRange(float range, float rangeSquared, Vector3 pos)
        {
            return (pos - position).LengthSquared < rangeSquared;
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

        public override void SetTransform(Vector3? pos, Quaternion? rot, Vector3? scale, int part, out Vector3? prevPos, out Quaternion? prevRot, out Vector3? prevScale)
        {
            prevPos = null;
            prevRot = null;
            prevScale = null;

            if (pos.HasValue)
            {
                prevPos = position;
                position = pos.Value;
            }
        }

        public override void ApplyTransformActionToSelection(AbstractTransformAction transformAction, ref TransformChangeInfos infos)
        {
            position = transformAction.NewPos(position, out Vector3? prevPos);
            infos.Add(this, 0, prevPos, null, null);
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
