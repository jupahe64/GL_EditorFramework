using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
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

        private EditorScene scene;
        
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            scene = new EditorScene();

            listBox1.Items.Add("moving platform");
            scene.objects.Add(new AnimatedObject(new Vector3(0, -4, 0)));

            listBox1.Items.Add("path");
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
            scene.objects.Add(new Path(pathPoints));

            listBox1.Items.Add("path");
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
            scene.objects.Add(new Path(pathPoints) { Closed = true });

            listBox1.Items.Add("path");
            /*pathPoints = new List<Path.PathPoint>();
            pathPoints.Add(new Path.PathPoint(
                new Vector3(-3, 6, 0),
                new Vector3(0, 0, -1.75f),
                new Vector3(0, 0, 1.75f)
                ));
            pathPoints.Add(new Path.PathPoint(
                new Vector3(0, 6, 3),
                new Vector3(-1.75f, 0, 0),
                new Vector3(1.75f, 0, 0)
                ));
            pathPoints.Add(new Path.PathPoint(
                new Vector3(3, 6, 0),
                new Vector3(0, 0, 1.75f),
                new Vector3(0, 0, -1.75f)
                ));
            pathPoints.Add(new Path.PathPoint(
                new Vector3(0, 6, -3),
                new Vector3(1.75f, 0, 0),
                new Vector3(-1.75f, 0, 0)
                ));*/
            scene.objects.Add(new Path(pathPoints) { Closed = true });

            for (int i = 5; i<10000; i++)
            {
                listBox1.Items.Add("block");
                scene.objects.Add(new TransformableObject(new Vector3(i,0,0)));
            }

            gL_ControlModern1.MainDrawable = scene;
            gL_ControlModern1.ActiveCamera = new GL_EditorFramework.StandardCameras.InspectCamera(1f);

            gL_ControlLegacy1.MainDrawable = new SingleObject(new Vector3());

            scene.SelectionChanged += Scene_SelectionChanged;
            listBox1.SelectedIndexChanged += ListBox1_SelectedIndexChanged;
        }

        private void Scene_SelectionChanged(object sender, EventArgs e)
        {
            listBox1.SelectedIndexChanged -= ListBox1_SelectedIndexChanged;
            listBox1.SelectedIndices.Clear();
            int i = 0;
            foreach(EditableObject o in scene.objects)
            {
                if(o.IsSelected())
                    listBox1.SelectedIndices.Add(i);
                i++;
            }

            listBox1.SelectedIndexChanged += ListBox1_SelectedIndexChanged;
        }

        private void ListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<EditableObject> newSelection = new List<EditableObject>();
            foreach(int i in listBox1.SelectedIndices)
            {
                newSelection.Add(scene.objects[i]);
            }

            scene.SelectedObjects = newSelection;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Add("block"); //make sure to add the entry before you add an object because the SelectionChanged event will be fired
            scene.Add(new TransformableObject(new Vector3()));
        }
    }
}
