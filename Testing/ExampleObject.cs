using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GL_EditorFramework;
using GL_EditorFramework.EditorDrawables;
using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using OpenTK;

namespace Example
{
    //This class is supposed to show of some very basic animation stuff you could do with this framework
    class ExampleObject : SingleObject
    {
        public ExampleObject(Vector3 pos) : base(pos)
        {

        }

        public override string ToString() => "moving platforms";

        static new Vector4 Color = new Vector4(1f, 0f, 0f, 1f);

        ulong animationStartFrame;

        public override void Draw(GL_ControlModern control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            if (!editorScene.ShouldBeDrawn(this))
                return;

            bool hovered = editorScene.Hovered == this;

            Matrix4 mtx = Matrix4.CreateScale(1f, 0.25f, 1f);
            mtx *= Matrix4.CreateFromAxisAngle(Vector3.UnitY, -Framework.HALF_PI);
            mtx *= Matrix4.CreateTranslation(Selected ? editorScene.SelectionTransformAction.NewPos(Position) : Position);

            Vector4 pickingColor = control.NextPickingColor();
            
            Vector4 lineBoxColor;

            if (hovered && Selected)
                lineBoxColor = hoverSelectColor;
            else if (Selected)
                lineBoxColor = selectColor;
            else if (hovered)
                lineBoxColor = hoverColor;
            else
                lineBoxColor = new Vector4(0.75f, 0.75f, 0.75f, 1);

            if(Selected)
                control.UpdateModelMatrix(mtx * Matrix4.CreateTranslation(Vector3.UnitX * (3f - Math.Abs((control.RedrawerFrame-animationStartFrame) * 0.0625f % 6f - 3f))));
            else
                control.UpdateModelMatrix(mtx);

            Renderers.ColorBlockRenderer.Draw(control, pass, Color, Color, pickingColor);

            control.UpdateModelMatrix(mtx);

            Renderers.ColorBlockRenderer.DrawLineBox(control, pass, lineBoxColor, pickingColor);

            control.UpdateModelMatrix(mtx * Matrix4.CreateTranslation(Vector3.UnitX * 3f));

            Renderers.ColorBlockRenderer.DrawLineBox(control, pass, lineBoxColor, pickingColor);
        }

        public override void Draw(GL_ControlModern control, Pass pass)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            Matrix4 mtx = Matrix4.CreateScale(1f, 0.25f, 1f);
            mtx *= Matrix4.CreateFromAxisAngle(Vector3.UnitY, -Framework.HALF_PI);
            mtx *= Matrix4.CreateTranslation(Position);
            control.UpdateModelMatrix(mtx);

            Vector4 pickingColor = control.NextPickingColor();

            Vector4 lineBoxColor = new Vector4(0.75f, 0.75f, 0.75f, 1);

            if (Selected)
                control.UpdateModelMatrix(mtx * Matrix4.CreateTranslation(Vector3.UnitX * (3f - Math.Abs((control.RedrawerFrame-animationStartFrame) * 0.0625f % 6f - 3f))));
            else
                control.UpdateModelMatrix(mtx);

            Renderers.ColorBlockRenderer.Draw(control, pass, Color, Color, pickingColor);

            control.UpdateModelMatrix(mtx);

            Renderers.ColorBlockRenderer.DrawLineBox(control, pass, lineBoxColor, pickingColor);

            control.UpdateModelMatrix(mtx * Matrix4.CreateTranslation(Vector3.UnitX * 3f));

            Renderers.ColorBlockRenderer.DrawLineBox(control, pass, lineBoxColor, pickingColor);
        }

        public override void Draw(GL_ControlLegacy control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            if (!editorScene.ShouldBeDrawn(this))
                return;

            bool hovered = editorScene.Hovered == this;

            Matrix4 mtx = Matrix4.CreateScale(1f, 0.25f, 1f);
            mtx *= Matrix4.CreateFromAxisAngle(Vector3.UnitY, -Framework.HALF_PI);
            mtx *= Matrix4.CreateTranslation(Selected ? editorScene.SelectionTransformAction.NewPos(Position) : Position);

            Vector4 pickingColor = control.NextPickingColor();

            Vector4 lineBoxColor;

            if (hovered && Selected)
                lineBoxColor = hoverSelectColor;
            else if (Selected)
                lineBoxColor = selectColor;
            else if (hovered)
                lineBoxColor = hoverColor;
            else
                lineBoxColor = new Vector4(0.75f, 0.75f, 0.75f, 1);

            if (Selected)
                control.UpdateModelMatrix(mtx * Matrix4.CreateTranslation(Vector3.UnitX * (3f - Math.Abs((control.RedrawerFrame-animationStartFrame) * 0.0625f % 6f - 3f))));
            else
                control.UpdateModelMatrix(mtx);

            Renderers.ColorBlockRenderer.Draw(control, pass, Color, Color, pickingColor);

            control.UpdateModelMatrix(mtx);

            Renderers.ColorBlockRenderer.DrawLineBox(control, pass, lineBoxColor, pickingColor);

            control.UpdateModelMatrix(mtx * Matrix4.CreateTranslation(Vector3.UnitX * 3f));

            Renderers.ColorBlockRenderer.DrawLineBox(control, pass, lineBoxColor, pickingColor);
        }

        public override void Draw(GL_ControlLegacy control, Pass pass)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            Matrix4 mtx = Matrix4.CreateScale(1f, 0.25f, 1f);
            mtx *= Matrix4.CreateFromAxisAngle(Vector3.UnitY, -Framework.HALF_PI);
            mtx *= Matrix4.CreateTranslation(Position);
            control.UpdateModelMatrix(mtx);

            Vector4 pickingColor = control.NextPickingColor();

            Vector4 lineBoxColor = new Vector4(0.75f, 0.75f, 0.75f, 1);

            if (Selected)
                control.UpdateModelMatrix(mtx * Matrix4.CreateTranslation(Vector3.UnitX * (3f - Math.Abs((control.RedrawerFrame-animationStartFrame) * 0.0625f % 6f - 3f))));
            else
                control.UpdateModelMatrix(mtx);

            Renderers.ColorBlockRenderer.Draw(control, pass, Color, Color, pickingColor);

            control.UpdateModelMatrix(mtx);

            Renderers.ColorBlockRenderer.DrawLineBox(control, pass, lineBoxColor, pickingColor);

            control.UpdateModelMatrix(mtx * Matrix4.CreateTranslation(Vector3.UnitX * 3f));

            Renderers.ColorBlockRenderer.DrawLineBox(control, pass, lineBoxColor, pickingColor);
        }

        public override uint Select(int index, GL_ControlBase control)
        {
            
            if (!Selected)
            {
                Selected = true;
                animationStartFrame = control.RedrawerFrame;
                control.AttachPickingRedrawer();
            }
            return 0;
        }

        public override uint SelectDefault(GL_ControlBase control)
        {
            
            if (!Selected)
            {
                Selected = true;
                animationStartFrame = control.RedrawerFrame;
                control.AttachPickingRedrawer();
            }
            return 0;
        }

        public override uint SelectAll(GL_ControlBase control)
        {
            
            if (!Selected)
            {
                Selected = true;
                animationStartFrame = control.RedrawerFrame;
                control.AttachPickingRedrawer();
            }
            return 0;
        }

        public override uint Deselect(int index, GL_ControlBase control)
        {
            
            if (Selected)
            {
                Selected = false;
                control.DetachPickingRedrawer();
            }
            return 0;
        }

        public override uint DeselectAll(GL_ControlBase control)
        {
            
            if (Selected)
            {
                Selected = false;
                control.DetachPickingRedrawer();
            }
            return 0;
        }

        public override bool TrySetupObjectUIControl(EditorSceneBase scene, ObjectUIControl objectUIControl)
        {
            if (!Selected)
                return false;
            objectUIControl.AddObjectUIContainer(new PropertyProvider(this, scene), "Transform");
            objectUIControl.AddObjectUIContainer(new ExampleUIContainer(this, scene), "Example Controls");
            return true;
        }

        public class ExampleUIContainer : IObjectUIContainer
        {
            string text = "";
            string longText = "";
            float number = 0;
            SingleObject obj;
            EditorSceneBase scene;

            EnemyType enemyType = EnemyType.Stone;
            EnemyType enemyType2 = EnemyType.Fire;
            string objectType = "AnimatedObject";

            static readonly string[] objectTypes = new string[]
            {
                "SingleObject",
                "Transformable",
                "Path",
                "AnimatedObject"
            };

            public ExampleUIContainer(SingleObject obj, EditorSceneBase scene)
            {
                this.obj = obj;
                this.scene = scene;
            }

            enum EnemyType
            {
                Fire,
                Water,
                Grass,
                Stone,
                Air
            }

            public void DoUI(IObjectUIControl control)
            {
                text = control.TextInput(text, "TextInput");
                longText = control.FullWidthTextInput(longText, "Long Text Input");
                number = control.NumberInput(number, "Number Input");
                control.Link("Just some Link");

                control.DoubleButton("Add", "Remove");
                control.TripleButton("Add", "Remove", "Insert");
                control.QuadripleButton("+", "-", "*","/");
                enemyType =  (EnemyType)control.ChoicePicker("Enemy1 Type", enemyType,  Enum.GetValues(typeof(EnemyType)));
                enemyType2 = (EnemyType)control.ChoicePicker("Enemy2 Type", enemyType2, Enum.GetValues(typeof(EnemyType)));
                control.VerticalSeperator();
                objectType = control.DropDownTextInput("Object Type", objectType, objectTypes);

                control.Spacing(30);
                control.PlainText("Some Text");
            }

            public void OnValueChangeStart()
            {

            }

            public void OnValueChanged()
            {
                scene.Refresh();
            }

            public void OnValueSet()
            {
                
            }

            public void UpdateProperties()
            {

            }
        }
    }
}
