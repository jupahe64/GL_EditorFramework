using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GL_EditorFramework
{
    public enum SelectionChangeMode
    {
        SET,
        ADD,
        SUBTRACT
    }

    public class ItemClickedEventArgs : MouseEventArgs
    {
        public object Item { get; }

        public ItemClickedEventArgs(object item, MouseButtons button, int clicks, int x, int y, int delta) :
            base(button, clicks, x, y, delta)
        {
            Item = item;
        }
    }

    public delegate void ItemClickedEventHandler(object sender, ItemClickedEventArgs e);

    public abstract class FastListViewBase : UserControl
    {
        public enum ItemHighlight
        {
            NONE,
            SELECTED,
            HOVERED,
            HOVERED_SELECTED
        }

        protected struct HoveredItem
        {
            public object Item { get; set; }
            public int Index { get; set; }
            public bool IsSelected { get; set; }
        }

        public event EventHandler SelectionChanged;

        int mouseX = 0;
        int mouseY = 0;
        bool mouseDown = false;

        bool selecting = false;

        int itemCount = 0;
        HoveredItem hovered;

        private int selectRangeStart = -1;
        private int prevSelectStartIndex = -1;
        private int selectRangeEnd = -1;
        private bool keepTheRest = true;
        private bool subtract = false;

        public object LastSelectedItem { get; private set; } = null;
        public int LastSelectedIndex { get; private set; } = -1;

        private Timer marginScrollTimer = new Timer();

        private int marginScrollSpeed = 0;

        Timer doubleClickTimer = new Timer();
        Timer searchTypeTimer = new Timer();

        int succesiveClicks = 0;

        public event ItemClickedEventHandler ItemClicked;


        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams baseParams = base.CreateParams;
                const int WS_EX_COMPOSITED = 0x02000000;
                baseParams.ExStyle |= WS_EX_COMPOSITED;

                return baseParams;
            }
        }

        public FastListViewBase()
        {

            DoubleBuffered = true;

            

            marginScrollTimer.Interval = 1;
            marginScrollTimer.Tick += MarginScrollTimer_Tick;

            doubleClickTimer.Interval = SystemInformation.DoubleClickTime;
            searchTypeTimer.Interval = 1000;
            doubleClickTimer.Tick += DoubleClickTimer_Tick;
            searchTypeTimer.Tick += SearchTypeTimer_Tick;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            OnFontChanged(e);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);

            using (Graphics g = CreateGraphics())
            {
                FontHeight = (int)Math.Ceiling(Font.GetHeight(DeviceDpi));
            }
        }

        private void SearchTypeTimer_Tick(object sender, EventArgs e)
        {
            searchTypeTimer.Stop();
            searchString = string.Empty;
        }

        private void MarginScrollTimer_Tick(object sender, EventArgs e)
        {
            if (!VScroll)
                return;

            VerticalScroll.Value = smoothScrollY = Math.Max(VerticalScroll.Minimum, Math.Min(smoothScrollY + marginScrollSpeed, VerticalScroll.Maximum));

            if (selecting && hovered.Index != -1)
                selectRangeEnd = hovered.Index;

            Invalidate();
        }

        private void DoubleClickTimer_Tick(object sender, EventArgs e)
        {
            doubleClickTimer.Stop();
            succesiveClicks = 0;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button != MouseButtons.Left)
                return;

            if (hovered.Index == -1)
                return;

            mouseDown = true;

            int hoveredIndex = hovered.Index;
            bool hoveredIsSelected = hovered.IsSelected;

            if (ModifierKeys.HasFlag(Keys.Shift) && prevSelectStartIndex != -1)
            {
                selectRangeStart = prevSelectStartIndex;
                selectRangeEnd = hoveredIndex;
            }
            else
            {
                selectRangeStart = hoveredIndex;
                selectRangeEnd = selectRangeStart;
            }

            keepTheRest = ModifierKeys.HasFlag(Keys.Control);
            subtract = hoveredIsSelected && keepTheRest;
            selecting = true;
            Invalidate(Bounds);
        }

        int smoothScrollY = 0;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            mouseX = e.X;
            mouseY = e.Y;

            using(var g = CreateGraphics())
            {
                var handler = new DrawItemHandler(g, -1, -1, this);

                DrawItems(handler);

                handler.Evaluate(this);
            }

            if (e.Button != MouseButtons.Left)
                return;

            if (hovered.Index == -1)
                return;

            if (selecting)
                selectRangeEnd = hovered.Index;

            if (e.Y < 0)
            {
                marginScrollSpeed = e.Y;

                if (!marginScrollTimer.Enabled)
                    smoothScrollY = VerticalScroll.Value;

                marginScrollTimer.Start();
            }
            else if (e.Y > Height)
            {
                marginScrollSpeed = e.Y - Height;

                if (!marginScrollTimer.Enabled)
                    smoothScrollY = VerticalScroll.Value;

                marginScrollTimer.Start();
            }
            else
                marginScrollTimer.Stop();

            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button != MouseButtons.Left)
                return;

            if (hovered.Index == -1)
                return;

            mouseDown = false;
            marginScrollTimer.Stop();

            if (selecting)
            {
                int min = Math.Min(selectRangeStart, selectRangeEnd);
                int max = Math.Max(selectRangeStart, selectRangeEnd);

                if (subtract)
                    LastSelectedItem = Select(min, max, SelectionChangeMode.SUBTRACT);
                else if (keepTheRest)
                    LastSelectedItem = Select(min, max, SelectionChangeMode.ADD);
                else
                    LastSelectedItem = Select(min, max, SelectionChangeMode.SET);

                prevSelectStartIndex = selectRangeStart;
                selectRangeStart = -1;
                selectRangeEnd = -1;
                keepTheRest = true;

                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }

            selecting = false;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (itemCount == 0)
                return;

            int index = LastSelectedIndex;

            if (index == -1)
                return;

            bool selectionChanged = false;

            if (e.KeyCode == Keys.Up && index > 0)
            {
                LastSelectedItem = Select(index - 1, e.Shift ? SelectionChangeMode.ADD : SelectionChangeMode.SET);
                selectionChanged = true;
            }
            else if (e.KeyCode == Keys.Down && index < itemCount - 1)
            {
                LastSelectedItem = Select(index + 1, e.Shift ? SelectionChangeMode.ADD : SelectionChangeMode.SET);
                selectionChanged = true;
            }

            Invalidate();

            Focus();

            if (selectionChanged)
                SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        //returns the last selected item or null
        protected abstract object SelectNext(string searchString, int startIndex);

        string searchString = string.Empty;

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            //TODO
            base.OnKeyPress(e);

            if (itemCount == 0)
                return;

            searchTypeTimer.Stop();
            searchTypeTimer.Start();

            searchString += e.KeyChar;

            var result = SelectNext(searchString, (LastSelectedIndex+1)%itemCount);

            if (result == null)
                searchString.Substring(0, searchString.Length - 1);
            else
                LastSelectedItem = result;
        }

        //returns the last selected item or null
        protected abstract object Select(int rangeMin, int rangeMax, SelectionChangeMode selectionChangeMode);

        protected object Select(int index, SelectionChangeMode selectionChangeMode)
        {
            return Select(index, index, selectionChangeMode);
        }

        public sealed class DrawItemHandler
        {
            public readonly Graphics graphics;

            //parameters
            int selectRangeMin;
            int selectRangeMax;

            //counters
            int currentIndex;
            int bottomY;

            //helper
            bool currentIsInRange = false;

            //results
            int hoveredIndex = -1;
            object hoveredItem = null;
            bool hoveredIsSelected = false;
            int lastSelectedIndex = -1;
            int topY;

            //list view variables
            object lastSelectedItem;
            private int mouseX;
            private int mouseY;
            private bool subtract;
            private bool keepTheRest;

            public DrawItemHandler(Graphics graphics, int selectRangeMin, int selectRangeMax, FastListViewBase listView)
            {
                this.graphics = graphics;
                this.selectRangeMin = selectRangeMin;
                this.selectRangeMax = selectRangeMax;

                lastSelectedItem = listView.LastSelectedItem;
                mouseY = listView.mouseY;
                mouseX = listView.mouseX;
                subtract = listView.subtract;
                keepTheRest = listView.keepTheRest;
            }

            public ItemHighlight HandleItem(object item, bool isSelected, int y, int height, int x = 0)
            {
                if (item == lastSelectedItem)
                    lastSelectedIndex = currentIndex;

                if (x <= mouseX && y <= mouseY && mouseY < y + height)
                {
                    hoveredIndex = currentIndex;
                    hoveredItem = item;
                    hoveredIsSelected = isSelected;
                }

                if (currentIndex == selectRangeMin)
                    currentIsInRange = true;
                else if (currentIndex > selectRangeMax)
                    currentIsInRange = false;

                bool _isSelected;

                if (subtract)
                    _isSelected = isSelected && !currentIsInRange;
                else
                    _isSelected = currentIsInRange || (keepTheRest && isSelected);

                bool hovered = hoveredIndex == currentIndex;

                if(currentIndex==0)
                    topY = y;

                bottomY = y + height;

                currentIndex++;

                if (_isSelected && hovered)
                    return ItemHighlight.HOVERED_SELECTED;
                else if (_isSelected)
                    return ItemHighlight.SELECTED;
                else if (hovered)
                    return ItemHighlight.HOVERED;
                else
                    return ItemHighlight.NONE;
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void Evaluate(FastListViewBase listView)
            {
                if (mouseY < topY)
                {
                    hoveredIndex = 0;
                    hoveredItem = null;
                }
                else if (mouseY > bottomY)
                {
                    hoveredIndex = listView.itemCount-1;
                    hoveredItem = null;
                }

                listView.hovered = new HoveredItem()
                {
                    Index = hoveredIndex,
                    Item = hoveredItem,
                    IsSelected = hoveredIsSelected
                };

                listView.AutoScrollMinSize = new Size(listView.AutoScrollMinSize.Width, bottomY- listView.AutoScrollPosition.Y);

                listView.itemCount = currentIndex;

                listView.LastSelectedIndex = lastSelectedIndex;
            }
        }

        protected abstract void DrawItems(DrawItemHandler handler);

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var min = Math.Min(selectRangeStart, selectRangeEnd);
            var max = Math.Max(selectRangeStart, selectRangeEnd);

            var handler = new DrawItemHandler(e.Graphics, min, max, this);

            DrawItems(handler);

            handler.Evaluate(this);
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);

            if (itemCount != 0)
                Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (itemCount == 0)
                return;

            if (mouseDown)
            {
                if (selecting && hovered.Index != -1)
                    selectRangeEnd = hovered.Index;

                Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            HandleClick(e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            HandleClick(e);
        }

        private void HandleClick(MouseEventArgs e)
        {
            succesiveClicks++;

            doubleClickTimer.Stop();
            doubleClickTimer.Start();

            Console.WriteLine("Clicks: " + succesiveClicks);

            if (hovered.Item != null)
                ItemClicked?.Invoke(this, new ItemClickedEventArgs(hovered.Item, e.Button, succesiveClicks, e.X, e.Y, e.Delta));
        }
    }
}
