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
    /// A control for displaying object specific UI that an <see cref="IObjectUIContainer"/> provides
    /// </summary>
    public partial class FlexibleUIControl : UserControl
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        protected static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, int lParam);

        public Font HeadingFont;
        public Font LinkFont;

        public new Font Font
        {
            get => textBox1.Font;
            set => textBox1.Font = value;
        }

        protected enum EventType
        {
            DRAW,
            CLICK,
            DRAG_START,
            DRAG,
            DRAG_END,
            DRAG_ABORT,
            LOST_FOCUS
        }

        protected static uint VALUE_CHANGE_START = 1;
        protected static uint VALUE_CHANGED = 2;
        protected static uint VALUE_SET = 4;

        protected uint changeTypes = 0;

        protected EventType eventType = EventType.DRAW;

        protected Graphics g;

        protected int index;

        protected Point mousePos;
        protected Point lastMousePos;
        protected Point dragStarPos;

        protected Brush buttonHighlight = new SolidBrush(MixedColor(SystemColors.GradientInactiveCaption, SystemColors.ControlLightLight));

        Timer doubleClickTimer = new Timer();
        bool acceptDoubleClick = false;

        protected bool mouseDown = false;

        protected int focusedIndex = -1;
        protected int dragIndex = -1;

        bool mouseWasDragged = false;
        protected int textBoxHeight;

        bool textBoxHasNumericFilter = false;

        protected TextBox textBox1;
        protected ComboBox comboBox1;

        public FlexibleUIControl()
        {
            SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);

            SuspendLayout();

            textBox1 = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Location = new Point(83, 76),
                Name = "textBox1",
                Size = new Size(67, 13),
                TabIndex = 0,
                TextAlign = HorizontalAlignment.Center
            };
            textBox1.MouseClick += new MouseEventHandler(TextBox1_MouseClick);
            textBox1.KeyDown += new KeyEventHandler(TextBox1_KeyDown);
            textBox1.LostFocus += new EventHandler(TextBox1_LostFocus);


            comboBox1 = new ComboBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom
            | AnchorStyles.Left
            | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.Simple,
                FormattingEnabled = true,
                Location = new Point(10, 30),
                Margin = new Padding(0),
                Name = "comboBox1",
                Size = new Size(200, 123),
                TabIndex = 1,
                Visible = false
            };
            comboBox1.KeyDown += new KeyEventHandler(ComboBox1_KeyDown);
            comboBox1.LostFocus += new EventHandler(ComboBox1_LostFocus);


            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            Controls.Add(comboBox1);
            Controls.Add(textBox1);
            Size = new Size(220, 160);
            ResumeLayout(false);
            PerformLayout();


            HeadingFont = new Font(Font.FontFamily, 10);

            LinkFont = new Font(Font, FontStyle.Underline);

            doubleClickTimer.Interval = SystemInformation.DoubleClickTime;
            doubleClickTimer.Tick += DoubleClickTimer_Tick;

            textBox1.Visible = false;
            textBoxHeight = textBox1.Height;
        }

        protected int usableWidth;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (VScroll)
                usableWidth = Width - SystemInformation.VerticalScrollBarWidth - 2;
            else
                usableWidth = Width - 2;
            Refresh();
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

        protected static Brush backBrush = new SolidBrush(MixedColor(SystemColors.ControlDark, SystemColors.Control));

        protected static Point[] arrowDown = new Point[]
        {
            new Point( 2,  6),
            new Point(18,  6),
            new Point(10, 14)
        };

        protected static Point[] arrowLeft = new Point[]
        {
            new Point(14,  2),
            new Point(14, 18),
            new Point( 6, 10)
        };

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

            changeTypes = 0;
        }

        protected virtual bool HasNoUIContent() => false;

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || HasNoUIContent())
                return;
            mouseDown = false;

            if (mouseWasDragged)
            {
                eventType = EventType.DRAG_END;
                Refresh();
            }
            else
            {
                eventType = EventType.CLICK;
                Refresh();
            }

            eventType = EventType.DRAW;

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

                if (textBoxRequest.Value.useNumericFilter && !textBoxHasNumericFilter)
                {
                    textBox1.KeyPress += TextBox1_KeyPress;
                    textBoxHasNumericFilter = true;
                }
                else if (textBoxHasNumericFilter)
                {
                    textBox1.KeyPress -= TextBox1_KeyPress;
                    textBoxHasNumericFilter = false;
                }

                textBoxRequest = null;
            }

            changeTypes = 0;
        }

        protected bool TryInitDrawing(PaintEventArgs e)
        {
            if (HasNoUIContent())
                return false;

            g = e.Graphics;

            if (comboBox1.Visible)
            {
                g.DrawString(comboBoxName, Font, SystemBrushes.ControlText, 10, 10);
                return false;
            }

            index = 0;

            return true;
        }

        protected virtual void DrawField(int x, int y, int width, string value, Brush outline, Brush background, bool isCentered = true)
        {
            g.FillRectangle(outline, x, y, width, textBoxHeight + 2);
            g.FillRectangle(background, x + 1, y + 1, width - 2, textBoxHeight);

            g.SetClip(new Rectangle(
                x + 1,
                y + 1,
                width - 2,
                textBoxHeight));

            if (isCentered)
                g.DrawString(value, Font, SystemBrushes.ControlText,
                x + 1 + (width - (int)Math.Ceiling(g.MeasureString(value, Font).Width)) / 2, y);
            else
                g.DrawString(value, Font, SystemBrushes.ControlText,
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

        protected class ControlInvalidatedException : Exception { }

        int autoScrollRestoreHeight;
        int autoScrollRestoreY;
        string comboBoxName;

        protected void ShowComboBox(string name, string text, object[] recommendations)
        {
            comboBoxName = name;
            autoScrollRestoreHeight = AutoScrollMinSize.Height;
            autoScrollRestoreY = -AutoScrollPosition.Y;
            comboBox1.Text = text;
            comboBox1.Items.Clear();
            if (recommendations != null && recommendations.Length != 0)
                comboBox1.Items.AddRange(recommendations);
            comboBox1.Visible = true;
            comboBox1.Focus();
            Invalidate();
            focusedIndex = index;
            changeTypes |= VALUE_CHANGE_START;
            throw new ControlInvalidatedException();
        }

        #region UI Elements
        float valueBeforeDrag;
        protected float NumericInputField(int x, int y, int width, float number, NumberInputInfo info, bool isCentered)
        {
            switch (eventType)
            {
                case EventType.CLICK:
                    if (new Rectangle(x + 1, y + 1, width - 1, textBoxHeight - 2).Contains(mousePos))
                    {
                        DrawField(x, y, width, "", SystemBrushes.ActiveBorder, SystemBrushes.ControlLightLight, isCentered);
                        PrepareFieldForInput(x, y, width, number.ToString(), isCentered);
                    }
                    else
                        DrawField(x, y, width, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, isCentered);

                    break;

                case EventType.DRAG_START:
                    if (focusedIndex == index)
                        DrawField(x, y, width, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, isCentered);
                    else
                    {
                        DrawField(x, y, width, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, isCentered);

                        if (new Rectangle(x + 1, y + 1, width - 1, textBoxHeight - 2).Contains(mousePos))
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
                        DrawField(x, y, width, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, isCentered);
                    else
                        DrawField(x, y, width, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, isCentered);

                    break;

                case EventType.DRAG_END:
                    if (dragIndex == index)
                    {
                        changeTypes |= VALUE_SET;
                        dragIndex = -1;
                    }

                    if (focusedIndex == index)
                        DrawField(x, y, width, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, isCentered);
                    else
                        DrawField(x, y, width, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, isCentered);

                    break;

                case EventType.LOST_FOCUS:
                    if (focusedIndex == index && float.TryParse(textBox1.Text, out float parsed))
                    {
                        changeTypes |= VALUE_SET;
                        number = Clamped(parsed, info.min, info.max, info.wrapAround);
                    }

                    if (focusedIndex == index)
                        DrawField(x, y, width, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, isCentered);
                    else
                        DrawField(x, y, width, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, isCentered);

                    break;

                case EventType.DRAG_ABORT:
                    if (dragIndex == index)
                    {
                        changeTypes |= VALUE_CHANGED;
                        number = valueBeforeDrag;
                        dragIndex = -1;
                    }
                    if (focusedIndex == index)
                        DrawField(x, y, width, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, isCentered);
                    else
                        DrawField(x, y, width, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, isCentered);

                    break;

                default: //EventType.DRAW
                    if (focusedIndex == index)
                        DrawField(x, y, width, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, isCentered);
                    else
                        DrawField(x, y, width, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, isCentered);

                    break;
            }

            index++;
            return number;
        }

        protected string TextInputField(int x, int y, int width, string text, bool isCentered)
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

        protected virtual void DrawText(int x, int y, string text)
        {
            g.DrawString(text, Font, SystemBrushes.ControlText, x, y);
        }

        protected virtual void DrawLink(int x, int y, string text, InteractionType linkInteraction)
        {
            switch (linkInteraction)
            {
                case InteractionType.MOUSE_DOWN:
                    g.DrawString(text, LinkFont, Brushes.Red, x, y);
                    break;
                case InteractionType.HOVER:
                    g.DrawString(text, LinkFont, Brushes.Blue, x, y);
                    break;
                default:
                    g.DrawString(text, Font, Brushes.Blue, x, y);
                    break;
            }
        }

        protected enum InteractionType
        {
            NONE,
            HOVER,
            MOUSE_DOWN
        }

        protected virtual void DrawButton(int x, int y, int width, string name, InteractionType buttonInteraction)
        {
            switch (buttonInteraction)
            {
                case InteractionType.MOUSE_DOWN:
                    g.FillRectangle(SystemBrushes.HotTrack, x, y, width, textBoxHeight + 6);
                    g.FillRectangle(SystemBrushes.GradientInactiveCaption, x + 1, y + 1, width - 2, textBoxHeight + 4);
                    break;
                case InteractionType.HOVER:
                    g.FillRectangle(SystemBrushes.Highlight, x, y, width, textBoxHeight + 6);
                    g.FillRectangle(buttonHighlight, x + 1, y + 1, width - 2, textBoxHeight + 4);
                    break;
                default:
                    g.FillRectangle(SystemBrushes.ControlDark, x, y, width, textBoxHeight + 6);
                    g.FillRectangle(SystemBrushes.ControlLight, x + 1, y + 1, width - 2, textBoxHeight + 4);
                    break;
            }
        }

        protected bool Button(int x, int y, int width, string name)
        {
            bool clicked = false;

            if (new Rectangle(x, y, width, textBoxHeight + 6).Contains(mousePos))
            {
                if (mouseDown)
                {
                    DrawButton(x, y, width, name, InteractionType.MOUSE_DOWN);
                }
                else
                {
                    DrawButton(x, y, width, name, InteractionType.HOVER);
                }

                clicked = eventType == EventType.CLICK;
            }
            else
            {
                DrawButton(x, y, width, name, InteractionType.NONE);
            }

            g.DrawString(name, Font, SystemBrushes.ControlText,
                x + (width - (int)g.MeasureString(name, Font).Width) / 2, y + 3);

            index++;

            return clicked;
        }

        protected object ChoicePickerField(int x, int y, int width, object value, IList values)
        {
            int clickAreaWidth = width / 3;
            DrawField(x, y, width, value.ToString(), SystemBrushes.ActiveBorder, SystemBrushes.ControlLightLight);

            float arrowWidth = g.MeasureString(">", Font).Width;

            int index = values.IndexOf(value);
            if (index > 0)
            {
                g.DrawString("<", Font, SystemBrushes.ControlText, x + 1, y);
                if (new Rectangle(x + 1, y + 1, clickAreaWidth, textBoxHeight - 2).Contains(mousePos))
                {
                    if (eventType == EventType.DRAG_START)
                        changeTypes |= VALUE_CHANGE_START;

                    if (eventType == EventType.CLICK)
                        value = values[index - 1];
                }
            }

            if (index < values.Count - 1)
            {
                g.DrawString(">", Font, SystemBrushes.ControlText, x + width - 1 - arrowWidth, y);
                if (new Rectangle(x + width - clickAreaWidth, y + 1, clickAreaWidth, textBoxHeight - 2).Contains(mousePos))
                {
                    if (eventType == EventType.DRAG_START)
                        changeTypes |= VALUE_CHANGE_START;

                    if (eventType == EventType.CLICK)
                        value = values[index + 1];
                }
            }

            return value;
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
}