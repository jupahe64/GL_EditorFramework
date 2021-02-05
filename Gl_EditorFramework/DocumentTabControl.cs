using GL_EditorFramework.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace GL_EditorFramework
{
    public class DocumentTabClosingEventArgs : CancelEventArgs
    {
        public DocumentTabControl.DocumentTab Tab { get; set; }

        public DocumentTabClosingEventArgs(DocumentTabControl.DocumentTab tab)
        {
            Tab = tab;
        }
    }

    public delegate void DocumentTabClosingEventHandler(object sender, DocumentTabClosingEventArgs e);


    [Designer("System.Windows.Forms.Design.ParentControlDesigner, System.Design", typeof(IDesigner))]
    public class DocumentTabControl : UserControl
    {
        List<DocumentTab> tabs = new List<DocumentTab>();

        int selectedIndex = -1;

        int hoveredIndex = -1;

        bool hoveringOverClose = false;

        Graphics g;

        public event EventHandler SelectedTabChanged;

        public event DocumentTabClosingEventHandler TabClosing;

        public class DocumentTab
        {
            public string Name;
            public object Document;

            public DocumentTab(string name, object tag)
            {
                Name = name;
                Document = tag;
            }
        }

        public DocumentTabControl()
        {
            SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);

            BorderStyle = BorderStyle.None;
        }

        public DocumentTab SelectedTab
        {
            get
            {
                if (selectedIndex != -1)
                    return tabs[selectedIndex];
                else
                    return null;
            }
        }

        public void Select(DocumentTab tab)
        {
            int index = tabs.IndexOf(tab);
            if (index != -1)
                Select(index);
        }

        public void Select(int index)
        {
            if (index < 0 || index > tabs.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be within the bounds of " + nameof(Tabs));

            if(selectedIndex!=index)
            {
                selectedIndex = index;
                SelectedTabChanged?.Invoke(this, new EventArgs());
            }

            Invalidate();
        }

        public void AddTab(DocumentTab tab, bool select)
        {
            tabs.Add(tab);

            if (select)
            {
                selectedIndex = tabs.Count-1;
                SelectedTabChanged?.Invoke(this, new EventArgs());
            }

            Invalidate();
        }

        public IReadOnlyList<DocumentTab> Tabs => tabs;

        public void InsertTab(int index, DocumentTab tab, bool select)
        {
            if (index < 0 || index > tabs.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be within the bounds of "+nameof(Tabs));

            tabs.Insert(index, tab);

            if (select)
            {
                selectedIndex = index;
                SelectedTabChanged?.Invoke(this, new EventArgs());
            }

            Invalidate();
        }

        public void RemoveTab(DocumentTab tab)
        {
            int index = tabs.IndexOf(tab);
            if (index != -1)
                RemoveTab(index);
        }

        public void RemoveTab(int index)
        {
            tabs.RemoveAt(index);

            if (selectedIndex > index || selectedIndex > tabs.Count - 1)
            {
                selectedIndex--;
                SelectedTabChanged?.Invoke(this, new EventArgs());
            }
            else if(index == selectedIndex)
            {
                SelectedTabChanged?.Invoke(this, new EventArgs());
            }

            Invalidate();
        }

        public void ClearTabs()
        {
            tabs.Clear();
            selectedIndex = -1;
            SelectedTabChanged?.Invoke(this, new EventArgs());

            Invalidate();
        }

        /// <summary>
        /// Trys clearing all tabs while checking if each one can be closed without interuption
        /// </summary>
        /// <returns>Weither there was an interuption</returns>
        public bool TryClearTabs()
        {
            while (tabs.Count!=0)
            {
                DocumentTabClosingEventArgs args = new DocumentTabClosingEventArgs(tabs[selectedIndex]);
                TabClosing?.Invoke(this, args);

                if (args.Cancel)
                    return false;

                RemoveTab(selectedIndex);
            }

            return true;
        }

        protected static Point[] arrowLeft = new Point[]
        {
            new Point(arrowWidth/2+4,  2),
            new Point(arrowWidth/2+4, 18),
            new Point(arrowWidth/2-4, 10)
        };

        protected static Point[] arrowRight = new Point[]
        {
            new Point(arrowWidth/2-4,  2),
            new Point(arrowWidth/2-4, 18),
            new Point(arrowWidth/2+4, 10)
        };

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);

            g = e.Graphics;

            hoveredIndex = -1;

            hoveringOverClose = false;

            g.FillRectangle(SystemBrushes.ControlLightLight, 1, 30, Width - 2, Height - 31);
            g.DrawRectangle(SystemPens.ControlDark, 1, 30, Width - 2, Height - 31);

            int x = arrowWidth + 5;

            Rectangle tabArea = new Rectangle(arrowWidth + 5, 10, Width - 10 - arrowWidth * 2, 21);

            g.SetClip(tabArea);

            for (int i = scrollIndexOffset; i < tabs.Count; i++)
            {
                int width = (int)Math.Ceiling(g.MeasureString(tabs[i].Name, Font).Width);

                g.FillRectangle(SystemBrushes.ControlDark, x, 10, width + 20, 21);

                if (i == selectedIndex)
                    g.FillRectangle(SystemBrushes.ControlLightLight, x+1, 11, width + 18, 21);
                else
                    g.FillRectangle(SystemBrushes.Control, x + 1, 11, width + 18, 19);

                g.DrawString(tabs[i].Name, Font, SystemBrushes.ControlText, x + 5, 13);

                if (tabArea.Contains(mousePos))
                {
                    if (mousePos.X > x && mousePos.X < x + width + 20)
                    {
                        hoveredIndex = i;

                        if (mousePos.X > x + width + 6)
                        {
                            hoveringOverClose = true;

                            g.DrawImage(Resources.CloseTabIconHover, x + width + 6, 16);
                            goto ICON_HOVERED;
                        }
                    }
                }

                g.DrawImage(Resources.CloseTabIcon, x + width + 6, 16);

                ICON_HOVERED:
                x += width + 22;
            }

            canScrollRight = x > Width - 5 - arrowWidth;

            g.ResetClip();

            hoveredArrow = HoveredArrow.NONE;

            if (scrollIndexOffset>0)
            {
                if (new Rectangle(5, 10, arrowWidth, 19).Contains(mousePos))
                    hoveredArrow = HoveredArrow.LEFT;

                g.TranslateTransform(5, 10);
                g.FillPolygon(hoveredArrow == HoveredArrow.LEFT ? SystemBrushes.ControlDark : Framework.backBrush, arrowLeft);
                g.ResetTransform();
            }

            if (canScrollRight)
            {
                g.FillRectangle(SystemBrushes.ControlDark, Width - arrowWidth - 5 - 2, 11, 2, 20);

                if (new Rectangle(Width - arrowWidth - 5, 10, arrowWidth, 19).Contains(mousePos))
                    hoveredArrow = HoveredArrow.RIGHT;

                g.TranslateTransform(Width - arrowWidth - 5, 10);
                g.FillPolygon(hoveredArrow == HoveredArrow.RIGHT ? SystemBrushes.ControlDark : Framework.backBrush, arrowRight);
                g.ResetTransform();
            }

        }

        enum HoveredArrow
        {
            NONE,
            LEFT,
            RIGHT,
        }

        HoveredArrow hoveredArrow = HoveredArrow.NONE;

        const int arrowWidth = 20;

        int scrollIndexOffset = 0;

        bool canScrollRight = false;

        Point mousePos = new Point(-1,-1);

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            mousePos = e.Location;

            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);

            if (hoveredIndex != -1)
            {
                if (hoveringOverClose)
                {
                    DocumentTabClosingEventArgs args = new DocumentTabClosingEventArgs(tabs[hoveredIndex]);

                    TabClosing?.Invoke(this, args);

                    if(!args.Cancel)
                        RemoveTab(hoveredIndex);
                }
                else
                {
                    Select(hoveredIndex);
                }

                Invalidate();
            }

            if(hoveredArrow==HoveredArrow.LEFT)
            {
                scrollIndexOffset--;
                Refresh();
            }
            else if (hoveredArrow == HoveredArrow.RIGHT)
            {
                scrollIndexOffset++;
                Refresh();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (e.Delta > 0)
            {
                if (scrollIndexOffset>0)
                {
                    scrollIndexOffset--;
                    Refresh();
                }
            }
            else if (e.Delta<0)
            {
                if (canScrollRight)
                {
                    scrollIndexOffset++;
                    Refresh();
                }
            }
        }
    }
}
