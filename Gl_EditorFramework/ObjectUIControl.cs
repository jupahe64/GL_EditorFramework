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
        const int fieldWidth = 50;
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!TryInitDrawing(e))
            {
                AutoScrollMinSize = new Size();
                return;
            }

            currentY = margin + AutoScrollPosition.Y;

            foreach (ContainerInfo containerInfo in containerInfos)
            {
                int lastY = currentY - margin / 2;
                bool hovered = new Rectangle(Width - margin - 20 - SystemInformation.VerticalScrollBarWidth, currentY, 20 + SystemInformation.VerticalScrollBarWidth, 20).Contains(mousePos);

                if (hovered && eventType == EventType.CLICK)
                    containerInfo.isExpanded = !containerInfo.isExpanded;

                g.TranslateTransform(usableWidth - margin - 20, currentY);
                g.FillPolygon(hovered ? SystemBrushes.ControlDark : backBrush, containerInfo.isExpanded ? arrowDown : arrowLeft);
                g.ResetTransform();
                Heading(containerInfo.name);
                Spacing(margin / 2);

                if (containerInfo.isExpanded)
                    containerInfo.objectUIContainer.DoUI(this);

                g.DrawRectangle(SystemPens.ControlDark, margin / 2, lastY, usableWidth - margin, currentY - lastY);

                Spacing(margin);

                if (eventType != EventType.DRAW)
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
            DrawText(margin, currentY, name);

            number = NumericInputField(usableWidth - fieldWidth - margin, currentY, fieldWidth, number,
                new NumberInputInfo(increment, incrementDragDivider, min, max, wrapAround), true);
            currentY += rowHeight;
            return number;
        }

        public OpenTK.Vector3 Vector3Input(OpenTK.Vector3 vec, string name,
            float increment = 1, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false)
        {
            NumberInputInfo input = new NumberInputInfo(increment, incrementDragDivider, min, max, wrapAround);

            DrawText(margin, currentY, name);

            vec.X = NumericInputField(usableWidth - margin - fieldWidth * 3 - fieldSpace * 2, currentY, fieldWidth, vec.X, input, true);
            vec.Y = NumericInputField(usableWidth - margin - fieldWidth * 2 - fieldSpace * 1, currentY, fieldWidth, vec.Y, input, true);
            vec.Z = NumericInputField(usableWidth - margin - fieldWidth, currentY, fieldWidth, vec.Z, input, true);


            currentY += rowHeight;
            return vec;
        }

        public OpenTK.Vector3 FullWidthVector3Input(OpenTK.Vector3 vec, string name,
            float increment = 1, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false)
        {
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
            return vec;
        }

        public bool Button(string name)
        {
            bool clicked = Button(margin, currentY, usableWidth - margin * 2, name);
            currentY += 24;

            return clicked;
        }

        public int DoubleButton(string name, string name2)
        {
            int width = (usableWidth - margin * 2 - fullWidthSpace) / 2;

            int clickedIndex = 0;

            if (Button(margin, currentY, width, name))
                clickedIndex = 1;
            if (Button(margin + width + fullWidthSpace, currentY, width, name2))
                clickedIndex = 2;

            currentY += 24;

            return clickedIndex;
        }

        public int TripleButton(string name, string name2, string name3)
        {
            int width = (usableWidth - margin * 2 - fullWidthSpace * 2) / 3;

            int clickedIndex = 0;

            if (Button(margin, currentY, width, name))
                clickedIndex = 1;
            if (Button(margin + width + fullWidthSpace, currentY, width, name2))
                clickedIndex = 2;
            if (Button(margin + width * 2 + fullWidthSpace * 2, currentY, usableWidth - margin * 2 - width * 2 - fullWidthSpace * 2, name3))
                clickedIndex = 3;

            currentY += 24;

            return clickedIndex;
        }

        public int QuadripleButton(string name, string name2, string name3, string name4)
        {
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
            bool clicked = false;

            if (new Rectangle(margin, currentY, (int)g.MeasureString(name, Font).Width, textBoxHeight).Contains(mousePos))
            {
                if (mouseDown)
                    g.DrawString(name, LinkFont, Brushes.Red, margin, currentY);
                else
                    g.DrawString(name, LinkFont, Brushes.Blue, margin, currentY);

                clicked = eventType == EventType.CLICK;
            }
            else
            {
                g.DrawString(name, LinkFont, Brushes.Blue, margin, currentY);
            }

            currentY += rowHeight;
            index++;

            return clicked;
        }

        public void PlainText(string text)
        {
            DrawText(margin, currentY, text);
            currentY += rowHeight;
        }

        public void Heading(string text)
        {
            g.DrawString(text, HeadingFont, SystemBrushes.ControlText, margin, currentY);
            currentY += rowHeight;
        }

        public bool CheckBox(string name, bool isChecked)
        {
            DrawText(margin, currentY, name);

            if (new Rectangle(usableWidth - margin - (textBoxHeight + 2), currentY, textBoxHeight + 2, textBoxHeight + 2).Contains(mousePos))
            {
                if (eventType == EventType.DRAG_START)
                    changeTypes |= VALUE_CHANGE_START;

                if (eventType == EventType.CLICK)
                {
                    isChecked = !isChecked;
                    changeTypes |= VALUE_SET;
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

            return isChecked;
        }

        public string TextInput(string text, string name)
        {
            DrawText(margin, currentY, name);

            text = TextInputField(usableWidth - margin - fieldWidth * 3 - fieldSpace * 2, currentY, fieldWidth * 3 + fieldSpace * 2, text, false);

            currentY += rowHeight;
            return text;
        }

        public string FullWidthTextInput(string text, string name)
        {
            DrawText(margin, currentY, name);
            currentY += rowHeight;
            text = TextInputField(margin, currentY, usableWidth - margin * 2, text, false);
            currentY += rowHeight;
            return text;
        }

        public object ChoicePicker(string name, object value, IList values)
        {
            int width = fieldWidth * 3 + fieldSpace * 2;

            DrawText(margin, currentY, name);

            value = ChoicePickerField(usableWidth - width - margin, currentY, width, value, values);

            currentY += rowHeight;
            return value;
        }

        public string DropDownTextInput(string name, string text, string[] dropDownItems)
        {
            DrawText(margin, currentY, name);
            currentY += rowHeight;

            switch (eventType)
            {
                case EventType.CLICK:
                    if (new Rectangle(margin + 1, currentY + 1, usableWidth - margin * 2 - 2, comboBoxHeight - 2).Contains(mousePos))
                    {
                        PrepareComboBox(margin, currentY, usableWidth - margin * 2, text, dropDownItems, true);
                    }
                    else
                        DrawComboBoxField(margin, currentY, usableWidth - margin * 2, text, SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, false);

                    break;

                case EventType.LOST_FOCUS:
                    if (focusedIndex == index)
                    {
                        changeTypes |= VALUE_SET;
                        text = comboBox1.Text;
                    }

                    if (focusedIndex == index)
                        DrawComboBoxField(margin, currentY, usableWidth - margin * 2, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, false);
                    else
                        DrawComboBoxField(margin, currentY, usableWidth - margin * 2, text, SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, false);

                    break;

                default:
                    if (focusedIndex == index)
                        DrawComboBoxField(margin, currentY, usableWidth - margin * 2, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, false);
                    else
                        DrawComboBoxField(margin, currentY, usableWidth - margin * 2, text, SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, false);

                    break;
            }

            index++;
            currentY += rowHeight;
            return text;
        }

        public void Spacing(int amount)
        {
            currentY += amount;
        }

        public void VerticalSeperator()
        {
            g.FillRectangle(SystemBrushes.ControlLightLight, margin, currentY - 2, usableWidth - margin * 2, 2);
            g.FillRectangle(SystemBrushes.ControlDark, margin, currentY - 2, usableWidth - margin * 2 - 1, 1);
            currentY += 2;
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