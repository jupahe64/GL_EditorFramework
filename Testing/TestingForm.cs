﻿using System;
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
using WinInput = System.Windows.Input;

namespace Testing
{
    public partial class TestingForm : Form
    {
        public TestingForm()
        {
            InitializeComponent();
        }

        private TestProvider propertyContainer = new TestProvider();

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
            scene.ListChanged += Scene_ListChanged;
            scene.CurrentListChanged += Scene_CurrentListChanged;
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

            scene.ToogleSelected(scene.objects[4], true);
            Scene_SelectionChanged(this, null);

            propertyContainer.PathPointEdit += (object sender, EventArgs _e) => {
                sceneListView1.CurrentList = propertyContainer.selectedPath.PathPoints;
            };
        }

        private void Scene_CurrentListChanged(object sender, CurrentListChangedEventArgs e)
        {
            sceneListView1.CurrentList = e.List;
        }

        private void SceneListView1_ItemsMoved(object sender, ItemsMovedEventArgs e)
        {
            scene.ReorderObjects(sceneListView1.CurrentList, e.OriginalIndex, e.Count, e.Offset);
            e.Handled = true;
            gL_ControlModern1.Refresh();
        }

        private void Scene_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.Lists.Contains(sceneListView1.CurrentList))
            {
                sceneListView1.UpdateAutoScroll();
                sceneListView1.Refresh();
            }
        }

        private void SceneListView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (object obj in e.ItemsToDeselect)
                scene.ToogleSelected((EditableObject)obj, false);

            foreach (object obj in e.ItemsToSelect)
                scene.ToogleSelected((EditableObject)obj, true);

            e.Handled = true;
            gL_ControlModern1.Refresh();

            Scene_SelectionChanged(this, null);
        }

        private void GL_ControlModern1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                scene.DeleteSelected();
                gL_ControlModern1.Refresh();
                sceneListView1.UpdateAutoScroll();
                Scene_SelectionChanged(this, null);
            }
        }

        private void ObjectPropertyControl1_ValueSet(object sender, EventArgs e)
        {
            scene.ApplyCurrentTransformAction();
            propertyContainer.pCenter = propertyContainer.center;

            propertyChangesAction.translation = new Vector3();
            scene.CurrentAction = propertyChangesAction;
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

            objectPropertyControl1.CurrentPropertyProvider = scene.GetPropertyProvider();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            scene.Add(sceneListView1.CurrentList, new TransformableObject(Vector3.Zero,Quaternion.Identity,Vector3.One));
            sceneListView1.UpdateAutoScroll();
        }
    }

    public class TestProvider : IPropertyProvider
    {
        public Vector3 pCenter;
        public Vector3 center;

        bool showMore = false;

        public Path selectedPath;
        public event EventHandler PathPointEdit;

        public void Setup(IEnumerable<object> editableObjects)
        {
            if (editableObjects.Count() == 1)
                selectedPath = (editableObjects.First() as Path);
            else
                selectedPath = null;

            EditableObject.BoundingBox box = EditableObject.BoundingBox.Default;
            foreach (IEditableObject obj in editableObjects)
            {
                obj.GetSelectionBox(ref box);
            }
            center = box.GetCenter();
            pCenter = center;
        }

        public void DoUI(IObjectPropertyControl control)
        {
            center.X = control.NumberInput(center.X, "Position X", 0.125f, 2);
            center.Y = control.NumberInput(center.Y, "Position Y", 0.125f, 2);
            center.Z = control.NumberInput(center.Z, "Position Z", 0.125f, 2);

            if(WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
                center = control.Vector3Input(center, "Position");
            else
                center = control.Vector3Input(center, "Position", 0.125f, 2);

            
            if (showMore)
            {
                if (control.Button("Hide Links"))
                    showMore = false;

                if (control.Link("Link 1"))
                    MessageBox.Show("Thx for clicking.");
                if (control.Link("Link 2"))
                    MessageBox.Show("Thx for clicking.");
                if (control.Link("Link 3"))
                    MessageBox.Show("Thx for clicking.");
            }
            else
            {
                if (control.Button("Show Links"))
                    showMore = true;
            }

            if (selectedPath != null)
            {
                if (control.Button("Edit PathPoints"))
                    PathPointEdit?.Invoke(this, null);
            }
        }
    }
}
