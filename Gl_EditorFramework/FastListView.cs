using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections;

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

        private object lastSelectedObject = null;


        private enum Action
        {
            NONE,
            SELECT,
            DRAG
        }

        Action action = Action.NONE;

        public event SelectionChangedEventHandler SelectionChanged;
        public event ItemsMovedEventHandler ItemsMoved;
        public event ItemClickedEventHandler ItemClicked;

        private Timer marginScrollTimer = new Timer();

        private int marginScrollSpeed = 0;

        Timer doubleClickTimer = new Timer();

        int succesiveClicks = 0;

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
            marginScrollTimer.Tick += MarginScrollTimer_Tick;

            doubleClickTimer.Interval = SystemInformation.DoubleClickTime;
            doubleClickTimer.Tick += DoubleClickTimer_Tick;
        }

        /// <summary>
        /// The set used to determine which objects are selected
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ISet<object> SelectedItems {
            get => selectedItems;
            set {
                selectedItems = value;
                Refresh();
            }
        }

        private static readonly List<object> emptyList = new List<object>();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList CurrentList {
            get => list;
            set
            {
                list = value;
                UpdateAutoscrollHeight();
                Refresh();
            }
        }

        public object GetItemAt(Point point)
        {
            if (CurrentList == null || CurrentList.Count == 0)
                return null;

            return CurrentList[Math.Max(0, Math.Min((point.Y - AutoScrollPosition.Y) / (FontHeight), list.Count - 1))];
        }

        /// <summary>
        /// Recalculate the height of the Autoscroll for this <see cref="FastListView"/>
        /// </summary>
        public void UpdateAutoscrollHeight()
        {
            if (!(list?.Count>0))
                return;

            AutoScrollMinSize = new Size(0, FontHeight * list?.Count ?? 0);
        }

        int mouseY = 0;

        bool mouseDown = false;

        private void MarginScrollTimer_Tick(object sender, EventArgs e)
        {
            if (!(list?.Count>0))
                return;

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

            if (!(list?.Count > 0))
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

            if (!(list?.Count>0))
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

            if (!(list?.Count>0))
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

                List<object> items = new List<object>();

                for (int i = min; i <= max; i++)
                    items.Add(list[i]);

                if (subtract)
                    UpdateSelection(items, SelectionChangeMode.SUBTRACT);
                else if(keepTheRest)
                    UpdateSelection(items, SelectionChangeMode.ADD);
                else
                    UpdateSelection(items, SelectionChangeMode.SET);

                lastSelectedObject = list[selectEndIndex];

                prevSelectStartIndex = selectStartIndex;
                selectStartIndex = -1;
                selectEndIndex = -1;
                keepTheRest = true;
            }

            action = Action.NONE;
            Refresh();
        }

        private void UpdateSelection(IEnumerable<object> items, SelectionChangeMode selectionChangeMode)
        {
            SelectionChangedEventArgs eventArgs = new SelectionChangedEventArgs(items, selectionChangeMode);

            SelectionChanged?.Invoke(this, eventArgs);

            if (!eventArgs.Handled)
            {
                if (selectionChangeMode==SelectionChangeMode.SET)
                {
                    selectedItems.Clear();

                    foreach (object obj in items)
                        selectedItems.Add(obj);
                }
                else if(selectionChangeMode == SelectionChangeMode.ADD)
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

        public static IEnumerable<object> ItemToEnumerable(object item)
        {
            yield return item;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                    return true;
                case Keys.Shift | Keys.Right:
                case Keys.Shift | Keys.Left:
                case Keys.Shift | Keys.Up:
                case Keys.Shift | Keys.Down:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (!(list?.Count > 0))
                return;

            int index = list.IndexOf(lastSelectedObject);

            if (index == -1)
                return;

            if (e.KeyCode == Keys.Up && index > 0)
            {
                lastSelectedObject = list[index - 1];
                UpdateSelection(ItemToEnumerable(lastSelectedObject), e.Shift?SelectionChangeMode.ADD: SelectionChangeMode.SET);
                EnsureVisisble(index - 1);
            }
            else if (e.KeyCode==Keys.Down && index < list.Count - 1)
            {
                lastSelectedObject = list[index + 1];
                UpdateSelection(ItemToEnumerable(lastSelectedObject), e.Shift ? SelectionChangeMode.ADD : SelectionChangeMode.SET);
                EnsureVisisble(index + 1);
            }

            Focus();
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            if (!(list?.Count > 0))
                return;

            int index = (list.IndexOf(lastSelectedObject)+1)%list.Count;
            
            for (int i = 0; i < list.Count; i++)
            {
                if (list[index].ToString().ToLower().StartsWith(e.KeyChar.ToString().ToLower()))
                {
                    lastSelectedObject = list[index];
                    UpdateSelection(ItemToEnumerable(lastSelectedObject), SelectionChangeMode.SET);
                    EnsureVisisble(lastSelectedObject);
                    return;
                }
                index = (index + 1) % list.Count;
            }

        }

        public void EnsureVisisble(object item)
        {
            int index = list?.IndexOf(item)??-1;
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Brush textBrush = new SolidBrush(ForeColor);

            Graphics g = e.Graphics;

            if (list == null)
            {
                g.FillRectangle(SystemBrushes.ControlLight, 0, 0, Width, Height);

                return;
            }

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
                        g.DrawString(list[j].ToString(), Font, textBrush, 2, y);

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
                    g.DrawString(draggedCount.ToString(), Font, textBrush, 2, y);
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
                                g.DrawString(list[j].ToString(), Font, textBrush, 2, y);

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

            if(list!=null)
                Refresh();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (!(list?.Count>0))
                return;

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

            object item = GetItemAt(e.Location);
            if (item != null)
                ItemClicked?.Invoke(this, new ItemClickedEventArgs(item, e.Button, succesiveClicks, e.X, e.Y, e.Delta));
        }
    }
}
