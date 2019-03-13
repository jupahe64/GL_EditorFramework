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

		//only gets called when the object is selected
		public abstract bool CanStartDragging();

		public abstract bool IsSelected();

		public abstract Vector3 GetSelectionCenter();

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

		public virtual void ApplyTransformActionToSelection(AbstractTransformAction transformAction)
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
	}
}
