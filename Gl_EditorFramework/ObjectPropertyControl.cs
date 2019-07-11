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
    public partial class ObjectPropertyControl : UserControl, IObjectPropertyControl
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        protected static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, int lParam);

        public event EventHandler ValueChangeStart;
        public event EventHandler ValueChanged;
        public event EventHandler ValueSet;

        public Font HeaderFont;

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

        AbstractPropertyContainer propertyContainer;

        public AbstractPropertyContainer CurrentPropertyContainer
        {
            get => propertyContainer;

            set
            {
                propertyContainer = value;
                Refresh();
            }
        }

        Graphics g;

        int index;

        Point mousePos;
        Point lastMousePos;
        Point dragStarPos;

        Timer doubleClickTimer = new Timer();
        bool acceptDoubleClick = false;

        float[] values = new float[3];

        int focusedIndex = -1;
        int dragIndex = -1;

        bool mouseWasDragged = false;

        int textBoxHeight;
        public ObjectPropertyControl()
        {
            SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);

            InitializeComponent();

            HeaderFont = new Font(textBox1.Font.FontFamily, 10);

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
            if (acceptDoubleClick || e.Clicks==2)
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
            changeTypes = 0;

            Refresh();

            textBox1.Visible = false;
            focusedIndex = -1;

            eventType = EventType.DRAW;

            if ((changeTypes & VALUE_CHANGE_START)>0)
                ValueChangeStart.Invoke(this, e);
            else if ((changeTypes & VALUE_CHANGED) > 0)
                ValueChanged.Invoke(this, e);
            else if ((changeTypes & VALUE_SET) > 0)
                ValueSet.Invoke(this, e);
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (propertyContainer == null)
                return;

            g = e.Graphics;

            currentY = 10;

            index = 0;

            propertyContainer.DoUI(this);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouseWasDragged = false;
                dragStarPos = e.Location;
                eventType = EventType.DRAG_START;
                dragIndex = -1;
                changeTypes = 0;

                Refresh();

                Focus();

                eventType = EventType.DRAW;
            }
            else if (e.Button == MouseButtons.Right)
            {
                eventType = EventType.DRAG_ABORT;
                changeTypes = 0;
                Refresh();
                eventType = EventType.DRAW;
            }

            if ((changeTypes & VALUE_CHANGE_START) > 0)
                ValueChangeStart.Invoke(this, e);
            else if ((changeTypes & VALUE_CHANGED) > 0)
                ValueChanged.Invoke(this, e);
            else if ((changeTypes & VALUE_SET) > 0)
                ValueSet.Invoke(this, e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            mousePos = e.Location;

            if (e.Button==MouseButtons.Left && Math.Abs(mousePos.X - dragStarPos.X) > 2)
                mouseWasDragged = true;

            if(mouseWasDragged)
                eventType = EventType.DRAG;

            changeTypes = 0;
            Refresh();

            eventType = EventType.DRAW;
            lastMousePos = e.Location;

            if ((changeTypes & VALUE_CHANGE_START) > 0)
                ValueChangeStart.Invoke(this, e);
            else if ((changeTypes & VALUE_CHANGED) > 0)
                ValueChanged.Invoke(this, e);
            else if ((changeTypes & VALUE_SET) > 0)
                ValueSet.Invoke(this, e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (mouseWasDragged)
            {
                eventType = EventType.DRAG_END;
                changeTypes = 0;
                Refresh();
            }
            else{
                if (propertyContainer == null)
                    return;
                
                eventType = EventType.CLICK;
                changeTypes = 0;
                Refresh();
            }

            eventType = EventType.DRAW;

            if ((changeTypes & VALUE_CHANGE_START) > 0)
                ValueChangeStart.Invoke(this, e);
            else if ((changeTypes & VALUE_CHANGED) > 0)
                ValueChanged.Invoke(this, e);
            else if ((changeTypes & VALUE_SET) > 0)
                ValueSet.Invoke(this, e);
        }

        private void DrawField(int textX, int fieldX, int y, int width, string name, string value, Brush outline, Brush background)
        {
            g.FillRectangle(outline, fieldX, y, width, textBoxHeight+2);
            g.FillRectangle(background, fieldX + 1, y + 1,   width-2, textBoxHeight);

            g.DrawString(name + ": ", textBox1.Font, SystemBrushes.ControlText, textX, y);

            g.SetClip(new Rectangle(
                fieldX+1,
                y + 1,
                width-2,
                textBoxHeight));
            
            g.DrawString(value, textBox1.Font, SystemBrushes.ControlText, 
                fieldX+1 + (width-(int)g.MeasureString(value,textBox1.Font).Width)/2, y);

            g.ResetClip();
        }

        private void Unfocus()
        {
            if (focusedIndex != -1)
            {
                int iKeep = index;
                int yKeep = currentY;
                Graphics gKeep = g;
                Focus();
                index = iKeep;
                currentY = yKeep;
                g = gKeep;
            }
        }

        private void PrepareFieldForInput(int fieldX, int y, int width, string name, string value)
        {
            int stringWidth = (int)g.MeasureString(name, textBox1.Font).Width;

            textBox1.Text = value;
            textBox1.Location = new Point(fieldX+1, y+1);
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



        float valueBeforeDrag;
        public float NumberInput(float number, string name, float increment=1, int incrementDragDivider = 8)
        {
            switch (eventType)
            {
                case EventType.CLICK:
                    if (new Rectangle(Width - 89, currentY+1, 78, textBoxHeight-2).Contains(mousePos))
                    {
                        DrawField(15, Width - 90, currentY, 80, name, "", SystemBrushes.ActiveBorder, SystemBrushes.ControlLightLight);
                        PrepareFieldForInput(Width - 90, currentY, 80, name, number.ToString());
                    }
                    else
                        DrawField(15, Width - 90, currentY, 80, name, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);

                    currentY += 20;
                    index++;
                    return number;

                case EventType.DRAG_START:
                    if (focusedIndex == index)
                        DrawField(15, Width - 90, currentY, 80, name, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                    else
                    {
                        DrawField(15, Width - 90, currentY, 80, name, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);

                        if (new Rectangle(Width - 89, currentY+1, 78, textBoxHeight-2).Contains(mousePos))
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
                        DrawField(15, Width - 90, currentY, 80, name, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                    else
                        DrawField(15, Width - 90, currentY, 80, name, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);
                    
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
                        DrawField(15, Width - 90, currentY, 80, name, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                    else
                        DrawField(15, Width - 90, currentY, 80, name, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);
                    
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
                        DrawField(15, Width - 90, currentY, 80, name, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                    else
                        DrawField(15, Width - 90, currentY, 80, name, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);

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
                        DrawField(15, Width - 90, currentY, 80, name, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                    else
                        DrawField(15, Width - 90, currentY, 80, name, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);

                    currentY += 20;
                    index++;
                    return number;

                default: //EventType.DRAW
                    if (focusedIndex == index)
                        DrawField(15, Width - 90, currentY, 80, name, "", SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight);
                    else
                        DrawField(15, Width - 90, currentY, 80, name, number.ToString(), SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight);

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
            int width = (Width - 20) / 3;

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
                            if (new Rectangle(31 + width * i, currentY+1, width - 22, textBoxHeight-2).Contains(mousePos))
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
                            if (new Rectangle(31 + width * i, currentY+1, width - 22, textBoxHeight-2).Contains(mousePos))
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
    }

    public interface IObjectPropertyControl
    {
        float NumberInput(float number, string name, float increment = 1f, int incrementDragDivider = 8);
        OpenTK.Vector3 Vector3Input(OpenTK.Vector3 vec, string name, float increment = 1f, int incrementDragDivider = 8);
    }

    public abstract class AbstractPropertyContainer
    {
        public abstract void DoUI(IObjectPropertyControl control);
    }
}
