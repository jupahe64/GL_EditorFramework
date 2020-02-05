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

namespace SpotLight
{
    [Designer("System.Windows.Forms.Design.ParentControlDesigner, System.Design", typeof(IDesigner))]
    public class DocumentTabControl : UserControl
    {
        List<DocumentTab> tabs = new List<DocumentTab>();

        int selectedIndex = 0;

        int hoveredIndex = -1;

        bool hoveringOverClose = false;

        Graphics g;

        public event EventHandler SelectedTabChanged;

        public event HandledEventHandler TabClosing;

        public class DocumentTab
        {
            public string Name;
            public object Tag;

            public DocumentTab(string name, object tag)
            {
                Name = name;
                Tag = tag;
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

            selectedIndex = index;

            SelectedTabChanged?.Invoke(this, new EventArgs());

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
            tabs.RemoveAt(hoveredIndex);

            if (selectedIndex > index || selectedIndex > tabs.Count - 1)
            {
                selectedIndex--;
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

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);

            g = e.Graphics;

            hoveredIndex = -1;

            hoveringOverClose = false;

            g.FillRectangle(SystemBrushes.ControlLightLight, 1, 30, Width - 2, Height - 31);
            g.DrawRectangle(SystemPens.ControlDark, 1, 30, Width - 2, Height - 31);

            int x = 5;

            for (int i = 0; i < tabs.Count; i++)
            {
                int width = (int)Math.Ceiling(g.MeasureString(tabs[i].Name, Font).Width);

                g.FillRectangle(SystemBrushes.ControlDark, x, 10, width + 20, 21);

                if (i == selectedIndex)
                    g.FillRectangle(SystemBrushes.ControlLightLight, x+1, 11, width + 18, 21);
                else
                    g.FillRectangle(SystemBrushes.Control, x + 1, 11, width + 18, 19);

                g.DrawString(tabs[i].Name, Font, SystemBrushes.ControlText, x + 5, 13);

                if (mousePos.Y > 10 && mousePos.Y < 30)
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
        }

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
                    HandledEventArgs args = new HandledEventArgs();

                    TabClosing?.Invoke(this, args);

                    if(!args.Handled)
                        RemoveTab(hoveredIndex);
                }
                else
                {
                    selectedIndex = hoveredIndex;

                    SelectedTabChanged?.Invoke(this, new EventArgs());
                }

                Invalidate();
            }
        }
    }
}
