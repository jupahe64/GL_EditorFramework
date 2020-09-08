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
            LOST_FOCUS,
            RIGHT_CLICK
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

        protected Brush buttonHighlight = new SolidBrush(Framework.MixedColor(SystemColors.GradientInactiveCaption, SystemColors.ControlLightLight));

        Timer doubleClickTimer = new Timer();
        bool acceptDoubleClick = false;

        protected bool mouseDown = false;

        protected int focusedIndex = -1;
        protected int dragIndex = -1;

        bool mouseWasDragged = false;
        protected readonly int textBoxHeight;

        bool textBoxHasNumericFilter = false;

        protected TextBox textBox1;

        protected SuggestionDropDown suggestionsDropDown = new SuggestionDropDown();

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
                TextAlign = HorizontalAlignment.Center,
                Visible = false
            };
            textBox1.MouseClick += TextBox1_MouseClick;
            textBox1.KeyDown += TextBox1_KeyDown;
            textBox1.LostFocus += TextBox1_LostFocus;
            textBox1.GotFocus += TextBox1_GotFocus;
            textBox1.TextChanged += TextBox1_TextChanged;

            suggestionsDropDown.ItemSelected += SuggestionsDropDown_ItemSelected;

            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            Controls.Add(textBox1);
            Size = new Size(220, 160);
            ResumeLayout(false);
            PerformLayout();


            HeadingFont = new Font(Font.FontFamily, 10);

            LinkFont = new Font(Font, FontStyle.Underline);

            doubleClickTimer.Interval = SystemInformation.DoubleClickTime;
            doubleClickTimer.Tick += DoubleClickTimer_Tick;

            textBoxHeight = textBox1.Height;
        }

        private void SuggestionsDropDown_ItemSelected(object sender, EventArgs e)
        {
            textBox1.Text = suggestionsDropDown.SelectedSuggestion;
            Focus();
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

        public void UnFocusInput()
        {
            if (textBox1.Focused)
                Focus();
        }

        private void TextBox1_GotFocus(object sender, EventArgs e)
        {
            if (suggestionsForTextBox.Length != 0)
            {
                suggestionsDropDown.Show(textBox1.PointToScreen(new Point(-1, textBox1.Height - 1)), textBox1.Width, textBox1.Text, suggestionsForTextBox, true, filterSuggestionsForTextBox);
            }
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

            suggestionsDropDown.Hide();

            changeTypes = 0;
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            suggestionsDropDown.UpdateSuggestions(textBox1.Text);
        }

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return && textBox1.Focused)
            {
                Focus();
                e.SuppressKeyPress = true;
            }
        }

        #region make sure suggestionDropDown behaves correctly
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);

            Form parentForm = this.FindForm();

            if (parentForm != null)
            {
                parentForm.LocationChanged += ParentForm_LocationChanged;
                parentForm.SizeChanged += ParentForm_SizeChanged;

                parentForm.FormClosed += ParentForm_FormClosed;
            }
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);

            Form parentForm = this.FindForm();

            if (parentForm != null)
            {
                parentForm.LocationChanged += ParentForm_LocationChanged;
                parentForm.SizeChanged += ParentForm_SizeChanged;

                parentForm.FormClosed += ParentForm_FormClosed;
            }
        }

        private void ParentForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            suggestionsDropDown.Close();
        }

        private void ParentForm_SizeChanged(object sender, EventArgs e)
        {
            suggestionsDropDown.Width = textBox1.Width;
        }

        private void ParentForm_LocationChanged(object sender, EventArgs e)
        {
            suggestionsDropDown.Location = textBox1.PointToScreen(new Point(-1, textBox1.Height - 1));
        }
        #endregion

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
            else if (e.Button == MouseButtons.Right && mouseDown)
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
            if ((e.Button != MouseButtons.Left && e.Button != MouseButtons.Right) || HasNoUIContent())
                return;
            mouseDown = false;

            if (mouseWasDragged)
            {
                eventType = EventType.DRAG_END;
                Refresh();
            }
            else
            {
                eventType = (e.Button == MouseButtons.Left) ? EventType.CLICK : EventType.RIGHT_CLICK;
                Refresh();
            }

            eventType = EventType.DRAW;

            if (textBoxRequest.HasValue)
            {
                suggestionsForTextBox = Array.Empty<string>();

                SuspendLayout();
                textBox1.Visible = true;

                textBox1.Text = textBoxRequest.Value.value;
                textBox1.TextAlign = textBoxRequest.Value.alignment;
                textBox1.Location = new Point(textBoxRequest.Value.x, textBoxRequest.Value.y);
                textBox1.Width = textBoxRequest.Value.width;
                ResumeLayout();

                textBox1.Focus();

                if(focusRequest.HasValue)
                    focusedIndex = focusRequest.Value;

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

            if (comboBoxRequest.HasValue)
            {
                suggestionsForTextBox = comboBoxRequest.Value.items;

                filterSuggestionsForTextBox = comboBoxRequest.Value.filterSuggestions;

                SuspendLayout();
                textBox1.Visible = true;

                textBox1.Text = comboBoxRequest.Value.value;
                textBox1.TextAlign = HorizontalAlignment.Left;
                textBox1.Location = new Point(comboBoxRequest.Value.x, comboBoxRequest.Value.y);
                textBox1.Width = comboBoxRequest.Value.width;
                ResumeLayout();

                textBox1.Focus();

                if (focusRequest.HasValue)
                    focusedIndex = focusRequest.Value;

                int lParam = mousePos.Y - textBox1.Top << 16 | (mousePos.X - textBox1.Left & 65535);
                int num = (int)SendMessage(new HandleRef(this, textBox1.Handle), 215, 0, lParam);

                textBox1.Select(Math.Max(0, num), 0);

                if (textBoxHasNumericFilter)
                {
                    textBox1.KeyPress -= TextBox1_KeyPress;
                    textBoxHasNumericFilter = false;
                }

                comboBoxRequest = null;
            }

            changeTypes = 0;
        }

        private string[] suggestionsForTextBox = Array.Empty<string>();

        private bool filterSuggestionsForTextBox = true;

        protected bool TryInitDrawing(PaintEventArgs e)
        {
            if (HasNoUIContent())
                return false;

            g = e.Graphics;

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

        protected virtual void DrawComboBoxField(int x, int y, int width, string value, Brush outline, Brush background, bool isCentered = true)
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
                x + 1 + (width - (int)Math.Ceiling(g.MeasureString(value, Font).Width)) / 2, y+1);
            else
                g.DrawString(value, Font, SystemBrushes.ControlText,
                x + 1, y+1);

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

        struct ComboBoxSetup
        {
            public int x;
            public int y;
            public int width;
            public string value;
            public string[] items;
            public bool hasTextBox;
            public bool filterSuggestions;

            public ComboBoxSetup(int x, int y, int width, string value, string[] items, bool hasTextBox, bool filterSuggestions)
            {
                this.x = x;
                this.y = y;
                this.width = width;
                this.value = value;
                this.items = items;
                this.hasTextBox = hasTextBox;
                this.filterSuggestions = filterSuggestions;
            }
        }

        int? focusRequest;

        ComboBoxSetup? comboBoxRequest;

        private void PrepareFieldForInput(int x, int y, int width, string value, bool isNumericInput = true, bool isCentered = true)
        {
            textBoxRequest = new TextBoxSetup(x + 1, y + 1, width - 2, value, isCentered ? HorizontalAlignment.Center : HorizontalAlignment.Left, isNumericInput);

            focusRequest = index;

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

        protected void PrepareComboBox(int x, int y, int width, string value, string[] items, bool hasTextBox, bool filterSuggestions)
        {
            comboBoxRequest = new ComboBoxSetup(x, y, width, value, items, hasTextBox, filterSuggestions);

            focusRequest = index;
            changeTypes |= VALUE_CHANGE_START;
        }


        new public ContextMenu ContextMenu { get; set; }

        protected void ShowContextMenu(ContextMenu contextMenu)
        {
            contextMenu.Show(this, mousePos);
        }

        protected void ShowContextMenu()
        {
            ContextMenu.Show(this, mousePos);
        }

        #region UI Elements
        float valueBeforeDrag;
        protected float NumericInputField(int x, int y, int width, float number, NumberInputInfo info, bool isCentered)
        {
            if (focusedIndex == index)
                UpdateTextbox(x, y, width);

            switch (eventType)
            {
                case EventType.CLICK:
                    if (new Rectangle(x + 1, y + 1, width - 1, textBoxHeight - 2).Contains(mousePos))
                    {
                        DrawField(x, y, width, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, isCentered);
                        PrepareFieldForInput(x, y, width, number.ToString(), isCentered);

                        eventType = EventType.DRAW; //Click Handled
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
            if (focusedIndex == index)
                UpdateTextbox(x, y, width);

            switch (eventType)
            {
                case EventType.CLICK:
                    if (new Rectangle(x + 1, y + 1, width - 2, textBoxHeight - 2).Contains(mousePos))
                    {
                        DrawField(x, y, width, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, isCentered);
                        PrepareFieldForInput(x, y, width, text, false, isCentered);

                        eventType = EventType.DRAW; //Click Handled
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

        private void UpdateTextbox(int x, int y, int width)
        {
            textBox1.SetBounds(x + 1, y + 1, width - 2, -1, BoundsSpecified.Location | BoundsSpecified.Width);
            if (suggestionsDropDown.Visible)
            {
                suggestionsDropDown.Location = textBox1.PointToScreen(new Point(-1, textBox1.Height - 1));
                suggestionsDropDown.Width = width;
            }
        }

        protected string DropDownTextInputField(int x, int y, int width, string text, string[] dropDownItems, bool filterSuggestions)
        {
            if (focusedIndex == index)
                UpdateTextbox(x, y, width);

            switch (eventType)
            {
                case EventType.CLICK:
                    if (new Rectangle(x + 1, y + 1, width - 2, textBoxHeight - 2).Contains(mousePos))
                    {
                        PrepareComboBox(x, y, width, text, dropDownItems, true, filterSuggestions);

                        eventType = EventType.DRAW; //Click Handled
                    }
                    else
                        DrawComboBoxField(x, y, width, text, SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, false);

                    break;

                case EventType.LOST_FOCUS:
                    if (focusedIndex == index)
                    {
                        changeTypes |= VALUE_SET;
                        text = textBox1.Text;
                    }

                    if (focusedIndex == index)
                        DrawComboBoxField(x, y, width, "", SystemBrushes.Highlight, SystemBrushes.ControlLightLight, false);
                    else
                        DrawComboBoxField(x, y, width, text, SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, false);

                    break;

                default:
                    if (focusedIndex == index)
                        DrawComboBoxField(x, y, width, "", SystemBrushes.Highlight, SystemBrushes.ControlLightLight, false);
                    else
                        DrawComboBoxField(x, y, width, text, SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, false);

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

                if(clicked)
                    eventType = EventType.DRAW; //Click Handled
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

        protected bool ImageButton(int x, int y, Image image, Image imageClicked = null, Image imageHovered = null)
        {
            bool clicked = false;

            if (new Rectangle(x, y, image.Width, image.Height + 6).Contains(mousePos))
            {
                if (mouseDown)
                {
                    g.DrawImage(imageClicked ?? image, x, y);
                }
                else
                {
                    g.DrawImage(imageHovered ?? image, x, y);
                }

                clicked = eventType == EventType.CLICK;

                if (clicked)
                    eventType = EventType.DRAW; //Click Handled
            }
            else
            {
                g.DrawImage(image, x, y);
            }

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
                    {
                        value = values[index - 1];
                        changeTypes |= VALUE_SET;

                        eventType = EventType.DRAW; //Click Handled
                    }
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
                    {
                        value = values[index + 1];
                        changeTypes |= VALUE_SET;

                        eventType = EventType.DRAW; //Click Handled
                    }
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
    }
}