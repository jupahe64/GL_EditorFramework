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
            var list = RootListComboBox.SelectedItem;
            RootListComboBox.Items.Clear();
            RootListComboBox.Items.AddRange(rootLists.Keys.ToArray());

            RootListComboBox.SelectedItem = list;
        }

        private Stack<IList> listStack = new Stack<IList>();

        public event SelectionChangedEventHandler SelectionChanged;
        public event ItemsMovedEventHandler ItemsMoved;
        public event ListEventHandler ListExited;
        public event ItemClickedEventHandler ItemClicked;

        private string currentRootListName = "None";

        public SceneListView()
        {
            InitializeComponent();

            ItemsListView.SelectionChanged += (x, y) => SelectionChanged?.Invoke(x, y);
            ItemsListView.ItemsMoved += (x, y) => ItemsMoved?.Invoke(x, y);
            ItemsListView.ItemClicked += (x, y) => ItemClicked?.Invoke(x, y);

            Graphics g = CreateGraphics();
        }

        /// <summary>
        /// The set used to determine which objects are selected
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ISet<object> SelectedItems
        {
            get => ItemsListView.SelectedItems;
            set
            {
                ItemsListView.SelectedItems = value;
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
                RootListComboBox.SelectedItem = value;
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
                IList prevList = ItemsListView.CurrentList;

                CurrentRootListName = listName;
                listStack.Clear();

                RootListComboBox.Visible = true;
                btnBack.Visible = false;

                ItemsListView.CurrentList = RootLists[listName];

                if (!RootLists.ContainsValue(prevList))
                    ListExited?.Invoke(null, new ListEventArgs(prevList));
            }
        }

        /// <summary>
        /// The current list in the list view
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList CurrentList
        {
            get => ItemsListView.CurrentList;
        }

        /// <summary>
        /// Views a new list and adds the current one to the Stack
        /// </summary>
        /// <param name="list">the list to be entered</param>
        public void EnterList(IList list)
        {
            if (listStack == null || list == CurrentList)
                return;

            if (ItemsListView.CurrentList != null)
                listStack.Push(ItemsListView.CurrentList);
            ItemsListView.CurrentList = list;

            RootListComboBox.Visible = false;
            btnBack.Visible = true;
        }

        public void UnselectCurrentList()
        {
            ItemsListView.CurrentList = null;

            listStack.Clear();

            RootListComboBox.Visible = true;
            btnBack.Visible = false;

        }

        /// <summary>
        /// Tries to go back to the last list in the Stack
        /// </summary>
        public void ExitList()
        {

            if (listStack.Count != 0)
                ItemsListView.CurrentList = listStack.Pop();

            if (listStack.Count == 0)
            {
                RootListComboBox.Visible = true;
                btnBack.Visible = false;
            } 
        }

        public void InvalidateCurrentList()
        {
            ItemsListView.CurrentList = null;
        }

        public bool TryEnsureVisible(object item)
        {
            if (listStack.Count == 0)
            {
                foreach ((string listname, IList rootlist) in rootLists)
                {
                    int index = rootlist.IndexOf(item);

                    if (index != -1)
                    {
                        SetRootList(listname);

                        ItemsListView.EnsureVisisble(index);
                    }
                }
            }
            else
            {
                int index = CurrentList.IndexOf(item);

                if (index != -1)
                {
                    ItemsListView.EnsureVisisble(index);
                }
            }

            return false;
        }

        [Obsolete("The drawing code will update it automatically")]
        public void UpdateAutoScrollHeight()
        {
            
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            ExitList();
            ListEventArgs args = new ListEventArgs(ItemsListView.CurrentList);
            ListExited?.Invoke(this, args);
        }

        private void RootListComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (RootListComboBox.Visible)
                ItemsListView.CurrentList = rootLists[(string)RootListComboBox.SelectedItem];
        }

        public override void Refresh()
        {
            RootListComboBox.Refresh();
            ItemsListView.Refresh();
            base.Refresh();
        }
    }
}
