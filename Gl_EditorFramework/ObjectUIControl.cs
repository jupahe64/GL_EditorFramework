using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Specialized;

namespace GL_EditorFramework
{
    /// <summary>
    /// A control for displaying object specific UI that an <see cref="IObjectUIProvider"/> provides
    /// </summary>
    public partial class ObjectUIControl : UserControl, IObjectUIControl
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        protected static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, int lParam);

        public Font HeadingFont;
        public Font LinkFont;

        enum EventType
        {
            DRAW,
            CLICK,
            DRAG_START,
            DRAG,
            DRAG_END,
            DRAG_ABORT,
            LOST_FOCUS
        }

        static uint VALUE_CHANGE_START = 1;
        static uint VALUE_CHANGED = 2;
        static uint VALUE_SET = 4;

        uint changeTypes = 0;

        EventType eventType = EventType.DRAW;

        IObjectUIProvider objectUIProvider;

        /// <summary>
        /// The ObjectUIProvider used for the UI in this control
        /// </summary>
        public IObjectUIProvider CurrentObjectUIProvider
        {
            get => objectUIProvider;

            set
            {
                objectUIProvider = value;
                Refresh();
            }
        }

        Graphics g;

        int index;

        Point mousePos;
        Point lastMousePos;
        Point dragStarPos;

        int usableWidth;

        Brush buttonHighlight = new SolidBrush(MixedColor(SystemColors.GradientInactiveCaption, SystemColors.ControlLightLight));

        Timer doubleClickTimer = new Timer();
        bool acceptDoubleClick = false;

        bool mouseDown = false;

        int focusedIndex = -1;
        int dragIndex = -1;

        bool mouseWasDragged = false;
        int textBoxHeight;

        public ObjectUIControl()
        {
            SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);

            InitializeComponent();

            HeadingFont = new Font(Font.FontFamily, 10);

            LinkFont = new Font(textBox1.Font, FontStyle.Underline);

            doubleClickTimer.Interval = SystemInformation.DoubleClickTime;
            doubleClickTimer.Tick += DoubleClickTimer_Tick;

            textBox1.Visible = false;
            textBoxHeight = textBox1.Height;
            textBox1.KeyPress += TextBox1_KeyPress;
        }

        private void TextBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (acceptDoubleClick || e.Clicks == 2)
            {
                textBox1.SelectAll();
                acceptDoubleClick = false;
            }

        }

        private void DoubleClickTimer_Tick(object sender, EventArgs e)
        {
            acceptDoubleClick = false;
            doubleClickTimer.Stop();
        }

        private void TextBox1_LostFocus(object sender, EventArgs e)
        {
            if (focusedIndex == -1)
                return;

            eventType = EventType.LOST_FOCUS;

            Refresh();

            textBox1.Visible = false;
            focusedIndex = -1;

            eventType = EventType.DRAW;

            if ((changeTypes & VALUE_CHANGE_START) > 0)
                objectUIProvider?.OnValueChangeStart();
            if ((changeTypes & VALUE_CHANGED) > 0)
                objectUIProvider?.OnValueChanged();
            if ((changeTypes & VALUE_SET) > 0)
                objectUIProvider?.OnValueSet();

            changeTypes = 0;
        }

        private void ComboBox1_LostFocus(object sender, EventArgs e)
        {
            if (focusedIndex == -1)
                return;

            comboBox1.Visible = false;
            AutoScrollMinSize = new Size(0, autoScrollRestoreHeight);
            AutoScrollPosition = new Point(0, autoScrollRestoreY);

            eventType = EventType.LOST_FOCUS;

            Refresh();

            focusedIndex = -1;

            eventType = EventType.DRAW;

            if ((changeTypes & VALUE_CHANGE_START) > 0)
                objectUIProvider?.OnValueChangeStart();
            if ((changeTypes & VALUE_CHANGED) > 0)
                objectUIProvider?.OnValueChanged();
            if ((changeTypes & VALUE_SET) > 0)
                objectUIProvider?.OnValueSet();

            changeTypes = 0;
        }

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return && textBox1.Focused)
            {
                Focus();
                e.SuppressKeyPress = true;
            }
        }

        private void ComboBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return && comboBox1.Focused)
                Focus();
        }
        private int currentY;

        private void TextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            NumberFormatInfo numberFormat = CultureInfo.CurrentCulture.NumberFormat;
            string numberDecimalSeparator = numberFormat.NumberDecimalSeparator;
            string numberGroupSeparator = numberFormat.NumberGroupSeparator;
            string negativeSign = numberFormat.NegativeSign;
            string text = e.KeyChar.ToString();
            if (!char.IsDigit(e.KeyChar) && !text.Equals(numberDecimalSeparator) && !text.Equals(numberGroupSeparator) && !text.Equals(negativeSign) && 
                e.KeyChar != '\b' && (ModifierKeys & (Keys.Control | Keys.Alt)) == Keys.None)
            {
                e.Handled = true;
            }
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            Refresh();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            usableWidth = Width - 15;
            Refresh();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (objectUIProvider == null)
                return;

            g = e.Graphics;

            if (comboBox1.Visible)
            {
                g.DrawString(comboBoxName, textBox1.Font, SystemBrushes.ControlText, 10, 10);
                return;
            }

            currentY = 10 + AutoScrollPosition.Y;

            index = 0;

            try
            {
                objectUIProvider.DoUI(this);

                AutoScrollMinSize = new Size(0, currentY - AutoScrollPosition.Y);
            }
            catch(ControlInvalidatedException) //this Control has been invalidated
            {
                AutoScrollMinSize = new Size();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (comboBox1.Visible)
                return;

            if (e.Button == MouseButtons.Left)
            {
                mouseDown = true;

                mouseWasDragged = false;
                dragStarPos = e.Location;
                eventType = EventType.DRAG_START;
                dragIndex = -1;

                Refresh();

                Focus();

                eventType = EventType.DRAW;
            }
            else if (e.Button == MouseButtons.Right)
            {
                eventType = EventType.DRAG_ABORT;
                Refresh();
                eventType = EventType.DRAW;
            }

            if ((changeTypes & VALUE_CHANGE_START) > 0)
                objectUIProvider?.OnValueChangeStart();
            if ((changeTypes & VALUE_CHANGED) > 0)
                objectUIProvider?.OnValueChanged();
            if ((changeTypes & VALUE_SET) > 0)
                objectUIProvider?.OnValueSet();

            changeTypes = 0;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            mousePos = e.Location;

            if (e.Button == MouseButtons.Left && Math.Abs(mousePos.X - dragStarPos.X) > 2)
                mouseWasDragged = true;

            if (mouseWasDragged)
                eventType = EventType.DRAG;

            Refresh();

            eventType = EventType.DRAW;
            lastMousePos = e.Location;

            if ((changeTypes & VALUE_CHANGE_START) > 0)
                objectUIProvider?.OnValueChangeStart();
            if ((changeTypes & VALUE_CHANGED) > 0)
                objectUIProvider?.OnValueChanged();
            if ((changeTypes & VALUE_SET) > 0)
                objectUIProvider?.OnValueSet();

            changeTypes = 0;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            mouseDown = false;

            if (mouseWasDragged)
            {
                eventType = EventType.DRAG_END;
                Refresh();
            }
            else
            {
                if (objectUIProvider == null)
                    return;

                eventType = EventType.CLICK;
                Refresh();
            }

            eventType = EventType.DRAW;

            if ((changeTypes & VALUE_CHANGE_START) > 0)
                objectUIProvider?.OnValueChangeStart();
            if ((changeTypes & VALUE_CHANGED) > 0)
                objectUIProvider?.OnValueChanged();
            if ((changeTypes & VALUE_SET) > 0)
                objectUIProvider?.OnValueSet();

            if (textBoxRequest.HasValue)
            {
                SuspendLayout();
                textBox1.Visible = true;

                textBox1.Text = textBoxRequest.Value.value;
                textBox1.TextAlign = textBoxRequest.Value.alignment;
                textBox1.Location = new Point(textBoxRequest.Value.x, textBoxRequest.Value.y);
                textBox1.Width = textBoxRequest.Value.width;
                ResumeLayout();

                textBox1.Focus();

                int lParam = mousePos.Y - textBox1.Top << 16 | (mousePos.X - textBox1.Left & 65535);
                int num = (int)SendMessage(new HandleRef(this, textBox1.Handle), 215, 0, lParam);

                textBox1.Select(Math.Max(0, num), 0);

                if (textBoxRequest.Value.useNumericFilter)
                    textBox1.KeyPress += TextBox1_KeyPress;
                else
                    textBox1.KeyPress -= TextBox1_KeyPress;

                textBoxRequest = null;
            }

            changeTypes = 0;
        }

        private void DrawField(int x, int y, int width, string value, Brush outline, Brush background, bool isCentered = true)
        {
            g.FillRectangle(outline, x, y, width, textBoxHeight + 2);
            g.FillRectangle(background, x + 1, y + 1, width - 2, textBoxHeight);

            g.SetClip(new Rectangle(
                x + 1,
                y + 1,
                width - 2,
                textBoxHeight));

            if(isCentered)
                g.DrawString(value, textBox1.Font, SystemBrushes.ControlText,
                x + 1 + (width - (int)Math.Ceiling(g.MeasureString(value, textBox1.Font).Width)) / 2, y);
            else
                g.DrawString(value, textBox1.Font, SystemBrushes.ControlText,
                x + 1, y);

            g.ResetClip();
        }

        struct TextBoxSetup
        {
            public int x;
            public int y;
            public int width;
            public string value;
            public HorizontalAlignment alignment;
            public bool useNumericFilter;

            public TextBoxSetup(int x, int y, int width, string value, HorizontalAlignment alignment, bool useNumericFilter)
            {
                this.x = x;
                this.y = y;
                this.width = width;
                this.value = value;
                this.alignment = alignment;
                this.useNumericFilter = useNumericFilter;
            }
        }

        TextBoxSetup? textBoxRequest;

        private void PrepareFieldForInput(int x, int y, int width, string value, bool isNumericInput = true, bool isCentered = true)
        {
            textBoxRequest = new TextBoxSetup(x + 1, y + 1, width - 2, value, isCentered ? HorizontalAlignment.Center : HorizontalAlignment.Left, isNumericInput);
            
            focusedIndex = index;

            changeTypes |= VALUE_CHANGE_START;

            acceptDoubleClick = true;
            doubleClickTimer.Start();
        }

        private static float Clamped(float value, float min, float max, bool wrapAround)
        {
            if (wrapAround)
            {
                float span = max - min;
                return min + (((value - min) % span + span) % span);
            }
            else
            {
                return Math.Min(Math.Max(min, value), max);
            }
        }
        
        #region UI Elements
        float valueBeforeDrag;
        private float NumericInputField(int x, int y, int width, float number, NumberInputInfo info, bool isCentered)
        {
            switch (eventType)
            {
                case EventType.CLICK:
                    if (new Rectangle(x + 1, currentY + 1, width - 1, textBoxHeight - 2).Contains(mousePos))
                    {
                        DrawField(x, currentY, width, "", SystemBrushes.ActiveBorder, SystemBrushes.ControlLightLight, isCentered);
                        PrepareFieldForInput(x, currentY, width, number.ToString(), isCentered);
                    }
                    else
                        DrawField(x, currentY, width, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, isCentered);

                    break;

                case EventType.DRAG_START:
                    if (focusedIndex == index)
                        DrawField(x, currentY, width, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, isCentered);
                    else
                    {
                        DrawField(x, currentY, width, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, isCentered);

                        if (new Rectangle(x + 1, currentY + 1, width - 1, textBoxHeight - 2).Contains(mousePos))
                        {
                            dragIndex = index;
                            valueBeforeDrag = number;
                            changeTypes |= VALUE_CHANGE_START;
                        }
                    }

                    break;

                case EventType.DRAG:
                    if (dragIndex == index)
                    {
                        changeTypes |= VALUE_CHANGED;
                        number = Clamped(valueBeforeDrag + (mousePos.X - dragStarPos.X) / info.incrementDragDivider * info.increment, 
                            info.min, info.max, info.wrapAround);
                    }
                    if (focusedIndex == index)
                        DrawField(x, currentY, width, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, isCentered);
                    else
                        DrawField(x, currentY, width, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, isCentered);

                    break;

                case EventType.DRAG_END:
                    if (dragIndex == index)
                    {
                        changeTypes |= VALUE_SET;
                        dragIndex = -1;
                    }

                    if (focusedIndex == index)
                        DrawField(x, currentY, width, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, isCentered);
                    else
                        DrawField(x, currentY, width, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, isCentered);

                    break;

                case EventType.LOST_FOCUS:
                    if (focusedIndex == index && float.TryParse(textBox1.Text, out float parsed))
                    {
                        changeTypes |= VALUE_SET;
                        number = Clamped(parsed, info.min, info.max, info.wrapAround);
                    }

                    if (focusedIndex == index)
                        DrawField(x, currentY, width, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, isCentered);
                    else
                        DrawField(x, currentY, width, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, isCentered);

                    break;

                case EventType.DRAG_ABORT:
                    if (dragIndex == index)
                    {
                        changeTypes |= VALUE_CHANGED;
                        number = valueBeforeDrag;
                        dragIndex = -1;
                    }
                    if (focusedIndex == index)
                        DrawField(x, currentY, width, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, isCentered);
                    else
                        DrawField(x, currentY, width, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, isCentered);

                    break;

                default: //EventType.DRAW
                    if (focusedIndex == index)
                        DrawField(x, currentY, width, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, isCentered);
                    else
                        DrawField(x, currentY, width, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight,isCentered);

                    break;
            }

            index++;
            return number;
        }

        private string TextInputField(int x, int y, int width, string text, bool isCentered)
        {
            switch (eventType)
            {
                case EventType.CLICK:
                    if (new Rectangle(x + 1, y + 1, width - 2, textBoxHeight - 2).Contains(mousePos))
                    {
                        DrawField(x, y, width, "", SystemBrushes.ActiveBorder, SystemBrushes.ControlLightLight, isCentered);
                        PrepareFieldForInput(x, y, width, text, false, isCentered);
                    }
                    else
                        DrawField(x, y, width, text, SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, isCentered);

                    break;

                case EventType.LOST_FOCUS:
                    if (focusedIndex == index)
                    {
                        changeTypes |= VALUE_SET;
                        text = textBox1.Text;
                    }

                    if (focusedIndex == index)
                        DrawField(x, y, width, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, isCentered);
                    else
                        DrawField(x, y, width, text, SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, isCentered);

                    break;

                default:
                    if (focusedIndex == index)
                        DrawField(x, y, width, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, isCentered);
                    else
                        DrawField(x, y, width, text, SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, isCentered);

                    break;
            }

            index++;
            return text;
        }

        private bool Button(int x, int y, int width, string name)
        {
            bool clicked = false;

            if (new Rectangle(x, y, width, textBoxHeight + 6).Contains(mousePos))
            {
                if (mouseDown)
                {
                    g.FillRectangle(SystemBrushes.HotTrack, x, y, width, textBoxHeight + 6);
                    g.FillRectangle(SystemBrushes.GradientInactiveCaption, x + 1, y + 1, width - 2, textBoxHeight + 4);
                }
                else
                {
                    g.FillRectangle(SystemBrushes.Highlight, x, y, width, textBoxHeight + 6);
                    g.FillRectangle(buttonHighlight, x + 1, y + 1, width - 2, textBoxHeight + 4);
                }

                clicked = eventType == EventType.CLICK;
            }
            else
            {
                g.FillRectangle(SystemBrushes.ControlDark, x, y, width, textBoxHeight + 6);
                g.FillRectangle(SystemBrushes.ControlLight, x + 1, y + 1, width - 2, textBoxHeight + 4);


            }

            g.DrawString(name, textBox1.Font, SystemBrushes.ControlText,
                x + (width - (int)g.MeasureString(name, textBox1.Font).Width) / 2, y + 3);

            index++;

            return clicked;
        }
        #endregion

        public struct NumberInputInfo
        {
            public readonly float increment;
            public readonly int incrementDragDivider;
            public readonly float min;
            public readonly float max;
            public readonly bool wrapAround;

            public NumberInputInfo(float increment = 1, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false)
            {
                this.increment = increment;
                this.incrementDragDivider = incrementDragDivider;
                this.min = min;
                this.max = max;
                this.wrapAround = wrapAround;
            }
        }

        const int fieldWidth = 50;
        const int fieldSpace = 2;
        const int beforeTwoLineSpacing = 5;
        const int fullWidthSpace = 5;
        const int margin = 10;
        const int rowHeight = 20;

        #region IObjectControl
        public float NumberInput(float number, string name,
            float increment = 1, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false)
        {
            g.DrawString(name, textBox1.Font, SystemBrushes.ControlText, margin, currentY);

            number = NumericInputField(usableWidth - fieldWidth-margin, currentY, fieldWidth, number, 
                new NumberInputInfo(increment, incrementDragDivider, min, max, wrapAround),true);
            currentY += rowHeight;
            return number;
        }

        public OpenTK.Vector3 Vector3Input(OpenTK.Vector3 vec, string name, 
            float increment = 1, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false)
        {
            NumberInputInfo input = new NumberInputInfo(increment, incrementDragDivider, min, max, wrapAround);
            
            g.DrawString(name, textBox1.Font, SystemBrushes.ControlText, margin, currentY);
            
            vec.X = NumericInputField(usableWidth - margin - fieldWidth * 3 - fieldSpace * 2, currentY, fieldWidth, vec.X, input, true);
            vec.Y = NumericInputField(usableWidth - margin - fieldWidth * 2 - fieldSpace * 1, currentY, fieldWidth, vec.Y, input, true);
            vec.Z = NumericInputField(usableWidth - margin - fieldWidth,                      currentY, fieldWidth, vec.Z, input, true);
            

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

            g.DrawString("X", textBox1.Font, SystemBrushes.ControlText, margin, currentY);
            vec.X = NumericInputField(margin + nameWidth, currentY, width - nameWidth, vec.X, input, true);

            g.DrawString("Y", textBox1.Font, SystemBrushes.ControlText, 10 + width + fullWidthSpace, currentY);
            vec.Y = NumericInputField(margin + nameWidth + width + fullWidthSpace, currentY, width - nameWidth, vec.Y, input, true);

            g.DrawString("Z", textBox1.Font, SystemBrushes.ControlText, 10 + width * 2 + fullWidthSpace * 2, currentY);
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

            if (Button(margin,                 currentY, width, name))
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

            if (Button(margin,                         currentY, width, name))
                clickedIndex = 1;
            if (Button(margin + width     + fullWidthSpace,     currentY, width, name2))
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

            if (Button(margin, currentY,                         width, name))
                clickedIndex = 1;
            if (Button(margin + width     + fullWidthSpace,     currentY, width, name2))
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

            if (new Rectangle(margin, currentY, (int)g.MeasureString(name, textBox1.Font).Width, textBoxHeight).Contains(mousePos))
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
            g.DrawString(text, Font, SystemBrushes.ControlText, margin, currentY);
            currentY += rowHeight;
        }

        public void Heading(string text)
        {
            g.DrawString(text, HeadingFont, SystemBrushes.ControlText, margin, currentY);
            currentY += rowHeight;
        }

        public bool CheckBox(string name, bool isChecked)
        {
            g.DrawString(name, textBox1.Font, SystemBrushes.ControlText, margin, currentY);

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
            g.DrawString(name, textBox1.Font, SystemBrushes.ControlText, margin, currentY);

            text = TextInputField(usableWidth - margin - fieldWidth * 3 - fieldSpace * 2, currentY, fieldWidth * 3 + fieldSpace * 2, text, false);

            currentY += rowHeight;
            return text;
        }

        public string FullWidthTextInput(string text, string name)
        {
            g.DrawString(name, textBox1.Font, SystemBrushes.ControlText, margin, currentY);
            currentY += rowHeight;
            text = TextInputField(margin, currentY, usableWidth - margin * 2, text, false);
            currentY += rowHeight;
            return text;
        }

        public object ChoicePicker(string name, object value, IList values)
        {
            int width = fieldWidth * 3 + fieldSpace * 2;

            g.DrawString(name, textBox1.Font, SystemBrushes.ControlText, margin, currentY);
            DrawField(usableWidth - width - margin, currentY, width, value.ToString(), SystemBrushes.ActiveBorder, SystemBrushes.ControlLightLight);

            float arrowWidth = g.MeasureString(">", textBox1.Font).Width;
            
            int index = values.IndexOf(value);
            if(index > 0)
            {
                g.DrawString("<", textBox1.Font, SystemBrushes.ControlText, usableWidth - width - margin + 1, currentY);
                if (new Rectangle(usableWidth - width - margin + 1, currentY + 1, fieldWidth, textBoxHeight - 2).Contains(mousePos))
                {
                    if (eventType == EventType.DRAG_START)
                        changeTypes |= VALUE_CHANGE_START;

                    if (eventType == EventType.CLICK)
                        value = values[index - 1];
                }
            }                                                  

            if (index < values.Count - 1)
            {
                g.DrawString(">", textBox1.Font, SystemBrushes.ControlText, usableWidth - margin - 1 - arrowWidth, currentY);
                if (new Rectangle(usableWidth - margin - 1 - fieldWidth, currentY + 1, fieldWidth, textBoxHeight - 2).Contains(mousePos))
                {
                    if (eventType == EventType.DRAG_START)
                        changeTypes |= VALUE_CHANGE_START;

                    if (eventType == EventType.CLICK)
                        value = values[index + 1];
                }
            }

            currentY += rowHeight;
            return value;
        }

        int autoScrollRestoreHeight;
        int autoScrollRestoreY;
        string comboBoxName;

        class ControlInvalidatedException : Exception { }

        public string AdvancedTextInput(string name, string text, object[] recommendations)
        {
            g.DrawString(name, textBox1.Font, SystemBrushes.ControlText, margin, currentY);
            currentY += rowHeight;

            switch (eventType)
            {
                case EventType.CLICK:
                    if (new Rectangle(margin + 1, currentY+1, usableWidth - margin * 2 - 2, textBoxHeight - 2).Contains(mousePos))
                    {
                        comboBoxName = name;
                        autoScrollRestoreHeight = AutoScrollMinSize.Height;
                        autoScrollRestoreY = -AutoScrollPosition.Y;
                        comboBox1.Text = text;
                        comboBox1.Items.Clear();
                        if(recommendations!=null && recommendations.Length!=0)
                            comboBox1.Items.AddRange(recommendations);
                        comboBox1.Visible = true;
                        comboBox1.Focus();
                        Invalidate();
                        focusedIndex = index;
                        changeTypes |= VALUE_CHANGE_START;
                        throw new ControlInvalidatedException();
                    }
                    else
                        DrawField(margin, currentY, usableWidth - margin * 2, text, SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, false);

                    break;

                case EventType.LOST_FOCUS:
                    if (focusedIndex == index)
                    {
                        changeTypes |= VALUE_SET;
                        text = comboBox1.Text;
                    }

                    if (focusedIndex == index)
                        DrawField(margin, currentY, usableWidth - margin * 2, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, false);
                    else
                        DrawField(margin, currentY, usableWidth - margin * 2, text, SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, false);

                    break;

                default:
                    if (focusedIndex == index)
                        DrawField(margin, currentY, usableWidth - margin * 2, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, false);
                    else
                        DrawField(margin, currentY, usableWidth - margin * 2, text, SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, false);

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
        #endregion

        static Color MixedColor(Color color1, Color color2)
        {
            byte a1 = color1.A;
            byte r1 = color1.R;
            byte g1 = color1.G;
            byte b1 = color1.B;

            byte a2 = color2.A;
            byte r2 = color2.R;
            byte g2 = color2.G;
            byte b2 = color2.B;

            int a3 = (a1 + a2) / 2;
            int r3 = (r1 + r2) / 2;
            int g3 = (g1 + g2) / 2;
            int b3 = (b1 + b2) / 2;

            return Color.FromArgb(a3, r3, g3, b3);
        }
    }

    /// <summary>
    /// A control for displaying object specific UI that an <see cref="IObjectUIProvider"/> provides
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
        string AdvancedTextInput(string name, string text, object[] recommendations);
        void Spacing(int amount);
    }

    /// <summary>
    /// A provider for object specific UI for example properties
    /// </summary>
    public interface IObjectUIProvider
    {
        void DoUI(IObjectUIControl control);
        void UpdateProperties();

        void OnValueChangeStart();
        void OnValueChanged();
        void OnValueSet();
    }
}
