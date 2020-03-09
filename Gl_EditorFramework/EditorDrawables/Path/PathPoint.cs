using System;
using System.Collections;
using System.Reflection;
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

        public virtual Vector3 GlobalPos { get => Position; set => Position = value; }

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

        public override void StartDragging(DragActionType actionType, int hoveredPart, EditorSceneBase scene)
        {
            if (hoveredPart == 0)
            {
                if (Selected)
                    scene.StartTransformAction(new LocalOrientation(GlobalPos), actionType);
            }

            if (ControlPoint1 != Vector3.Zero)
            {
                if (hoveredPart == 1)
                {
                    if (actionType == DragActionType.TRANSLATE)
                    {
                        //controlPoints can be moved exclusively
                        scene.StartAction(new CP1MoveAction(this, scene));
                        Console.WriteLine("hovered: " + scene.HoveredPart);
                    }
                    else
                    {
                        if (Selected)
                            scene.StartTransformAction(new LocalOrientation(GlobalPos), actionType);
                    }
                }
            }

            if (ControlPoint2 != Vector3.Zero)
            {
                if (hoveredPart == 2)
                {
                    if (actionType == DragActionType.TRANSLATE)
                    {
                        //controlPoints can be moved exclusively
                        scene.StartAction(new CP2MoveAction(this, scene));
                        Console.WriteLine("hovered: " + scene.HoveredPart);
                    }
                    else
                    {
                        if (Selected)
                            scene.StartTransformAction(new LocalOrientation(GlobalPos), actionType);
                    }
                }
            }
        }

        class CP1MoveAction : TransformingAction<TranslateAction>
        {
            PathPoint point;
            EditorSceneBase scene;

            PropertyCapture propertyCapture;

            Vector3 startPosGlobal;

            public CP1MoveAction(PathPoint point, EditorSceneBase scene)
            {
                this.point = point;
                this.scene = scene;

                startPosGlobal = point.GlobalCP1;

                transformAction = new TranslateAction(scene.GL_Control, scene.GL_Control.GetMousePos(), point.GlobalCP1, scene.GL_Control.PickingDepth);

                propertyCapture = new PropertyCapture(point);
            }

            protected override void Update()
            {
                point.GlobalCP1 = transformAction.NewPos(startPosGlobal);
            }

            public override void Apply()
            {
                if (propertyCapture.TryGetRevertable(out IRevertable revertable))
                    scene.AddToUndo(revertable);
            }

            public override void Cancel()
            {
                if (propertyCapture.TryGetRevertable(out IRevertable revertable))
                    revertable.Revert(scene);
            }
        }

        class CP2MoveAction : TransformingAction<TranslateAction>
        {
            PathPoint point;
            EditorSceneBase scene;

            PropertyCapture propertyCapture;

            Vector3 startPosGlobal;

            public CP2MoveAction(PathPoint point, EditorSceneBase scene)
            {
                this.point = point;
                this.scene = scene;

                startPosGlobal = point.GlobalCP2;

                transformAction = new TranslateAction(scene.GL_Control, scene.GL_Control.GetMousePos(), point.GlobalCP2, scene.GL_Control.PickingDepth);

                propertyCapture = new PropertyCapture(point);
            }

            protected override void Update()
            {
                point.GlobalCP2 = transformAction.NewPos(startPosGlobal);
            }

            public override void Apply()
            {
                if (propertyCapture.TryGetRevertable(out IRevertable revertable))
                    scene.AddToUndo(revertable);
            }

            public override void Cancel()
            {
                if (propertyCapture.TryGetRevertable(out IRevertable revertable))
                    revertable.Revert(scene);
            }
        }

        public override bool IsSelected(int partIndex) => Selected;

        public override void GetSelectionBox(ref BoundingBox boundingBox)
        {
            if (!Selected)
                return;

            boundingBox.Include(new BoundingBox(
                GlobalPos.X - Path.CubeScale,
                GlobalPos.X + Path.CubeScale,
                GlobalPos.Y - Path.CubeScale,
                GlobalPos.Y + Path.CubeScale,
                GlobalPos.Z - Path.CubeScale,
                GlobalPos.Z + Path.CubeScale
            ));
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

                    var newPos = transformAction.NewIndividualPos(GlobalCP1, out bool cpHasChanged);
                    if (cpHasChanged)
                    {
                        GlobalCP1 = newPos;
                        transformChangeInfos.Add(this, 1, pc, null, null);
                    }
                }

                if (ControlPoint2 != Vector3.Zero)
                {
                    Vector3 pc = ControlPoint2;

                    var newPos = transformAction.NewIndividualPos(GlobalCP2, out bool cpHasChanged);
                    if (cpHasChanged)
                    {
                        GlobalCP2 = newPos;
                        transformChangeInfos.Add(this, 2, pc, null, null);
                    }
                }

                Vector3 pp = Position;

                {
                    var newPos = transformAction.NewPos(GlobalPos, out bool posHasChanged);

                    if (posHasChanged)
                    {
                        GlobalPos = newPos;
                        transformChangeInfos.Add(this, 0, pp, null, null);
                    }
                }
            }
        }

        public override void ApplyTransformActionToPart(AbstractTransformAction transformAction, int _part, ref TransformChangeInfos transformChangeInfos)
        {
            if (_part == 0)
            {
                Vector3 pp = Position;

                var newPos = transformAction.NewPos(GlobalPos, out bool posHasChanged);

                if (posHasChanged)
                {
                    GlobalPos = newPos;
                    transformChangeInfos.Add(this, 0, pp, null, null);
                }

                return;
            }

            if (ControlPoint1 != Vector3.Zero)
            {
                if (_part == 1)
                {
                    Console.WriteLine("hovered: " + _part);

                    Vector3 pc = ControlPoint1;

                    var newPos = transformAction.NewPos(GlobalPos + GlobalCP1, out bool posHasChanged) - GlobalPos;

                    if (posHasChanged)
                    {
                        GlobalCP1 = newPos;
                        transformChangeInfos.Add(this, 1, pc, null, null);
                    }

                    return;
                }
            }

            if (ControlPoint2 != Vector3.Zero)
            {
                if (_part == 2)
                {
                    Console.WriteLine("hovered: " + _part);

                    Vector3 pc = ControlPoint2;

                    var newPos = transformAction.NewPos(GlobalPos + GlobalCP2, out bool posHasChanged) - GlobalPos;

                    if (posHasChanged)
                    {
                        GlobalCP2 = newPos;
                        transformChangeInfos.Add(this, 2, pc, null, null);
                    }

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
            return GlobalPos;
        }
    }
}
