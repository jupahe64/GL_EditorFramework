using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static GL_EditorFramework.EditorDrawables.EditorSceneBase;

namespace GL_EditorFramework.EditorDrawables
{
    public abstract class EditableObject : AbstractGlDrawable
    {
        public static Vector4 hoverColor = new Vector4(1, 1, 0.925f,1);
        public static Vector4 selectColor = new Vector4(1, 1, 0.675f, 1);
        
        [Browsable(false)]
        public bool Visible = true;
        
        public EditableObject()
        {

        }

        public abstract bool TryStartDragging(DragActionType actionType, int hoveredPart, out LocalOrientation localOrientation, out bool dragExclusively);

        public abstract bool IsSelected();

        public abstract bool IsSelected(int partIndex);

        public abstract BoundingBox GetSelectionBox();

        public abstract LocalOrientation GetLocalOrientation(int partIndex);

        public abstract bool IsInRange(float range, float rangeSquared, Vector3 pos);

        public abstract uint SelectAll(GL_ControlBase control);

        public abstract uint SelectDefault(GL_ControlBase control);
        
        public virtual void Draw(GL_ControlModern control, Pass pass, EditorSceneBase editorScene)
        {
            
        }

        public virtual void Draw(GL_ControlLegacy control, Pass pass, EditorSceneBase editorScene)
        {

        }

        public abstract uint Select(int partIndex, GL_ControlBase control);

        public abstract uint Deselect(int partIndex, GL_ControlBase control);
        public abstract uint DeselectAll(GL_ControlBase control);

        public virtual void SetTransform(Vector3? pos, Quaternion? rot, Vector3? scale, int part, out Vector3? prevPos, out Quaternion? prevRot, out Vector3? prevScale)
        {
            prevPos = null;
            prevRot = null;
            prevScale = null;
        }

        public virtual void ApplyTransformActionToSelection(AbstractTransformAction transformAction, ref TransformChangeInfos transformChangeInfos)
        {
            
        }

        public virtual void ApplyTransformActionToPart(AbstractTransformAction transformAction, int part, ref TransformChangeInfos transformChangeInfos)
        {

        }

        public virtual Vector3 Position
        {
            get
            {
                return Vector3.Zero;
            }
            set
            {

            }
        }

        public virtual Quaternion Rotation
        {
            get
            {
                return Quaternion.Identity;
            }
            set
            {

            }
        }

        public virtual Vector3 Scale
        {
            get
            {
                return Vector3.One;
            }
            set
            {

            }
        }

        public struct BoundingBox
        {
            public float minX;
            public float maxX;
            public float minY;
            public float maxY;
            public float minZ;
            public float maxZ;

            public static BoundingBox Default = new BoundingBox(float.MaxValue, float.MinValue, float.MaxValue, float.MinValue, float.MaxValue, float.MinValue);

            public BoundingBox(float minX, float maxX, float minY, float maxY, float minZ, float maxZ)
            {				   
                this.minX = minX;
                this.maxX = maxX;
                this.minY = minY;
                this.maxY = maxY;
                this.minZ = minZ;
                this.maxZ = maxZ;
            }

            public void Include(Vector3 vec)
            {
                if (vec.X < minX)
                    minX = vec.X;
                if (vec.X > maxX)
                    maxX = vec.X;

                if (vec.Y < minY)
                    minY = vec.Y;
                if (vec.Y > maxY)
                    maxY = vec.Y;

                if (vec.Z < minZ)
                    minZ = vec.Z;
                if (vec.Z > maxZ)
                    maxZ = vec.Z;
            }

            public void Include(BoundingBox other)
            {
                if (other.minX < minX)
                    minX = other.minX;
                if (other.maxX > maxX)
                    maxX = other.maxX;

                if (other.minY < minY)
                    minY = other.minY;
                if (other.maxY > maxY)
                    maxY = other.maxY;

                if (other.minZ < minZ)
                    minZ = other.minZ;
                if (other.maxZ > maxZ)
                    maxZ = other.maxZ;
            }

            public Vector3 GetCenter() => new Vector3(
                (minX + maxX) * 0.5f,
                (minY + maxY) * 0.5f,
                (minZ + maxZ) * 0.5f);
        }

        public struct LocalOrientation
        {
            public Vector3 Origin;
            public Quaternion Rotation;
            public LocalOrientation(Vector3 origin, Quaternion rotation)
            {
                Origin = origin;
                Rotation = rotation;
            }

            public LocalOrientation(Vector3 origin)
            {
                Origin = origin;
                Rotation = Quaternion.Identity;
            }
        }
    }
}
