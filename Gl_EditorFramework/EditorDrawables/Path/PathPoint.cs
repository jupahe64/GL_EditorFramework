using System;
using System.Collections;
using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using OpenTK;
using static GL_EditorFramework.EditorDrawables.EditorSceneBase;

namespace GL_EditorFramework.EditorDrawables
{
    public class PathPoint : EditableObject
    {
        public bool Selected = false;

        public override string ToString() => "PathPoint";

        public PathPoint(Vector3 position, Vector3 controlPoint1, Vector3 controlPoint2)
        {
            Position = position;
            ControlPoint1 = controlPoint1;
            ControlPoint2 = controlPoint2;
        }

        public Path Path { get; internal set; }

        [PropertyCapture.Undoable]
        public Vector3 Position { get; set; }

        public virtual Vector3 GlobalPosition { get => Position; set => Position = value; }

        /// <summary>
        /// The position of the first ControlPoint (in) relative to the PathPoint
        /// </summary>
        [PropertyCapture.Undoable]
        public Vector3 ControlPoint1 { get; set; }

        public virtual Vector3 GlobalCP1 { get => ControlPoint1; set => ControlPoint1 = value; }

        /// <summary>
        /// The position of the second ControlPoint (out) relative to the PathPoint
        /// </summary>
        [PropertyCapture.Undoable]
        public Vector3 ControlPoint2 { get; set; }

        public virtual Vector3 GlobalCP2 { get => ControlPoint2; set => ControlPoint2 = value; }
        
        public override bool TryStartDragging(DragActionType actionType, int hoveredPart, out LocalOrientation localOrientation, out bool dragExclusively)
        {
            if (hoveredPart == 0)
            {
                localOrientation = new LocalOrientation(Position);
                dragExclusively = false;
                return Selected;
            }

            if (ControlPoint1 != Vector3.Zero)
            {
                if (hoveredPart == 1)
                {
                    if (actionType == DragActionType.TRANSLATE)
                    {
                        localOrientation = new LocalOrientation(Position + ControlPoint1);
                        dragExclusively = true; //controlPoints can be moved exclusively
                        return true;
                    }
                    else
                    {
                        localOrientation = new LocalOrientation(Position);
                        dragExclusively = false;
                        return Selected;
                    }
                }
            }

            if (ControlPoint2 != Vector3.Zero)
            {
                if (hoveredPart == 2)
                {
                    if (actionType == DragActionType.TRANSLATE)
                    {
                        localOrientation = new LocalOrientation(Position + ControlPoint2);
                        dragExclusively = true; //controlPoints can be moved exclusively
                        return true;
                    }
                    else
                    {
                        localOrientation = new LocalOrientation(Position);
                        dragExclusively = false;
                        return Selected;
                    }
                }
            }
            throw new Exception("Invalid partIndex");
        }

        public override bool IsSelected(int partIndex) => Selected;

        public override void GetSelectionBox(ref BoundingBox boundingBox)
        {
            if (!Selected)
                return;

            boundingBox.Include(new BoundingBox(
                Position.X - Path.CubeScale,
                Position.X + Path.CubeScale,
                Position.Y - Path.CubeScale,
                Position.Y + Path.CubeScale,
                Position.Z - Path.CubeScale,
                Position.Z + Path.CubeScale
            ));
        }

        public override LocalOrientation GetLocalOrientation(int partIndex)
        {
            return new LocalOrientation(Position);
        }

        public override bool IsInRange(float range, float rangeSquared, Vector3 pos) => true; //probably never gets called

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
            if (partIndex == 0)
            {
                Selected = true;
                    
                    
            }
            return REDRAW;
        }

        public override uint Deselect(int partIndex, GL_ControlBase control)
        {
            if (partIndex == 0)
            {
                Selected = false;
                    
                    
            }
            return REDRAW;
        }

        public override uint DeselectAll(GL_ControlBase control)
        {
            Selected = false;
                
                
            return REDRAW;
        }

        public override void SetTransform(Vector3? pos, Vector3? rot, Vector3? scale, int _part, out Vector3? prevPos, out Vector3? prevRot, out Vector3? prevScale)
        {
            prevPos = null;
            prevRot = null;
            prevScale = null;

            if (!pos.HasValue)
                return;

            if (_part == 0)
            {
                prevPos = Position;
                Position = pos.Value;
                return;
            }

            if (_part == 1)
            {
                prevPos = ControlPoint1;
                ControlPoint1 = pos.Value;
                return;
            }
                
            if (_part == 2)
            {
                prevPos = ControlPoint2;
                ControlPoint2 = pos.Value;
                return;
            }
        }
            
        public override void Prepare(GL_ControlModern control)
        {
            //probably never gets called
        }

        public override void Prepare(GL_ControlLegacy control)
        {
            //probably never gets called
        }

        public override void Draw(GL_ControlModern control, Pass pass)
        {
            //probably never gets called
        }

        public override void Draw(GL_ControlLegacy control, Pass pass)
        {
            //probably never gets called
        }

        public override void ApplyTransformActionToSelection(AbstractTransformAction transformAction, ref TransformChangeInfos transformChangeInfos)
        {
            if (Selected)
            {
                if (ControlPoint1 != Vector3.Zero)
                {
                    Vector3 pc = ControlPoint1;

                    GlobalCP1 = transformAction.NewIndividualPos(GlobalCP1, out bool cpHasChanged);
                    if (cpHasChanged)
                        transformChangeInfos.Add(this, 1, pc, null, null);
                }

                if (ControlPoint2 != Vector3.Zero)
                {
                    Vector3 pc = ControlPoint2;

                    GlobalCP2 = transformAction.NewIndividualPos(GlobalCP2, out bool cpHasChanged);
                    if (cpHasChanged)
                        transformChangeInfos.Add(this, 2, pc, null, null);
                }

                Vector3 pp = Position;

                GlobalPosition = transformAction.NewPos(GlobalPosition, out bool posHasChanged);

                if (posHasChanged)
                    transformChangeInfos.Add(this, 0, pp, null, null);
            }
        }

        public override void ApplyTransformActionToPart(AbstractTransformAction transformAction, int _part, ref TransformChangeInfos transformChangeInfos)
        {
            if (ControlPoint1 != Vector3.Zero)
            {
                if (_part == 1)
                {
                    Vector3 pc = ControlPoint1;

                    GlobalCP1 = transformAction.NewPos(GlobalPosition + GlobalCP1, out bool posHasChanged) - GlobalPosition;

                    if (posHasChanged)
                        transformChangeInfos.Add(this, 1, pc, null, null);

                    return;
                }
            }

            if (ControlPoint2 != Vector3.Zero)
            {
                if (_part == 2)
                {
                    Vector3 pc = ControlPoint2;

                    GlobalCP2 = transformAction.NewPos(GlobalPosition + GlobalCP2, out bool posHasChanged) - GlobalPosition;

                    if (posHasChanged)
                        transformChangeInfos.Add(this, 2, pc, null, null);

                    return;
                }
            }
        }

        public override int GetPickableSpan() => 3;

        public override void DeleteSelected(EditorSceneBase scene, DeletionManager manager, IList list)
        {
            if (Selected)
                manager.Add(list, this);
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
    }
}
