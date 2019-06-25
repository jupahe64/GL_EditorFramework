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

        enum ChangeType
        {
            NONE,
            START,
            CHANGED,
            SET
        }

        EventType eventType = EventType.DRAW;
        ChangeType changeType = ChangeType.NONE;

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

        float[] values = new float[3];

        int focusedIndex = -1;
        int dragIndex = -1;

        bool mouseWasDragged = false;

        int textBoxHeight;
        public ObjectPropertyControl()
        {
            InitializeComponent();
            textBox1.Visible = false;
            textBoxHeight = textBox1.Height;
            textBox1.KeyPress += TextBox1_KeyPress;
            textBox1.KeyDown += TextBox1_KeyDown;
            textBox1.LostFocus += TextBox1_LostFocus;
        }

        private void TextBox1_LostFocus(object sender, EventArgs e)
        {
            if (focusedIndex == -1)
                return;

            eventType = EventType.LOST_FOCUS;
            changeType = ChangeType.NONE;

            Refresh();
            textBox1.Visible = false;
            focusedIndex = -1;

            eventType = EventType.DRAW;

            if (changeType == ChangeType.START)
                ValueChangeStart.Invoke(this, e);
            else if (changeType == ChangeType.CHANGED)
                ValueChanged.Invoke(this, e);
            else if (changeType == ChangeType.SET)
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

        bool nothingWasClicked = true;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouseWasDragged = false;
                dragStarPos = e.Location;
                eventType = EventType.DRAG_START;
                changeType = ChangeType.NONE;
                nothingWasClicked = true;

                Refresh();

                if (nothingWasClicked)
                    Focus();

                eventType = EventType.DRAW;
            }
            else if (e.Button == MouseButtons.Right)
            {
                eventType = EventType.DRAG_ABORT;
                changeType = ChangeType.NONE;
                Refresh();
                eventType = EventType.DRAW;
            }

            if (changeType == ChangeType.START)
                ValueChangeStart.Invoke(this, e);
            else if (changeType == ChangeType.CHANGED)
                ValueChanged.Invoke(this, e);
            else if (changeType == ChangeType.SET)
                ValueSet.Invoke(this, e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            mousePos = e.Location;

            if (e.Button==MouseButtons.Left && Math.Abs(mousePos.X - dragStarPos.X) > 2)
                mouseWasDragged = true;

            if(mouseWasDragged)
                eventType = EventType.DRAG;

            changeType = ChangeType.NONE;
            Refresh();

            eventType = EventType.DRAW;
            lastMousePos = e.Location;

            if (changeType == ChangeType.START)
                ValueChangeStart.Invoke(this, e);
            else if (changeType == ChangeType.CHANGED)
                ValueChanged.Invoke(this, e);
            else if (changeType == ChangeType.SET)
                ValueSet.Invoke(this, e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (mouseWasDragged)
            {
                eventType = EventType.DRAG_END;
                changeType = ChangeType.NONE;
                Refresh();
            }
            else{
                if (propertyContainer == null)
                    return;
                
                eventType = EventType.CLICK;
                changeType = ChangeType.NONE;
                Refresh();
            }

            eventType = EventType.DRAW;

            if (changeType == ChangeType.START)
                ValueChangeStart.Invoke(this, e);
            else if (changeType == ChangeType.CHANGED)
                ValueChanged.Invoke(this, e);
            else if (changeType == ChangeType.SET)
                ValueSet.Invoke(this, e);
        }

        private void DrawField(int x, int y, int width, string name, string value, Brush outline, Brush background)
        {
            g.ResetClip();
            g.FillRectangle(outline,    x+4, y,   width-8, textBoxHeight+2);
            g.FillRectangle(outline,    x+2, y+1, width-4, textBoxHeight);
            g.FillRectangle(outline,    x+1, y+2, width-2, textBoxHeight-2);
            g.FillRectangle(outline,    x,   y+3, width,   textBoxHeight-4);

            g.FillRectangle(background, x+4, y+1, width-8, textBoxHeight);
            g.FillRectangle(background, x+2, y+2, width-4, textBoxHeight-2);
            g.FillRectangle(background, x+1, y+4, width-2, textBoxHeight-6);

            g.SetClip(new Rectangle(
                x + 4,
                y + 1,
                width - 8,
                textBox1.Height));

            g.DrawString(name + ": " + value, textBox1.Font, Brushes.Black, x+2, currentY);
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

        private void PrepareFieldForInput(int x, int y, int width, string name, string value)
        {
            textBox1.Text = value;
            textBox1.Location = new Point(x + 4 + (int)g.MeasureString(name + ": ", textBox1.Font).Width, y+1);
            textBox1.Width = width - 8 - (int)g.MeasureString(name + ": ", textBox1.Font).Width;
            textBox1.Visible = true;
            textBox1.Focus();

            int lParam = mousePos.Y - textBox1.Top << 16 | (mousePos.X - textBox1.Left & 65535);
            int num = (int)SendMessage(new HandleRef(this, textBox1.Handle), 215, 0, lParam);

            textBox1.Select(Math.Max(0, num), 0);
            
            focusedIndex = index;
        }



        float valueBeforeDrag;
        public float NumberInput(float number, string name, float increment=1)
        {
            switch (eventType)
            {
                case EventType.DRAW:
                    if(focusedIndex == index)
                        DrawField(10, currentY, 80, name, "", Brushes.Turquoise, Brushes.White);
                    else
                        DrawField(10, currentY, 80, name, number.ToString(), Brushes.LightGray, Brushes.White);

                    currentY += 20;
                    index++;
                    return number;

                case EventType.CLICK:
                    DrawField(10, currentY, 80, name, "", Brushes.LightBlue, Brushes.White);
                    if (new Rectangle(10, currentY, 80, textBoxHeight).Contains(mousePos))
                        PrepareFieldForInput(10, currentY, 80, name, number.ToString());

                    currentY += 20;
                    index++;
                    return number;

                case EventType.DRAG_START:
                    DrawField(10, currentY, 80, name, number.ToString(), Brushes.LightGray, Brushes.White);

                    if (new Rectangle(10, currentY, 80, textBoxHeight).Contains(mousePos))
                    {
                        dragIndex = index;
                        valueBeforeDrag = number;
                        nothingWasClicked = false;
                    }

                    currentY += 20;
                    index++;
                    changeType = ChangeType.START;
                    return number;

                case EventType.DRAG:
                    if (dragIndex == index)
                    {
                        DrawField(10, currentY, 80, name, (valueBeforeDrag + (mousePos.X - dragStarPos.X)/2*increment).ToString(), Brushes.LightGray, Brushes.White);

                        currentY += 20;
                        index++;
                        changeType = ChangeType.CHANGED;
                        return valueBeforeDrag + (mousePos.X - dragStarPos.X) / 2 * increment;
                    }
                    else
                    {
                        DrawField(10, currentY, 80, name, number.ToString(), Brushes.LightGray, Brushes.White);

                        currentY += 20;
                        index++;
                        return number;
                    }

                case EventType.DRAG_END:
                    DrawField(10, currentY, 80, name, number.ToString(), Brushes.LightGray, Brushes.White);

                    currentY += 20;
                    dragIndex = -1;
                    index++;
                    changeType = ChangeType.SET;
                    return number;

                case EventType.LOST_FOCUS:
                    DrawField(10, currentY, 80, name, number.ToString(), Brushes.LightGray, Brushes.White);

                    currentY += 20;
                    if (focusedIndex == index && float.TryParse(textBox1.Text, out float parsed))
                    {
                        index++;
                        changeType = ChangeType.SET;
                        return parsed;
                    }
                    else
                    {
                        index++;
                        return number;
                    }

                case EventType.DRAG_ABORT:
                    if (dragIndex == index)
                    {
                        DrawField(10, currentY, 80, name, valueBeforeDrag.ToString(), Brushes.LightGray, Brushes.White);
                        currentY += 20;
                        index++;
                        dragIndex = -1;
                        changeType = ChangeType.CHANGED;
                        return valueBeforeDrag;
                    }
                    else
                    {
                        DrawField(10, currentY, 80, name, number.ToString(), Brushes.LightGray, Brushes.White);
                        currentY += 20;
                        index++;
                        return number;
                    }

                default:
                    currentY += 20;
                    index++;
                    return number;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }
    }

    public interface IObjectPropertyControl
    {
        float NumberInput(float number, string name, float increment);
    }

    public abstract class AbstractPropertyContainer
    {
        public abstract void DoUI(IObjectPropertyControl control);
    }
}
