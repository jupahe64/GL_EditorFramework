using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections;
using static GL_EditorFramework.Framework;

namespace GL_EditorFramework
{
    /// <summary>
    /// A control for viewing the content of and selecting items in multiple <see cref="IList"/>s
    /// </summary>
    public partial class SceneListView : UserControl
    {
        /// <summary>
        /// A dictionary containing all RootLists stored by their name
        /// </summary>
        public Dictionary<string, IList> RootLists
        {
            get => rootLists;
            set
            {
                rootLists = value;
                UpdateComboBoxItems();
            }
        }

        Dictionary<string, IList> rootLists = new Dictionary<string, IList>();

        public void UpdateComboBoxItems()
        {
            rootListComboBox.Items.Clear();
            rootListComboBox.Items.AddRange(rootLists.Keys.ToArray());
        }

        private Stack<IList> listStack = new Stack<IList>();

        public event SelectionChangedEventHandler SelectionChanged;
        public event ItemsMovedEventHandler ItemsMoved;
        public event ListEventHandler ListExited;

        int fontHeight;
        private string currentRootListName = "None";

        /// <summary>
        /// The set used to determine which objects are selected
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ISet<object> SelectedItems
        {
            get => listView.SelectedItems;
            set
            {
                listView.SelectedItems = value;
            }
        }

        /// <summary>
        /// The name of the current list on the root level
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentRootListName
        {
            get => currentRootListName;
            private set
            {
                currentRootListName = value;
                rootListComboBox.SelectedItem = value;
            }
        }

        /// <summary>
        /// Sets the current list to a list in <see cref="RootLists"/>
        /// </summary>
        /// <param name="listName">The name under which the name is stored in <see cref="RootLists"/></param>
        public void SetRootList(string listName)
        {
            if (RootLists.ContainsKey(listName))
            {
                CurrentRootListName = listName;
                listStack.Clear();
                listView.CurrentList = RootLists[listName];
            }
        }

        /// <summary>
        /// The current list in the list view
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList CurrentList
        {
            get => listView.CurrentList;
        }

        /// <summary>
        /// Views a new list and adds the current one to the Stack
        /// </summary>
        /// <param name="list">the list to be entered</param>
        public void EnterList(IList list)
        {
            if (listStack == null)
                return;

            if (listView.CurrentList != null)
                listStack.Push(listView.CurrentList);
            listView.CurrentList = list;

            rootListComboBox.Visible = false;
            btnBack.Visible = true;
        }

        /// <summary>
        /// Tries to go back to the last list in the Stack
        /// </summary>
        public void ExitList()
        {
            if (listStack.Count == 0)
                return;

            listView.CurrentList = listStack.Pop();

            rootListComboBox.Visible = true;
            btnBack.Visible = false;
        }

        public void InvalidateCurrentList()
        {
            listView.CurrentList = null;
        }

        /// <summary>
        /// Recalculate the height of the Autoscroll for the <see cref="FastListView"/>
        /// </summary>
        public void UpdateAutoScrollHeight()
        {
            listView.UpdateAutoscrollHeight();
        }

        public SceneListView()
        {
            InitializeComponent();

            listView.SelectionChanged += (x, y) => SelectionChanged?.Invoke(x, y);
            listView.ItemsMoved += (x, y) => ItemsMoved?.Invoke(x, y);

            Graphics g = CreateGraphics();

            fontHeight = (int)Math.Ceiling(Font.GetHeight(g.DpiY));
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            ExitList();
            ListEventArgs args = new ListEventArgs(listView.CurrentList);
            ListExited?.Invoke(this, args);
        }

        private void RootListComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            listView.CurrentList = rootLists[(string)rootListComboBox.SelectedItem];
        }

        private void rootListComboBox_DropDown(object sender, EventArgs e)
        {

        }
    }






    public class SelectionChangedEventArgs : HandledEventArgs
    {
        public IEnumerable<object> ItemsToSelect { get; private set; }
        public IEnumerable<object> ItemsToDeselect { get; private set; }
        public SelectionChangedEventArgs(IEnumerable<object> itemsToSelect, IEnumerable<object> itemsToDeselect)
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

        private Timer marginScrollTimer = new Timer();

        private int marginScrollSpeed = 0;

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
                AutoScrollMinSize = new Size(0, FontHeight * list?.Count??0);
                Refresh();
            }
        }

        /// <summary>
        /// Recalculate the height of the Autoscroll for this <see cref="FastListView"/>
        /// </summary>
        public void UpdateAutoscrollHeight()
        {
            if (list == null)
                return;

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
            if (list == null)
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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (list == null)
                return;

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

            if (list == null)
                return;

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

            if (list == null)
                return;

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

                UpdateSelection(itemsToSelect, itemsToDeselect);

                lastSelectedObject = list[selectEndIndex];

                prevSelectStartIndex = selectStartIndex;
                selectStartIndex = -1;
                selectEndIndex = -1;
                keepTheRest = true;
            }

            action = Action.NONE;
            Refresh();
        }

        private void UpdateSelection(IEnumerable<object> itemsToSelect, IEnumerable<object> itemsToDeselect)
        {
            SelectionChangedEventArgs eventArgs = new SelectionChangedEventArgs(itemsToSelect, itemsToDeselect);

            SelectionChanged?.Invoke(this, eventArgs);

            if (!eventArgs.Handled)
            {
                foreach (object obj in itemsToSelect)
                    selectedItems.Add(obj);

                foreach (object obj in itemsToDeselect)
                    selectedItems.Remove(obj);
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
            int index = list.IndexOf(lastSelectedObject);

            if (index == -1)
                return;

            if (e.KeyCode == Keys.Up && index > 0)
            {
                object previous = list[index - 1];
                if (selectedItems.Contains(previous))
                    UpdateSelection(Enumerable.Empty<object>(), selectedItems.Where(x => x != previous).ToArray());
                else
                    UpdateSelection(ItemToEnumerable(previous), selectedItems.ToArray());

                lastSelectedObject = previous;
                EnsureVisisble(previous);
            }
            else if (e.KeyCode==Keys.Down && index < list.Count - 1)
            {
                object next = list[index + 1];
                if (selectedItems.Contains(next))
                    UpdateSelection(Enumerable.Empty<object>(),selectedItems.Where(x=>x!= next).ToArray());
                else
                    UpdateSelection(ItemToEnumerable(next), selectedItems.ToArray());

                lastSelectedObject = next;
                EnsureVisisble(next);
            }

            Focus();
        }

        public void EnsureVisisble(object item)
        {
            int index = list.IndexOf(item);
            if (index == -1)
                return;

            int y = index * FontHeight + AutoScrollPosition.Y;

            if (y < 0)
                AutoScrollPosition = new Point(0, index * FontHeight);
            else if(y > Height - FontHeight)
                AutoScrollPosition = new Point(0, index * FontHeight - Height + FontHeight );
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Brush textBrush = new SolidBrush(ForeColor);

            Graphics g = e.Graphics;

            if (CurrentList == null)
            {
                g.FillRectangle(SystemBrushes.ControlLight, 0, 0, Width, Height);
                g.DrawString("No List Selected", Font, textBrush, 5, 5);

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

            if (list == null)
                return;

            Refresh();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (list == null)
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
    }
}
