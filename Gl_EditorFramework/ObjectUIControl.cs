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

namespace GL_EditorFramework
{
    /// <summary>
    /// A control for displaying object specific UI that an <see cref="IObjectUIProvider"/> provides
    /// </summary>
    public partial class ObjectUIControl : UserControl, IObjectUIControl
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        protected static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, int lParam);

        public Font HeaderFont;
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

        float[] values = new float[3];

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

            HeaderFont = new Font(textBox1.Font.FontFamily, 10);

            LinkFont = new Font(textBox1.Font, FontStyle.Underline);

            doubleClickTimer.Interval = SystemInformation.DoubleClickTime;
            doubleClickTimer.Tick += DoubleClickTimer_Tick;

            textBox1.Visible = false;
            textBoxHeight = textBox1.Height;
            textBox1.KeyPress += TextBox1_KeyPress;
            textBox1.KeyDown += TextBox1_KeyDown;
            textBox1.LostFocus += TextBox1_LostFocus;
            textBox1.MouseClick += TextBox1_MouseClick;
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

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return && textBox1.Focused)
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
            if (!char.IsDigit(e.KeyChar) && !text.Equals(numberDecimalSeparator) && !text.Equals(numberGroupSeparator) && !text.Equals(negativeSign) && e.KeyChar != '\b' && (ModifierKeys & (Keys.Control | Keys.Alt)) == Keys.None)
            {
                e.Handled = true;
            }
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            Refresh();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (objectUIProvider == null)
                return;

            g = e.Graphics;

            currentY = 10 + AutoScrollPosition.Y;

            index = 0;

            usableWidth = Width - 10;

            objectUIProvider.DoUI(this);

            AutoScrollMinSize = new Size(0, currentY - AutoScrollPosition.Y);
        }

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

            changeTypes = 0;
        }

        private void DrawField(int textX, int fieldX, int y, int width, string name, string value, Brush outline, Brush background)
        {
            g.FillRectangle(outline, fieldX, y, width, textBoxHeight + 2);
            g.FillRectangle(background, fieldX + 1, y + 1, width - 2, textBoxHeight);

            g.DrawString(name + ": ", textBox1.Font, SystemBrushes.ControlText, textX, y);

            g.SetClip(new Rectangle(
                fieldX + 1,
                y + 1,
                width - 2,
                textBoxHeight));

            g.DrawString(value, textBox1.Font, SystemBrushes.ControlText,
                fieldX + 1 + (width - (int)g.MeasureString(value, textBox1.Font).Width) / 2, y);

            g.ResetClip();
        }

        private void PrepareFieldForInput(int fieldX, int y, int width, string name, string value)
        {
            int stringWidth = (int)g.MeasureString(name, textBox1.Font).Width;

            textBox1.Text = value;
            textBox1.Location = new Point(fieldX + 1, y + 1);
            textBox1.Width = width - 2;
            textBox1.Visible = true;
            textBox1.Focus();

            int lParam = mousePos.Y - textBox1.Top << 16 | (mousePos.X - textBox1.Left & 65535);
            int num = (int)SendMessage(new HandleRef(this, textBox1.Handle), 215, 0, lParam);

            textBox1.Select(Math.Max(0, num), 0);

            focusedIndex = index;

            acceptDoubleClick = true;
            doubleClickTimer.Start();
        }


        #region Availible UI Elements
        float valueBeforeDrag;
        public float NumberInput(float number, string name, float increment = 1, int incrementDragDivider = 8)
        {
            switch (eventType)
            {
                case EventType.CLICK:
                    if (new Rectangle(usableWidth - 89, currentY + 1, 78, textBoxHeight - 2).Contains(mousePos))
                    {
                        DrawField(15, usableWidth - 90, currentY, 80, name, "", SystemBrushes.ActiveBorder, SystemBrushes.ControlLightLight);
                        PrepareFieldForInput(usableWidth - 90, currentY, 80, name, number.ToString());
                    }
                    else
                        DrawField(15, usableWidth - 90, currentY, 80, name, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);

                    currentY += 20;
                    index++;
                    return number;

                case EventType.DRAG_START:
                    if (focusedIndex == index)
                        DrawField(15, usableWidth - 90, currentY, 80, name, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                    else
                    {
                        DrawField(15, usableWidth - 90, currentY, 80, name, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);

                        if (new Rectangle(usableWidth - 89, currentY + 1, 78, textBoxHeight - 2).Contains(mousePos))
                        {
                            dragIndex = index;
                            valueBeforeDrag = number;
                            changeTypes |= VALUE_CHANGE_START;
                        }
                    }

                    currentY += 20;
                    index++;
                    return number;

                case EventType.DRAG:
                    if (dragIndex == index)
                    {
                        changeTypes |= VALUE_CHANGED;
                        number = valueBeforeDrag + (mousePos.X - dragStarPos.X) / incrementDragDivider * increment;
                    }
                    if (focusedIndex == index)
                        DrawField(15, usableWidth - 90, currentY, 80, name, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                    else
                        DrawField(15, usableWidth - 90, currentY, 80, name, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);

                    currentY += 20;
                    index++;
                    return number;

                case EventType.DRAG_END:
                    if (dragIndex == index)
                    {
                        changeTypes |= VALUE_SET;
                        dragIndex = -1;
                    }

                    if (focusedIndex == index)
                        DrawField(15, usableWidth - 90, currentY, 80, name, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                    else
                        DrawField(15, usableWidth - 90, currentY, 80, name, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);

                    currentY += 20;
                    index++;
                    return number;

                case EventType.LOST_FOCUS:
                    if (focusedIndex == index && float.TryParse(textBox1.Text, out float parsed))
                    {
                        changeTypes |= VALUE_SET;
                        number = parsed;
                    }

                    if (focusedIndex == index)
                        DrawField(15, usableWidth - 90, currentY, 80, name, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                    else
                        DrawField(15, usableWidth - 90, currentY, 80, name, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);

                    currentY += 20;
                    index++;
                    return number;

                case EventType.DRAG_ABORT:
                    if (dragIndex == index)
                    {
                        changeTypes |= VALUE_CHANGED;
                        number = valueBeforeDrag;
                        dragIndex = -1;
                    }
                    if (focusedIndex == index)
                        DrawField(15, usableWidth - 90, currentY, 80, name, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                    else
                        DrawField(15, usableWidth - 90, currentY, 80, name, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);

                    currentY += 20;
                    index++;
                    return number;

                default: //EventType.DRAW
                    if (focusedIndex == index)
                        DrawField(15, usableWidth - 90, currentY, 80, name, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                    else
                        DrawField(15, usableWidth - 90, currentY, 80, name, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);

                    currentY += 20;
                    index++;
                    return number;
            }
        }

        string[] coordNames = new string[]
        {
            "X",
            "Y",
            "Z"
        };

        public OpenTK.Vector3 Vector3Input(OpenTK.Vector3 vec, string name, float increment = 1, int incrementDragDivider = 8)
        {
            int width = (usableWidth - 20) / 3;

            float[] vector = new float[] { vec.X, vec.Y, vec.Z };

            currentY += 5;
            g.DrawString(name, HeaderFont, SystemBrushes.ControlText, 15, currentY);
            currentY += 20;

            switch (eventType)
            {
                case EventType.CLICK:
                    for (int i = 0; i < 3; i++)
                    {
                        if (focusedIndex == index)
                            DrawField(15 + width * i, 30 + width * i, currentY, width - 20, coordNames[i], "",
                                SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                        else
                        {
                            if (new Rectangle(31 + width * i, currentY + 1, width - 22, textBoxHeight - 2).Contains(mousePos))
                            {
                                DrawField(15 + width * i, 30 + width * i, currentY, width - 20, coordNames[i], vector[i].ToString(),
                                SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);

                                PrepareFieldForInput(30 + width * i, currentY, width - 20, name, vector[i].ToString());
                            }
                            else
                                DrawField(15 + width * i, 30 + width * i, currentY, width - 20, coordNames[i], vector[i].ToString(),
                                SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);
                        }
                        index++;
                    }

                    currentY += 30;
                    return vec;

                case EventType.DRAG_START:
                    for (int i = 0; i < 3; i++)
                    {
                        if (focusedIndex == index)
                            DrawField(15 + width * i, 30 + width * i, currentY, width - 20, coordNames[i], "",
                                SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                        else
                        {
                            if (new Rectangle(31 + width * i, currentY + 1, width - 22, textBoxHeight - 2).Contains(mousePos))
                            {
                                dragIndex = index;
                                changeTypes |= VALUE_CHANGE_START;
                                valueBeforeDrag = vector[i];
                            }
                            DrawField(15 + width * i, 30 + width * i, currentY, width - 20, coordNames[i], vector[i].ToString(),
                                (focusedIndex == index) ? SystemBrushes.ActiveCaption : SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);
                        }
                        index++;
                    }

                    currentY += 30;
                    return vec;

                case EventType.DRAG:
                    for (int i = 0; i < 3; i++)
                    {
                        if (dragIndex == index)
                        {
                            changeTypes |= VALUE_CHANGED;

                            vector[i] = valueBeforeDrag + (mousePos.X - dragStarPos.X) / incrementDragDivider * increment;
                        }

                        if (focusedIndex == index)
                            DrawField(15 + width * i, 30 + width * i, currentY, width - 20, coordNames[i], "",
                                SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                        else
                            DrawField(15 + width * i, 30 + width * i, currentY, width - 20, coordNames[i], vector[i].ToString(),
                                SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);

                        index++;
                    }
                    currentY += 30;

                    return new OpenTK.Vector3(vector[0], vector[1], vector[2]);

                case EventType.DRAG_END:
                    for (int i = 0; i < 3; i++)
                    {
                        if (dragIndex == index)
                        {
                            changeTypes |= VALUE_SET;
                            dragIndex = -1;
                        }

                        if (focusedIndex == index)
                            DrawField(15 + width * i, 30 + width * i, currentY, width - 20, coordNames[i], "",
                                SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                        else
                            DrawField(15 + width * i, 30 + width * i, currentY, width - 20, coordNames[i], vector[i].ToString(),
                                SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);

                        index++;
                    }

                    currentY += 30;
                    return vec;

                case EventType.LOST_FOCUS:
                    for (int i = 0; i < 3; i++)
                    {
                        if (focusedIndex == index && float.TryParse(textBox1.Text, out float parsed))
                        {
                            changeTypes |= VALUE_SET;
                            vector[i] = parsed;
                        }
                        DrawField(15 + width * i, 30 + width * i, currentY, width - 20, coordNames[i], vector[i].ToString(),
                            (focusedIndex == index) ? SystemBrushes.ActiveCaption : SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);

                        index++;
                    }

                    currentY += 30;
                    return new OpenTK.Vector3(vector[0], vector[1], vector[2]);

                case EventType.DRAG_ABORT:
                    for (int i = 0; i < 3; i++)
                    {
                        if (dragIndex == index)
                        {
                            changeTypes |= VALUE_SET;

                            vector[i] = valueBeforeDrag;

                            dragIndex = -1;
                        }

                        if (focusedIndex == index)
                            DrawField(15 + width * i, 30 + width * i, currentY, width - 20, coordNames[i], "",
                                SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                        else
                            DrawField(15 + width * i, 30 + width * i, currentY, width - 20, coordNames[i], vector[i].ToString(),
                                SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);

                        index++;
                    }
                    currentY += 30;

                    return new OpenTK.Vector3(vector[0], vector[1], vector[2]);

                default: //EventType.DRAW
                    for (int i = 0; i < 3; i++)
                    {
                        if (focusedIndex == index)
                            DrawField(15 + width * i, 30 + width * i, currentY, width - 20, coordNames[i], "",
                                SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                        else
                            DrawField(15 + width * i, 30 + width * i, currentY, width - 20, coordNames[i], vector[i].ToString(),
                                SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);

                        index++;
                    }

                    currentY += 30;
                    return vec;
            }
        }

        public bool Button(string name)
        {
            bool clicked = false;

            if (new Rectangle(15, currentY, usableWidth - 25, textBoxHeight + 6).Contains(mousePos))
            {
                if (mouseDown)
                {
                    g.FillRectangle(SystemBrushes.HotTrack, 15, currentY, usableWidth - 25, textBoxHeight + 6);
                    g.FillRectangle(SystemBrushes.GradientInactiveCaption, 16, currentY + 1, usableWidth - 27, textBoxHeight + 4);
                }
                else
                {
                    g.FillRectangle(SystemBrushes.Highlight, 15, currentY, usableWidth - 25, textBoxHeight + 6);
                    g.FillRectangle(buttonHighlight, 16, currentY + 1, usableWidth - 27, textBoxHeight + 4);
                }

                clicked = eventType == EventType.CLICK;
            }
            else
            {
                g.FillRectangle(SystemBrushes.ControlDark, 15, currentY, usableWidth - 25, textBoxHeight + 6);
                g.FillRectangle(SystemBrushes.ControlLight, 16, currentY + 1, usableWidth - 27, textBoxHeight + 4);


            }

            g.DrawString(name, textBox1.Font, SystemBrushes.ControlText,
                (usableWidth - 25 - (int)g.MeasureString(name, textBox1.Font).Width) / 2, currentY + 3);

            currentY += 20;
            index++;

            return clicked;
        }

        public bool Link(string name)
        {
            bool clicked = false;

            if (new Rectangle(15, currentY, (int)g.MeasureString(name, textBox1.Font).Width, textBoxHeight).Contains(mousePos))
            {
                if (mouseDown)
                    g.DrawString(name, LinkFont, Brushes.Red, 15, currentY);
                else
                    g.DrawString(name, LinkFont, Brushes.Blue, 15, currentY);

                clicked = eventType == EventType.CLICK;
            }
            else
            {
                g.DrawString(name, LinkFont, Brushes.Blue, 15, currentY);


            }

            currentY += 20;
            index++;

            return clicked;
        }

        public bool CheckBox(string name, bool isChecked)
        {
            if (new Rectangle(usableWidth - 29, currentY + 1, 18, textBoxHeight - 2).Contains(mousePos))
            {
                if (eventType == EventType.DRAG_START)
                    changeTypes |= VALUE_CHANGE_START;

                if (eventType == EventType.CLICK)
                {
                    isChecked = !isChecked;
                    changeTypes |= VALUE_SET;
                }

                DrawField(15, usableWidth - 30, currentY, 20, name, isChecked ? "✔" : "", SystemBrushes.ActiveBorder, SystemBrushes.ControlLightLight);
            }
            else
            {
                DrawField(15, usableWidth - 30, currentY, 20, name, isChecked ? "✔" : "", SystemBrushes.ActiveBorder, SystemBrushes.ControlLightLight);
            }

            currentY += 20;
            index++;

            return isChecked;
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
        float NumberInput(float number, string name, float increment = 1f, int incrementDragDivider = 8);
        OpenTK.Vector3 Vector3Input(OpenTK.Vector3 vec, string name, float increment = 1f, int incrementDragDivider = 8);
        bool Button(string name);
        bool Link(string name);
        bool CheckBox(string name, bool isChecked);
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
