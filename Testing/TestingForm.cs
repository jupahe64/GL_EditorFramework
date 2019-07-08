using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GL_EditorFramework;
using GL_EditorFramework.EditorDrawables;
using OpenTK;

namespace Testing
{
    public partial class TestingForm : Form
    {
        public TestingForm()
        {
            InitializeComponent();
        }

        private TestContainer propertyContainer = new TestContainer();

        private EditorScene scene;

        private EditorSceneBase.PropertyChanges propertyChangesAction = new EditorSceneBase.PropertyChanges();

        protected override void OnLoad(EventArgs e)
        {
            EditableObject obj;
            base.OnLoad(e);
            scene = new EditorScene();
            
            scene.objects.Add(obj = new AnimatedObject(new Vector3(0, -4, 0)));

            List<Path.PathPoint> pathPoints = new List<Path.PathPoint>
            {
                new Path.PathPoint(
                new Vector3(0, 0, 0),
                new Vector3(0, 0, 0),
                new Vector3(2, 0, 0)
                ),
                new Path.PathPoint(
                new Vector3(8, 4, 2),
                new Vector3(-4, 0, 4),
                new Vector3(4, 0, -4)
                ),
                new Path.PathPoint(
                new Vector3(4, 2, -6),
                new Vector3(0, 0, 0),
                new Vector3(0, 0, 0)
                )
            };
            
            scene.objects.Add(obj = new Path(pathPoints));

            pathPoints = new List<Path.PathPoint>
            {
                new Path.PathPoint(
                new Vector3(-3, 6, 0),
                new Vector3(0, 0, -1.75f),
                new Vector3(0, 0, 1.75f)
                ),
                new Path.PathPoint(
                new Vector3(0, 6, 3),
                new Vector3(-1.75f, 0, 0),
                new Vector3(1.75f, 0, 0)
                ),
                new Path.PathPoint(
                new Vector3(3, 6, 0),
                new Vector3(0, 0, 1.75f),
                new Vector3(0, 0, -1.75f)
                ),
                new Path.PathPoint(
                new Vector3(0, 6, -3),
                new Vector3(1.75f, 0, 0),
                new Vector3(-1.75f, 0, 0)
                )
            };
            
            scene.objects.Add(obj = new Path(pathPoints) { Closed = true });
            
            scene.objects.Add(obj = new Path(pathPoints) { Closed = true });

            for (int i = 5; i<10000; i++)
            {
                scene.objects.Add(obj = new TransformableObject(new Vector3(i,0,0), Quaternion.Identity, Vector3.One));
            }

            gL_ControlModern1.MainDrawable = scene;
            gL_ControlModern1.ActiveCamera = new GL_EditorFramework.StandardCameras.InspectCamera(1f);

            gL_ControlLegacy1.MainDrawable = new SingleObject(new Vector3());
            gL_ControlModern1.CameraDistance = 20;
            scene.SelectionChanged += Scene_SelectionChanged;
            scene.ObjectsMoved += Scene_ObjectsMoved;
            scene.ListChanged += Scene_ObjectCountChanged;
            objectPropertyControl1.ValueChanged += ObjectPropertyControl1_ValueChanged;
            objectPropertyControl1.ValueSet += ObjectPropertyControl1_ValueChanged;
            objectPropertyControl1.ValueChangeStart += ObjectPropertyControl1_ValueChangeStart;
            objectPropertyControl1.ValueSet += ObjectPropertyControl1_ValueSet;
            gL_ControlModern1.KeyDown += GL_ControlModern1_KeyDown;
            
            for(int i = 0; i<15; i++)
                sceneListView1.lists.Add("Test"+i,scene.objects);


            sceneListView1.SelectedItems = scene.SelectedObjects;
            sceneListView1.CurrentCategory = "Test0";
            sceneListView1.SelectionChanged += SceneListView1_SelectionChanged;
            sceneListView1.ItemsMoved += SceneListView1_ItemsMoved;
        }

        private void SceneListView1_ItemsMoved(object sender, ItemsMovedEventArgs e)
        {
            scene.MoveObjectsInList(e.OriginalIndex, e.Count, e.Offset);
            e.Handled = true;
            gL_ControlModern1.Refresh();
        }

        private void Scene_ObjectCountChanged(object sender, EventArgs e)
        {
            sceneListView1.UpdateAutoScroll();
            sceneListView1.Refresh();
        }

        private void SceneListView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (object obj in e.ItemsToSelect)
                scene.ToogleSelected((EditableObject)obj, true);

            foreach (object obj in e.ItemsToDeselect)
                scene.ToogleSelected((EditableObject)obj, false);

            e.Handled = true;
            gL_ControlModern1.Refresh();
        }

        private void GL_ControlModern1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                IEditableObject[] objsToDelete = scene.SelectedObjects.ToArray();
                scene.Delete(objsToDelete);
                gL_ControlModern1.Refresh();
                sceneListView1.UpdateAutoScroll();
                Scene_SelectionChanged(this, null);
            }
        }

        private void ObjectPropertyControl1_ValueSet(object sender, EventArgs e)
        {
            scene.ApplyCurrentTransformAction();
            propertyContainer.pCenter = propertyContainer.center;
        }

        private void ObjectPropertyControl1_ValueChangeStart(object sender, EventArgs e)
        {
            scene.CurrentAction = propertyChangesAction;
        }

        private void Scene_ObjectsMoved(object sender, EventArgs e)
        {
            propertyContainer.Setup(scene.SelectedObjects);
            objectPropertyControl1.Refresh();
        }

        private void ObjectPropertyControl1_ValueChanged(object sender, EventArgs e)
        {
            propertyChangesAction.translation = propertyContainer.center - propertyContainer.pCenter;

            gL_ControlModern1.Refresh();
        }

        private void Scene_SelectionChanged(object sender, EventArgs e)
        {
            sceneListView1.Refresh();

            if (scene.SelectedObjects.Count > 0)
            {
                propertyContainer.Setup(scene.SelectedObjects);

                objectPropertyControl1.CurrentPropertyContainer = propertyContainer;
            }
            else
            {
                if (objectPropertyControl1.CurrentPropertyContainer != null)
                    objectPropertyControl1.CurrentPropertyContainer = null;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            scene.Add(new TransformableObject(Vector3.Zero,Quaternion.Identity,Vector3.One));
            sceneListView1.UpdateAutoScroll();
        }
    }

    public class TestContainer : AbstractPropertyContainer
    {
        public Vector3 pCenter;
        public Vector3 center;

        public void Setup(List<IEditableObject> editableObjects)
        {
            EditableObject.BoundingBox box = EditableObject.BoundingBox.Default;
            foreach (IEditableObject obj in editableObjects)
            {
                box.Include(obj.GetSelectionBox());
            }
            center = box.GetCenter();
            pCenter = center;
        }

        public override void DoUI(IObjectPropertyControl control)
        {
            center.X = control.NumberInput(center.X, "x", 0.125f);
            center.Y = control.NumberInput(center.Y, "y", 0.125f);
            center.Z = control.NumberInput(center.Z, "z", 0.125f);
        }
    }
}
