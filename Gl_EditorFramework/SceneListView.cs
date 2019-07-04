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

        private string currentCategory = "None";
        private FastListView objectPanel;
        private CategoryPanel categoryPanel;
        int fontHeight;

        public IList SelectedItems {
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

        public SceneListView()
        {
            InitializeComponent();

            objectPanel.SelectionChanged += (x,y) => SelectionChanged?.Invoke(x,y);

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

    public delegate void SelectionChangedEventHandler(object sender, SelectionChangedEventArgs e);

    public class FastListView : UserControl
    {
        private IList list;
        private IList selectedItems;

        private int selectStartIndex = -1;
        private int selectEndIndex = -1;
        private bool addToSelection = true;

        public event SelectionChangedEventHandler SelectionChanged;

        public IList SelectedItems {
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

        public FastListView()
        {
            SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);
            Graphics g = CreateGraphics();
            FontHeight = (int)Math.Ceiling(Font.GetHeight(g.DpiY));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            selectStartIndex = (e.Y - AutoScrollPosition.Y) / (FontHeight);
            selectEndIndex = selectStartIndex;
            addToSelection = false;
            Refresh();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button == MouseButtons.Left)
            {
                selectEndIndex = (e.Y - AutoScrollPosition.Y) / (FontHeight);
                Refresh();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            int min = Math.Min(selectStartIndex, selectEndIndex);
            int max = Math.Max(selectStartIndex, selectEndIndex);

            List<object> itemsToSelect = new List<object>();
            List<object> itemsToDeselect = new List<object>();
            
            for (int i = min; i <= max; i++)
                if(!selectedItems.Contains(list[i]))
                    itemsToSelect.Add(list[i]);

            if (!addToSelection)
            {
                foreach(object obj in selectedItems)
                {
                    int index = list.IndexOf(obj);
                    if (index<min||max<index)
                        itemsToDeselect.Add(obj);
                }
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

            selectStartIndex = -1;
            selectEndIndex = -1;
            addToSelection = true;
            Refresh();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (List == null)
                return;

            Graphics g = e.Graphics;

            int i = 0;
            int y;
            int min = Math.Min(selectStartIndex, selectEndIndex);
            int max = Math.Max(selectStartIndex, selectEndIndex);

            foreach (object obj in List)
            {
                if ((y = i * (FontHeight) + AutoScrollPosition.Y) > 2 - FontHeight)
                {
                    if ((min <= i && i <= max) || (addToSelection&&SelectedItems.Contains(obj)))
                    {
                        g.FillRectangle(SystemBrushes.Highlight, 0, y, Width, FontHeight);
                        g.DrawString(obj.ToString(), Font, SystemBrushes.HighlightText, 2, y);
                    }
                    else
                        g.DrawString(obj.ToString(), Font, new SolidBrush(ForeColor), 2, y);

                }
                i++;
                if (y > Height)
                    break;
            }
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            Refresh();
        }
    }
}
