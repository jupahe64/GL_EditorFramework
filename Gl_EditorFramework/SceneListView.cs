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
    public struct ItemDoubleClickedEventArgs
    {
        public ItemDoubleClickedEventArgs(object item) : this()
        {
            Item = item;
        }

        public object Item { get; }
    }

    public delegate void ItemDoubleClickedEventHandler(object sender, ItemDoubleClickedEventArgs e);

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
        public event ItemDoubleClickedEventHandler ItemDoubleClicked;

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

        public void UnselectCurrentList()
        {
            listView.CurrentList = null;
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
        
        private void listView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            object item = listView.GetItemAt(e.Location);
            if (item != null)
                ItemDoubleClicked?.Invoke(this, new ItemDoubleClickedEventArgs(item));
        }
    }
}
