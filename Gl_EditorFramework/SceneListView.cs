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
        public Dictionary<string, IList> RootLists { get; set; } = new Dictionary<string, IList>();

        private Stack<IList> listStack = new Stack<IList>();

        public event SelectionChangedEventHandler SelectionChanged;
        public event ItemsMovedEventHandler ItemsMoved;
        public event EventHandler CurrentListChanged;
        public event ListEventHandler ListExited;

        int fontHeight;

        /// <summary>
        /// The set used to determine which objects are selected
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ISet<object> SelectedItems {
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
        public string CurrentRootListName { get; private set; } = "None";

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

            listStack.Push(listView.CurrentList);
            listView.CurrentList = list;

            rootListChangePanel.Visible = false;
            btnBack.Visible = true;
        }

        /// <summary>
        /// Tries to go back to the last list in the Stack
        /// </summary>
        public void ExitList()
        {
            if (listStack.Count==0)
                return;
            
            listView.CurrentList = listStack.Pop();

            rootListChangePanel.Visible = true;
            btnBack.Visible = false;
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

            listView.SelectionChanged += (x,y) => SelectionChanged?.Invoke(x,y);
            listView.ItemsMoved += (x, y) => ItemsMoved?.Invoke(x, y);

            Graphics g = CreateGraphics();

            fontHeight = (int)Math.Ceiling(Font.GetHeight(g.DpiY));

            rootListChangePanel.Height = rootListChangePanel.FontHeight + 4;
            btnBack.Height = rootListChangePanel.FontHeight + 6;

            listView.Top = rootListChangePanel.FontHeight + 7;
            listView.Height = Height - rootListChangePanel.FontHeight - 8;

            rootListChangePanel.Paint += RootListChangePanel_Paint;
            rootListChangePanel.Click += RootListChangePanel_Click;
        }

        private void RootListChangePanel_Click(object sender, EventArgs e)
        {
            if (rootListChangePanel.Expanded)
            {
                rootListChangePanel.Height = rootListChangePanel.FontHeight + 4;

                listView.Top = rootListChangePanel.FontHeight + 7;
                listView.Height =  Height - rootListChangePanel.FontHeight - 8;
                listView.Visible = true;
                rootListChangePanel.Expanded = false;
                CurrentRootListName = RootLists.Keys.ElementAt(rootListChangePanel.HoveredCategoryIndex);
                listView.CurrentList = RootLists[CurrentRootListName];
                CurrentListChanged?.Invoke(this, null);
                rootListChangePanel.Refresh();
            }
            else
            {
                rootListChangePanel.FullHeight = RootLists.Count * (rootListChangePanel.FontHeight+4);
                if (rootListChangePanel.FullHeight > Height / 2)
                {
                    rootListChangePanel.Height =  Height - 2;
                    listView.Visible = false;
                }
                else
                {
                    rootListChangePanel.Height =  rootListChangePanel.FullHeight;
                    listView.Top = rootListChangePanel.FullHeight + 7;
                    listView.Height =  Height - rootListChangePanel.FullHeight - 8;
                }
                rootListChangePanel.Expanded = true;
                rootListChangePanel.Refresh();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (rootListChangePanel.Expanded)
            {
                if (rootListChangePanel.FullHeight > Height / 2)
                {
                    rootListChangePanel.Height =  Height - 2;
                    listView.Visible = false;
                }
                else
                {
                    rootListChangePanel.Height =  rootListChangePanel.FullHeight;
                    listView.Top = rootListChangePanel.FullHeight + 7;
                    listView.Height =  Height - rootListChangePanel.FullHeight - 8;
                    listView.Visible = true;
                }
                rootListChangePanel.Refresh();
            }
        }

        private void RootListChangePanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (rootListChangePanel.Expanded)
            {
                int i = 0;
                int y;
                foreach (string rootList in RootLists.Keys)
                {
                    if ((y = 2 + i * (rootListChangePanel.FontHeight + 4) + rootListChangePanel.AutoScrollPosition.Y+rootListChangePanel.YOffset) > 2 - rootListChangePanel.FontHeight)
                    {
                        if (CurrentRootListName == rootList)
                        {
                            g.FillRectangle(SystemBrushes.Highlight, 0, y-2, rootListChangePanel.Width, rootListChangePanel.FontHeight+4);
                            g.DrawString(rootList, rootListChangePanel.Font, SystemBrushes.HighlightText, 4, y);
                        }
                        else if (rootListChangePanel.HoveredCategoryIndex == i)
                        {
                            g.FillRectangle(SystemBrushes.MenuHighlight, 0, y-2, rootListChangePanel.Width, rootListChangePanel.FontHeight+4);
                            g.DrawString(rootList, rootListChangePanel.Font, SystemBrushes.HighlightText, 4, y);
                        }
                        else
                            g.DrawString(rootList, rootListChangePanel.Font, new SolidBrush(ForeColor), 4, y);

                    }
                    i++;
                    if (y > rootListChangePanel.Height)
                        break;
                }
            } else
                g.DrawString(CurrentRootListName, rootListChangePanel.Font, new SolidBrush(ForeColor), 4, 2);
        }
        





        private class RootListChangePanel : UserControl
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

            public RootListChangePanel()
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

        private void BtnBack_Click(object sender, EventArgs e)
        {
            ExitList();
            ListEventArgs args = new ListEventArgs(listView.CurrentList);
            ListExited?.Invoke(this, args);
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
                list = value??emptyList;
                AutoScrollMinSize = new Size(0, FontHeight * list.Count);
                Refresh();
            }
        }

        /// <summary>
        /// Recalculate the height of the Autoscroll for this <see cref="FastListView"/>
        /// </summary>
        public void UpdateAutoscrollHeight()
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
            if (CurrentList == null)
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
