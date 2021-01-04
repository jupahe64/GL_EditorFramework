using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections;

namespace GL_EditorFramework
{
    public class SelectionChangedEventArgs : HandledEventArgs
    {
        public IEnumerable<object> Items { get; private set; }
        public SelectionChangeMode SelectionChangeMode { get; private set; }
        public SelectionChangedEventArgs(IEnumerable<object> items, SelectionChangeMode selectionChangeMode)
        {
            Items = items;
            SelectionChangeMode = selectionChangeMode;
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

    public class FastListView : FastListViewBase
    {
        public event SelectionChangedEventHandler SelectionChanged;
        public event ItemsMovedEventHandler ItemsMoved;

        private IList list;
        private ISet<object> selectedItems;

        /// <summary>
        /// The set used to determine which objects are selected
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ISet<object> SelectedItems
        {
            get => selectedItems;
            set
            {
                selectedItems = value;
                Refresh();
            }
        }

        private static readonly List<object> emptyList = new List<object>();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList CurrentList
        {
            get => list;
            set
            {
                list = value;
                Refresh();
            }
        }

        public object GetItemAt(Point point)
        {
            if (CurrentList == null || CurrentList.Count == 0)
                return null;

            return CurrentList[Math.Max(0, Math.Min((point.Y - AutoScrollPosition.Y) / (FontHeight), list.Count - 1))];
        }

        [Obsolete("The drawing code will update it automatically")]
        public void UpdateAutoscrollHeight()
        {
            Refresh();
        }

        private void UpdateSelection(IEnumerable<object> items, SelectionChangeMode selectionChangeMode)
        {
            SelectionChangedEventArgs eventArgs = new SelectionChangedEventArgs(items, selectionChangeMode);

            SelectionChanged?.Invoke(this, eventArgs);

            if (!eventArgs.Handled)
            {
                if (selectionChangeMode == SelectionChangeMode.SET)
                {
                    selectedItems.Clear();

                    foreach (object obj in items)
                        selectedItems.Add(obj);
                }
                else if (selectionChangeMode == SelectionChangeMode.ADD)
                {
                    foreach (object obj in items)
                        selectedItems.Add(obj);
                }
                else //SelectionChangeMode.SUBTRACT
                {
                    foreach (object obj in items)
                        selectedItems.Remove(obj);
                }
            }
        }

        public void EnsureVisisble(object item)
        {
            int index = list?.IndexOf(item) ?? -1;
            if (index == -1)
                return;

            EnsureVisisble(index);
        }

        public void EnsureVisisble(int index)
        {
            if (index < 0 || index >= list.Count)
                return;

            int y = index * FontHeight + AutoScrollPosition.Y;

            if (y < 0)
                AutoScrollPosition = new Point(0, index * FontHeight);
            else if (y > Height - FontHeight)
                AutoScrollPosition = new Point(0, index * FontHeight - Height + FontHeight);
        }

        protected override object Select(int rangeMin, int rangeMax, SelectionChangeMode selectionChangeMode)
        {
            List<object> items = new List<object>();

            for (int i = rangeMin; i <= rangeMax; i++)
                items.Add(list[i]);

            UpdateSelection(items, selectionChangeMode);

            return items.Last();
        }

        protected override object SelectNext(string searchString, int startIndex)
        {
            int searchIndex = startIndex;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[searchIndex].ToString().StartsWith(searchString, StringComparison.OrdinalIgnoreCase))
                {
                    return Select(searchIndex, SelectionChangeMode.SET);
                }
                searchIndex = (searchIndex + 1) % list.Count; //searchIndex++ but wrap around
            }

            return null; //selection didn't change
        }

        protected static (Brush font, Brush back)[] highlightBrushes = new (Brush font, Brush back)[]
        {
            (SystemBrushes.ControlText, SystemBrushes.ControlLightLight), //NONE
            (SystemBrushes.HighlightText, SystemBrushes.Highlight), //SELECTED
            (SystemBrushes.ControlText, SystemBrushes.ControlLightLight), //HOVERED
            (SystemBrushes.HighlightText, SystemBrushes.Highlight), //HOVERED_SELECTED
        };

        protected override void DrawItems(DrawItemHandler handler)
        {
            Graphics g = handler.graphics;

            if (list == null)
            {
                g.FillRectangle(SystemBrushes.ControlLight, 0, 0, Width, Height);
                return;
            }


            int y;

            for (int i = 0; i < list.Count; i++)
            {
                y = i * (FontHeight) + AutoScrollPosition.Y;

                var (font, back) = highlightBrushes[(int)handler.HandleItem(list[i], selectedItems.Contains(list[i]), y, FontHeight)];
                
                if (y > -FontHeight && y <= Height)
                {

                    g.FillRectangle(back, 0, y, Width, FontHeight);
                    g.DrawString(list[i].ToString(), Font, font, 2, y);

                }
            }
        }
    }
}