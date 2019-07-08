using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;

namespace GL_EditorFramework
{
    public partial class SceneListView : UserControl
    {
        public Dictionary<string, IList> lists = new Dictionary<string, IList>();

        public event SelectionChangedEventHandler SelectionChanged;
        public event ItemsMovedEventHandler ItemsMoved;

        private string currentCategory = "None";
        private FastListView objectPanel;
        private CategoryPanel categoryPanel;
        int fontHeight;

        public ISet<object> SelectedItems {
            get => objectPanel.SelectedItems;
            set
            {
                objectPanel.SelectedItems = value;
            }
        }

        public string CurrentCategory {
            get => currentCategory;
            set
            {
                if (lists.ContainsKey(value))
                {
                    currentCategory = value;
                    objectPanel.AutoScrollMinSize = new Size(0, lists[currentCategory].Count * (fontHeight+3));
                    objectPanel.List = lists[currentCategory];
                    Refresh();
                }
            }
        }

        public void UpdateAutoScroll()
        {
            objectPanel.UpdateAutoScroll();
        }

        public SceneListView()
        {
            InitializeComponent();

            objectPanel.SelectionChanged += (x,y) => SelectionChanged?.Invoke(x,y);
            objectPanel.ItemsMoved += (x, y) => ItemsMoved?.Invoke(x, y);

            Graphics g = CreateGraphics();

            fontHeight = (int)Math.Ceiling(Font.GetHeight(g.DpiY));

            categoryPanel.Size = new Size(298, categoryPanel.FontHeight + 4);

            objectPanel.Location = new Point(1, categoryPanel.FontHeight + 7);
            objectPanel.Size = new Size(298, 300 - categoryPanel.FontHeight - 8);

            categoryPanel.Paint += CategoryPanel_Paint;
            categoryPanel.Click += CategoryPanel_Click;
        }

        private void CategoryPanel_Click(object sender, EventArgs e)
        {
            if (categoryPanel.Expanded)
            {
                categoryPanel.Size = new Size(Width - 2, categoryPanel.FontHeight + 4);

                objectPanel.Location = new Point(1, categoryPanel.FontHeight + 7);
                objectPanel.Size = new Size(Width - 2, Height - categoryPanel.FontHeight - 8);
                objectPanel.Visible = true;
                categoryPanel.Expanded = false;
                currentCategory = lists.Keys.ElementAt(categoryPanel.HoveredCategoryIndex);
                objectPanel.List = lists[currentCategory];
                categoryPanel.Refresh();
            }
            else
            {
                categoryPanel.FullHeight = lists.Count * (categoryPanel.FontHeight+4);
                if (categoryPanel.FullHeight > Height / 2)
                {
                    categoryPanel.Size = new Size(Width - 2, Height - 2);
                    objectPanel.Visible = false;
                }
                else
                {
                    categoryPanel.Size = new Size(Width - 2, categoryPanel.FullHeight);
                    objectPanel.Location = new Point(1, categoryPanel.FullHeight + 7);
                    objectPanel.Size = new Size(Width-2, Height - categoryPanel.FullHeight - 8);
                }
                categoryPanel.Expanded = true;
                categoryPanel.Refresh();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (categoryPanel.Expanded)
            {
                if (categoryPanel.FullHeight > Height / 2)
                {
                    categoryPanel.Size = new Size(Width - 2, Height - 2);
                    objectPanel.Visible = false;
                }
                else
                {
                    categoryPanel.Size = new Size(Width - 2, categoryPanel.FullHeight);
                    objectPanel.Location = new Point(1, categoryPanel.FullHeight + 7);
                    objectPanel.Size = new Size(Width - 2, Height - categoryPanel.FullHeight - 8);
                    objectPanel.Visible = true;
                }
                categoryPanel.Refresh();
            }
        }

        private void CategoryPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (categoryPanel.Expanded)
            {
                int i = 0;
                int y;
                foreach (string category in lists.Keys)
                {
                    if ((y = 2 + i * (categoryPanel.FontHeight + 4) + categoryPanel.AutoScrollPosition.Y+categoryPanel.YOffset) > 2 - categoryPanel.FontHeight)
                    {
                        if (currentCategory == category)
                        {
                            g.FillRectangle(SystemBrushes.Highlight, 0, y-2, categoryPanel.Width, categoryPanel.FontHeight+4);
                            g.DrawString(category, categoryPanel.Font, SystemBrushes.HighlightText, 4, y);
                        }
                        else if (categoryPanel.HoveredCategoryIndex == i)
                        {
                            g.FillRectangle(SystemBrushes.MenuHighlight, 0, y-2, categoryPanel.Width, categoryPanel.FontHeight+4);
                            g.DrawString(category, categoryPanel.Font, SystemBrushes.HighlightText, 4, y);
                        }
                        else
                            g.DrawString(category, categoryPanel.Font, new SolidBrush(ForeColor), 4, y);

                    }
                    i++;
                    if (y > categoryPanel.Height)
                        break;
                }
            } else
                g.DrawString(currentCategory, categoryPanel.Font, new SolidBrush(ForeColor), 4, 2);
        }








        private void InitializeComponent()
        {
            this.objectPanel = new FastListView();
            this.categoryPanel = new CategoryPanel();
            this.SuspendLayout();
            // 
            // objectPanel
            // 
            this.objectPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.objectPanel.AutoScroll = true;
            this.objectPanel.AutoScrollMinSize = new System.Drawing.Size(0, 1000);
            this.objectPanel.BackColor = System.Drawing.SystemColors.Window;
            this.objectPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.objectPanel.Location = new System.Drawing.Point(1, 48);
            this.objectPanel.Margin = new System.Windows.Forms.Padding(1, 3, 1, 1);
            this.objectPanel.Name = "objectPanel";
            this.objectPanel.Size = new System.Drawing.Size(298, 251);
            this.objectPanel.TabIndex = 0;
            // 
            // categoryPanel
            // 
            this.categoryPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.categoryPanel.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.categoryPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.categoryPanel.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.categoryPanel.Location = new System.Drawing.Point(1, 1);
            this.categoryPanel.Margin = new System.Windows.Forms.Padding(1);
            this.categoryPanel.Name = "categoryPanel";
            this.categoryPanel.Size = new System.Drawing.Size(298, 43);
            this.categoryPanel.TabIndex = 1;
            // 
            // SceneListView
            // 
            this.Controls.Add(this.categoryPanel);
            this.Controls.Add(this.objectPanel);
            this.Name = "SceneListView";
            this.Size = new System.Drawing.Size(300, 300);
            this.ResumeLayout(false);

        }

        private class CategoryPanel : UserControl
        {
            public int HoveredCategoryIndex = -1;

            new public int FontHeight;

            public int FullHeight;
            public int YOffset = 0;

            public bool Expanded = false;

            public Dictionary<string, IList> lists = new Dictionary<string, IList>();

            protected override void OnFontChanged(EventArgs e)
            {
                base.OnFontChanged(e);
                Graphics g = CreateGraphics();
                FontHeight = (int)Math.Ceiling(Font.GetHeight(g.DpiY));
            }

            public CategoryPanel()
            {
                SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
                Graphics g = CreateGraphics();
                FontHeight = (int)Math.Ceiling(Font.GetHeight(g.DpiY));
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                HoveredCategoryIndex = -1;
                Refresh();
            }
            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                HoveredCategoryIndex = (e.Y - YOffset) / (FontHeight + 4);
                Refresh();
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                base.OnMouseWheel(e);
                YOffset = Math.Min(0,Math.Max(YOffset + e.Delta / 4, Height - FullHeight));

                

                Refresh();
            }
        }
    }

    public class SelectionChangedEventArgs : HandledEventArgs
    {
        public List<object> ItemsToSelect { get; private set; }
        public List<object> ItemsToDeselect { get; private set; }
        public SelectionChangedEventArgs(List<object> itemsToSelect, List<object> itemsToDeselect)
        {
            ItemsToSelect = itemsToSelect;
            ItemsToDeselect = itemsToDeselect;
        }
    }

    public class ItemsMovedEventArgs : HandledEventArgs
    {
        public int OriginalIndex { get; private set; }
        public int Count { get; private set; }
        public int Offset { get; private set; }
        public ItemsMovedEventArgs(int originalIndex, int count, int offset)
        {
            OriginalIndex = originalIndex;
            Count = count;
            Offset = offset;
        }
    }

    public delegate void SelectionChangedEventHandler(object sender, SelectionChangedEventArgs e);

    public delegate void ItemsMovedEventHandler(object sender, ItemsMovedEventArgs e);

    public class FastListView : UserControl
    {
        private IList list;
        private ISet<object> selectedItems;

        private int selectStartIndex = -1;
        private int prevSelectStartIndex = -1;
        private int selectEndIndex = -1;
        private bool keepTheRest = true;
        private bool subtract = false;

        private int draggedStartIndex = -1;
        private int draggedCount = 0;
        private int draggedStartMouseOffset = 0;
        private int dragOffset = 0;
        private bool useDragRepresention = false;
        private int dragGapSize = 0;

        private enum Action
        {
            NONE,
            SELECT,
            DRAG
        }

        Action action = Action.NONE;

        public event SelectionChangedEventHandler SelectionChanged;

        public event ItemsMovedEventHandler ItemsMoved;

        private Timer marginScrollTimer = new Timer();

        private int marginScrollSpeed = 0;

        public ISet<object> SelectedItems {
            get => selectedItems;
            set {
                selectedItems = value;
                Refresh();
            }
        }

        public IList List {
            get => list;
            set
            {
                list = value;
                AutoScrollMinSize = new Size(0, FontHeight * list.Count);
                Refresh();
            }
        }

        public void UpdateAutoScroll()
        {
            AutoScrollMinSize = new Size(0, FontHeight * list.Count);
        }

        public FastListView()
        {
            SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);
            Graphics g = CreateGraphics();
            FontHeight = (int)Math.Ceiling(Font.GetHeight(g.DpiY));

            marginScrollTimer.Interval = 1;
            marginScrollTimer.Tick += Timer_Tick;
        }

        int mouseY = 0;

        bool mouseDown = false;

        private void Timer_Tick(object sender, EventArgs e)
        {
            VerticalScroll.Value = Math.Max(VerticalScroll.Minimum, Math.Min(VerticalScroll.Value+marginScrollSpeed, VerticalScroll.Maximum));

            if(action == Action.SELECT)
                selectEndIndex = Math.Max(0, Math.Min((mouseY - AutoScrollPosition.Y) / (FontHeight), list.Count - 1));
            else if(action == Action.DRAG)
                dragOffset = Math.Max(
                    -draggedStartIndex, 
                    Math.Min((mouseY - AutoScrollPosition.Y - draggedStartMouseOffset) / FontHeight - draggedStartIndex, 
                    list.Count - draggedCount - draggedStartIndex));

            Refresh();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
                return;

            mouseDown = true;

            int index = Math.Max(0, Math.Min((mouseY - AutoScrollPosition.Y) / (FontHeight), list.Count - 1));

            if (ModifierKeys.HasFlag(Keys.Alt))
            {
                if (!selectedItems.Contains(list[index]))
                    return;

                int start = index;
                while(start>=0 && selectedItems.Contains(list[start]))
                    start--;

                int end = index;
                while (end < list.Count && selectedItems.Contains(list[end]))
                    end++;

                start++;

                draggedStartIndex = start;
                dragGapSize = draggedCount = end - start;
                draggedStartMouseOffset = mouseY - (start * (FontHeight) + AutoScrollPosition.Y);

                if (useDragRepresention = draggedCount > Width / 2 / FontHeight)
                {
                    dragGapSize = 1;
                    draggedStartMouseOffset = 0;
                    AutoScrollMinSize = new Size(0, FontHeight * (list.Count-draggedCount+1));
                }

                action = Action.DRAG;

                return;
            }

            if (ModifierKeys.HasFlag(Keys.Shift) && prevSelectStartIndex != -1)
            {
                selectStartIndex = prevSelectStartIndex;
                selectEndIndex = index;
            }
            else
            {
                selectStartIndex = index;
                selectEndIndex = selectStartIndex;
            }

            keepTheRest = ModifierKeys.HasFlag(Keys.Control);
            subtract = selectedItems.Contains(list[index]) && keepTheRest;
            action = Action.SELECT;
            Refresh();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            mouseY = e.Y;
            if (e.Button != MouseButtons.Left)
                return;

            if (action == Action.DRAG)
                dragOffset = Math.Max(
                    -draggedStartIndex,
                    Math.Min((e.Y - AutoScrollPosition.Y - draggedStartMouseOffset) / FontHeight - draggedStartIndex,
                    list.Count-draggedCount-draggedStartIndex));

            else if(action == Action.SELECT)
                selectEndIndex = Math.Max(0, Math.Min((mouseY - AutoScrollPosition.Y) / (FontHeight), list.Count - 1));

            if (e.Y < 0)
            {
                marginScrollSpeed = e.Y;
                marginScrollTimer.Start();
            }
            else if (e.Y > Height)
            {
                marginScrollSpeed = e.Y - Height;
                marginScrollTimer.Start();
            }
            else
                marginScrollTimer.Stop();

            Refresh();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left)
                return;

            mouseDown = false;
            marginScrollTimer.Stop();

            if (action == Action.DRAG)
            {
                if(useDragRepresention)
                    AutoScrollMinSize = new Size(0, FontHeight * list.Count);
                
                ItemsMovedEventArgs eventArgs = new ItemsMovedEventArgs(draggedStartIndex, draggedCount, dragOffset);

                ItemsMoved?.Invoke(this, eventArgs);

                if (!eventArgs.Handled)
                {
                    List<object> objs = new List<object>();

                    for (int i = 0; i < draggedCount; i++)
                    {
                        objs.Add(list[draggedStartIndex]);
                        list.RemoveAt(draggedStartIndex);
                    }

                    int index = draggedStartIndex + dragOffset;
                    foreach (object obj in objs)
                    {
                        list.Insert(index, obj);
                        index++;
                    }
                }

                draggedStartIndex = -1;
                dragOffset = 0;
            }
            else if (action == Action.SELECT)
            {
                int min = Math.Min(selectStartIndex, selectEndIndex);
                int max = Math.Max(selectStartIndex, selectEndIndex);

                List<object> itemsToSelect = new List<object>();
                List<object> itemsToDeselect = new List<object>();

                for (int i = min; i <= max; i++)
                    if (!selectedItems.Contains(list[i]))
                        itemsToSelect.Add(list[i]);

                if (!keepTheRest)
                {
                    foreach (object obj in selectedItems)
                    {
                        int index = list.IndexOf(obj);
                        if (index < min || max < index)
                            itemsToDeselect.Add(obj);
                    }
                }

                if (subtract)
                {
                    for (int i = min; i <= max; i++)
                        if (selectedItems.Contains(list[i]))
                            itemsToDeselect.Add(list[i]);
                }

                SelectionChangedEventArgs eventArgs = new SelectionChangedEventArgs(itemsToSelect, itemsToDeselect);

                SelectionChanged?.Invoke(this, eventArgs);

                if (!eventArgs.Handled)
                {
                    foreach (object obj in itemsToSelect)
                        selectedItems.Add(obj);

                    foreach (object obj in itemsToDeselect)
                        selectedItems.Remove(obj);
                }

                prevSelectStartIndex = selectStartIndex;
                selectStartIndex = -1;
                selectEndIndex = -1;
                keepTheRest = true;
            }

            action = Action.NONE;
            Refresh();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (List == null)
                return;

            Graphics g = e.Graphics;

            int i = 0;
            int j = 0;
            int y;
            int min = Math.Min(selectStartIndex, selectEndIndex);
            int max = Math.Max(selectStartIndex, selectEndIndex);

            while(j<list.Count)
            {
                if (i == draggedStartIndex + dragOffset)
                    i += dragGapSize; //skip

                if (j == draggedStartIndex)
                {
                    j += draggedCount; //skip
                    if (j >= list.Count)
                        break;
                }

                
                if ((y = i * (FontHeight) + AutoScrollPosition.Y) > -FontHeight)
                {
                    bool hightlighted;
                    if (subtract)
                        hightlighted = SelectedItems.Contains(list[j]) && !(min <= i && i <= max);
                    else
                        hightlighted = (min <= i && i <= max) || (keepTheRest && SelectedItems.Contains(list[j]));

                    if (hightlighted)
                    {
                        g.FillRectangle(SystemBrushes.Highlight, 0, y, Width, FontHeight);
                        g.DrawString(list[j].ToString(), Font, SystemBrushes.HighlightText, 2, y);
                    }
                    else
                        g.DrawString(list[j].ToString(), Font, new SolidBrush(ForeColor), 2, y);

                }
                i++;
                j++;
                if (y > Height)
                    break;
            }
            if (action == Action.DRAG)
            {
                if (useDragRepresention)
                {
                    i = draggedStartIndex + dragOffset;
                    y = i * (FontHeight) + AutoScrollPosition.Y;
                    g.DrawString(draggedCount.ToString(), Font, new SolidBrush(ForeColor), 2, y);
                    i++;
                }
                else
                {
                    i = draggedStartIndex + dragOffset;
                    for (j = draggedStartIndex; j < draggedStartIndex + draggedCount; j++)
                    {
                        if ((y = i * (FontHeight) + AutoScrollPosition.Y) > -FontHeight)
                        {
                            bool hightlighted;
                            if (subtract)
                                hightlighted = SelectedItems.Contains(list[j]) && !(min <= i && i <= max);
                            else
                                hightlighted = (min <= i && i <= max) || (keepTheRest && SelectedItems.Contains(list[j]));

                            if (hightlighted)
                            {
                                g.FillRectangle(SystemBrushes.Highlight, 0, y, Width, FontHeight);
                                g.DrawString(list[j].ToString(), Font, SystemBrushes.HighlightText, 2, y);
                            }
                            else
                                g.DrawString(list[j].ToString(), Font, new SolidBrush(ForeColor), 2, y);

                        }
                        i++;
                    }
                }

                g.FillRectangle(SystemBrushes.MenuHighlight, 0, (draggedStartIndex + dragOffset) * (FontHeight) + AutoScrollPosition.Y, Width, 2);
                g.FillRectangle(SystemBrushes.MenuHighlight, 0, i * (FontHeight) + AutoScrollPosition.Y-2, Width, 2);
            }
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            Refresh();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (mouseDown)
            {
                if (action == Action.DRAG)
                    dragOffset = Math.Max(
                    -draggedStartIndex,
                    Math.Min((mouseY - AutoScrollPosition.Y - draggedStartMouseOffset) / FontHeight - draggedStartIndex,
                    list.Count - draggedCount - draggedStartIndex));

                else if(action == Action.SELECT)
                    selectEndIndex = Math.Max(0, Math.Min((mouseY - AutoScrollPosition.Y) / (FontHeight), list.Count - 1));

                Refresh();
            }
        }
    }
}
