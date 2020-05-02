using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GL_EditorFramework
{
    public class ObjectUIControl : FlexibleUIControl, IObjectUIControl
    {
        int fieldWidth;
        const int fieldSpace = 2;
        const int beforeTwoLineSpacing = 5;
        const int fullWidthSpace = 5;
        const int margin = 10;
        const int rowHeight = 20;

        private int currentY;

        protected override bool HasNoUIContent() => containerInfos.Count == 0;

        class ContainerInfo
        {
            public bool isExpanded;
            public string name;
            public IObjectUIContainer objectUIContainer;

            public ContainerInfo(IObjectUIContainer objectUIContainer, string name)
            {
                this.isExpanded = true;
                this.name = name;
                this.objectUIContainer = objectUIContainer;
            }
        }

        List<ContainerInfo> containerInfos = new List<ContainerInfo>();

        public IEnumerable<IObjectUIContainer> ObjectUIContainers => containerInfos.Select(x => x.objectUIContainer);

        public void AddObjectUIContainer(IObjectUIContainer objectUIContainer, string name)
        {
            containerInfos.Add(new ContainerInfo(objectUIContainer, name));
        }

        public void RemoveObjectUIContainer(IObjectUIContainer objectUIContainer)
        {
            UnFocusInput();

            foreach (ContainerInfo containerInfo in containerInfos)
            {
                if (containerInfo.objectUIContainer == objectUIContainer)
                {
                    containerInfos.Remove(containerInfo);
                    break;
                }
            }
        }

        public void ClearObjectUIContainers()
        {
            UnFocusInput();

            containerInfos.Clear();
        }
        int horizontalSperatorStartY;

        bool acceptSeperatorCalls = false;

        private void BeginHorizontalSeperator()
        {
            if (!acceptSeperatorCalls)
                return;

            horizontalSperatorStartY = currentY;
        }

        bool draggingHSeperator = false;

        private void EndHorizontalSeperator()
        {
            if (!acceptSeperatorCalls)
                return;

            int t = horizontalSperatorStartY + 1;

            int b = currentY - 6;

            int x = usableWidth - fieldWidth * 3 - fieldSpace * 2 - margin - 5;

            if (t >= b || (Math.Abs(x- mousePos.X - 15)>20 && !draggingHSeperator)) //seperator has length of 0, don't draw it
                return;


            g.DrawLine(
                draggingHSeperator ? Pens.Black : SystemPens.ControlDark, 
                x, t, x, b);

            if (eventType == EventType.DRAG_START && new Rectangle(x - 4, t, x + 4, b - t).Contains(mousePos))
            {
                draggingHSeperator = true;

                seperatorPositionBeforeDrag = seperatorPosition;
            }
        }


        double seperatorPosition = 0.6;

        double seperatorPositionBeforeDrag;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);


        }

        Rectangle nameClipping;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!TryInitDrawing(e))
            {
                AutoScrollMinSize = new Size();
                return;
            }

            if (eventType == EventType.DRAG_END || eventType == EventType.CLICK)
                draggingHSeperator = false;

            if (eventType == EventType.DRAG_ABORT)
            {
                draggingHSeperator = false;
                seperatorPosition = seperatorPositionBeforeDrag;
            }

            if (draggingHSeperator)
            {
                seperatorPosition = Math.Min(Math.Max(0.25, (mousePos.X - margin) / (double)(usableWidth - 2 * margin)), 0.75);
            }

            fieldWidth = (int)(((usableWidth - 2 * margin) * (1-seperatorPosition) - fieldSpace * 2) / 3.0);

            nameClipping = new Rectangle(0, 0, usableWidth - margin - fieldWidth * 3 - fieldSpace * 2 - 10, Height);

            currentY = margin + AutoScrollPosition.Y;

            foreach (ContainerInfo containerInfo in containerInfos)
            {
                int lastY = currentY - margin / 2;
                bool hovered = new Rectangle(Width - margin - 20 - SystemInformation.VerticalScrollBarWidth, currentY, 20 + SystemInformation.VerticalScrollBarWidth, 20).Contains(mousePos);

                if (hovered && eventType == EventType.CLICK)
                {
                    containerInfo.isExpanded = !containerInfo.isExpanded;

                    eventType = EventType.DRAW; //Click Handled
                }

                g.TranslateTransform(usableWidth - margin - 20, currentY);
                g.FillPolygon(hovered ? SystemBrushes.ControlDark : Framework.backBrush, containerInfo.isExpanded ? arrowDown : arrowLeft);
                g.ResetTransform();
                Heading(containerInfo.name);
                Spacing(margin / 2);

                if (containerInfo.isExpanded)
                {
                    acceptSeperatorCalls = true;

                    BeginHorizontalSeperator();
                    containerInfo.objectUIContainer.DoUI(this);

                    EndHorizontalSeperator();

                    acceptSeperatorCalls = false;
                }

                g.DrawRectangle(SystemPens.ControlDark, margin / 2, lastY, usableWidth - margin, currentY - lastY);

                Spacing(margin);

                if (changeTypes != 0)
                {
                    if ((changeTypes & VALUE_CHANGE_START) > 0)
                        containerInfo.objectUIContainer.OnValueChangeStart();
                    if ((changeTypes & VALUE_CHANGED) > 0)
                        containerInfo.objectUIContainer.OnValueChanged();
                    if ((changeTypes & VALUE_SET) > 0)
                        containerInfo.objectUIContainer.OnValueSet();
                }
            }

            AutoScrollMinSize = new Size(0, currentY - AutoScrollPosition.Y + margin);
        }

        #region IObjectControl
        public float NumberInput(float number, string name,
            float increment = 1, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false)
        {
            g.SetClip(nameClipping);

            DrawText(margin, currentY, name);

            g.ResetClip();

            number = NumericInputField(usableWidth - fieldWidth - margin, currentY, fieldWidth, number,
                new NumberInputInfo(increment, incrementDragDivider, min, max, wrapAround), true);
            currentY += rowHeight;
            return number;
        }

        public OpenTK.Vector3 Vector3Input(OpenTK.Vector3 vec, string name,
            float increment = 1, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false)
        {
            NumberInputInfo input = new NumberInputInfo(increment, incrementDragDivider, min, max, wrapAround);

            g.SetClip(nameClipping);

            DrawText(margin, currentY, name);

            g.ResetClip();

            vec.X = NumericInputField(usableWidth - margin - fieldWidth * 3 - fieldSpace * 2, currentY, fieldWidth, vec.X, input, true);
            vec.Y = NumericInputField(usableWidth - margin - fieldWidth * 2 - fieldSpace * 1, currentY, fieldWidth, vec.Y, input, true);
            vec.Z = NumericInputField(usableWidth - margin - fieldWidth, currentY, fieldWidth, vec.Z, input, true);


            currentY += rowHeight;
            return vec;
        }

        public OpenTK.Vector3 FullWidthVector3Input(OpenTK.Vector3 vec, string name,
            float increment = 1, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            currentY += beforeTwoLineSpacing;

            NumberInputInfo input = new NumberInputInfo(increment, incrementDragDivider, min, max, wrapAround);

            const int nameWidth = 13;
            int width = (usableWidth - margin * 2 - fullWidthSpace * 2) / 3;

            g.DrawString(name, HeadingFont, SystemBrushes.ControlText, margin, currentY);
            currentY += rowHeight;

            DrawText(margin, currentY, "X");
            vec.X = NumericInputField(margin + nameWidth, currentY, width - nameWidth, vec.X, input, true);

            DrawText(10 + width + fullWidthSpace, currentY, "Y");
            vec.Y = NumericInputField(margin + nameWidth + width + fullWidthSpace, currentY, width - nameWidth, vec.Y, input, true);

            DrawText(10 + width * 2 + fullWidthSpace * 2, currentY, "Z");
            vec.Z = NumericInputField(margin + nameWidth + width * 2 + fullWidthSpace * 2, currentY, width - nameWidth, vec.Z, input, true);


            currentY += rowHeight;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it

            return vec;
        }

        public bool Button(string name)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            bool clicked = Button(margin, currentY, usableWidth - margin * 2, name);
            currentY += 24;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it

            return clicked;
        }

        public int DoubleButton(string name, string name2)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            int width = (usableWidth - margin * 2 - fullWidthSpace) / 2;

            int clickedIndex = 0;

            if (Button(margin, currentY, width, name))
                clickedIndex = 1;
            if (Button(margin + width + fullWidthSpace, currentY, width, name2))
                clickedIndex = 2;

            currentY += 24;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it

            return clickedIndex;
        }

        public int TripleButton(string name, string name2, string name3)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            int width = (usableWidth - margin * 2 - fullWidthSpace * 2) / 3;

            int clickedIndex = 0;

            if (Button(margin, currentY, width, name))
                clickedIndex = 1;
            if (Button(margin + width + fullWidthSpace, currentY, width, name2))
                clickedIndex = 2;
            if (Button(margin + width * 2 + fullWidthSpace * 2, currentY, usableWidth - margin * 2 - width * 2 - fullWidthSpace * 2, name3))
                clickedIndex = 3;

            currentY += 24;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it

            return clickedIndex;
        }

        public int QuadripleButton(string name, string name2, string name3, string name4)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            int width = (usableWidth - margin * 2 - fullWidthSpace * 3) / 4;

            int clickedIndex = 0;

            if (Button(margin, currentY, width, name))
                clickedIndex = 1;
            if (Button(margin + width + fullWidthSpace, currentY, width, name2))
                clickedIndex = 2;
            if (Button(margin + width * 2 + fullWidthSpace * 2, currentY, width, name3))
                clickedIndex = 3;
            if (Button(margin + width * 3 + fullWidthSpace * 3, currentY, width, name4))
                clickedIndex = 4;

            currentY += 24;

            return clickedIndex;
        }

        public bool Link(string name)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            bool clicked = false;

            if (new Rectangle(margin, currentY, (int)g.MeasureString(name, Font).Width, textBoxHeight).Contains(mousePos))
            {
                if (mouseDown)
                    g.DrawString(name, LinkFont, Brushes.Red, margin, currentY);
                else
                    g.DrawString(name, LinkFont, Brushes.Blue, margin, currentY);

                clicked = eventType == EventType.CLICK;

                if(clicked)
                    eventType = EventType.DRAW; //Click Handled
            }
            else
            {
                g.DrawString(name, LinkFont, Brushes.Blue, margin, currentY);
            }

            currentY += rowHeight;
            index++;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it

            return clicked;
        }

        public void PlainText(string text)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            DrawText(margin, currentY, text);
            currentY += rowHeight;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it
        }

        public void Heading(string text)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            g.DrawString(text, HeadingFont, SystemBrushes.ControlText, margin, currentY);
            currentY += rowHeight;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it
        }

        public bool CheckBox(string name, bool isChecked)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            DrawText(margin, currentY, name);

            if (new Rectangle(usableWidth - margin - (textBoxHeight + 2), currentY, textBoxHeight + 2, textBoxHeight + 2).Contains(mousePos))
            {
                if (eventType == EventType.DRAG_START)
                    changeTypes |= VALUE_CHANGE_START;

                if (eventType == EventType.CLICK)
                {
                    isChecked = !isChecked;
                    changeTypes |= VALUE_SET;

                    eventType = EventType.DRAW; //Click Handled
                }

                DrawField(usableWidth - margin - (textBoxHeight + 2), currentY, textBoxHeight + 2, isChecked ? "x" : "",
                    SystemBrushes.Highlight, SystemBrushes.ControlLightLight);
            }
            else
            {
                DrawField(usableWidth - margin - (textBoxHeight + 2), currentY, textBoxHeight + 2, isChecked ? "x" : "",
                    SystemBrushes.ActiveBorder, SystemBrushes.ControlLightLight);
            }

            currentY += rowHeight;
            index++;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it

            return isChecked;
        }

        public string TextInput(string text, string name)
        {
            g.SetClip(nameClipping);

            DrawText(margin, currentY, name);

            g.ResetClip();

            text = TextInputField(usableWidth - margin - fieldWidth * 3 - fieldSpace * 2, currentY, fieldWidth * 3 + fieldSpace * 2, text, false);

            currentY += rowHeight;
            return text;
        }

        public string FullWidthTextInput(string text, string name)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            DrawText(margin, currentY, name);
            currentY += rowHeight;
            text = TextInputField(margin, currentY, usableWidth - margin * 2, text, false);
            currentY += rowHeight;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it

            return text;
        }

        public object ChoicePicker(string name, object value, IList values)
        {
            int width = fieldWidth * 3 + fieldSpace * 2;

            g.SetClip(nameClipping);

            DrawText(margin, currentY, name);

            g.ResetClip();

            value = ChoicePickerField(usableWidth - width - margin, currentY, width, value, values);

            currentY += rowHeight;
            return value;
        }

        public string DropDownTextInput(string name, string text, string[] dropDownItems)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            g.SetClip(nameClipping);

            DrawText(margin, currentY, name);

            g.ResetClip();

            currentY += rowHeight;

            text = DropDownTextInputField(margin, currentY, usableWidth - margin * 2, text, dropDownItems);

            currentY += rowHeight;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it

            return text;
        }

        public void Spacing(int amount)
        {
            currentY += amount;
        }

        public void VerticalSeperator()
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            g.FillRectangle(SystemBrushes.ControlLightLight, margin, currentY - 2, usableWidth - margin * 2, 2);
            g.FillRectangle(SystemBrushes.ControlDark, margin, currentY - 2, usableWidth - margin * 2 - 1, 1);
            currentY += 2;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it
        }
        #endregion
    }

    /// <summary>
    /// A control for displaying object specific UI that an <see cref="IObjectUIContainer"/> provides
    /// </summary>
    public interface IObjectUIControl
    {
        float NumberInput(float number, string name,
            float increment = 1f, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false);

        OpenTK.Vector3 Vector3Input(OpenTK.Vector3 vec, string name,
            float increment = 1f, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false);

        OpenTK.Vector3 FullWidthVector3Input(OpenTK.Vector3 vec, string name,
            float increment = 1f, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false);

        bool Button(string name);
        int DoubleButton(string name, string name2);
        int TripleButton(string name, string name2, string name3);
        int QuadripleButton(string name, string name2, string name3, string name4);
        bool Link(string name);
        void PlainText(string text);
        void Heading(string text);
        bool CheckBox(string name, bool isChecked);
        string TextInput(string text, string name);
        string FullWidthTextInput(string text, string name);
        object ChoicePicker(string name, object value, IList values);
        string DropDownTextInput(string name, string text, string[] dropDownItems);
        void Spacing(int amount);
        void VerticalSeperator();
    }

    /// <summary>
    /// A provider for object specific UI for example properties
    /// </summary>
    public interface IObjectUIContainer
    {
        void DoUI(IObjectUIControl control);
        void UpdateProperties();

        void OnValueChangeStart();
        void OnValueChanged();
        void OnValueSet();
    }
}