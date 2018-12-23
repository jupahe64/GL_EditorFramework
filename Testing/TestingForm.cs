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

		private Random rng = new Random();

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			scene = new EditorScene();
			scene.objects.Add(new EditableObject());
			scene.objects.Add(new AnimatedObject() { Position = new Vector3(0, -4, 0) });
			gL_ControlModern1.MainDrawable = scene;

			gL_ControlLegacy1.MainDrawable = new EditableObject();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Color rand = Color.FromArgb(rng.Next());
			scene.AddObject(new EditableObject() { CubeColor = new Vector4(rand.R/255f,rand.G / 255f, rand.B / 255f, 1f) });
		}
	}
}
