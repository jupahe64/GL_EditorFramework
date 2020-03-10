namespace GL_EditorFramework
{
    partial class SceneListView
    {
        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.RootListComboBox = new System.Windows.Forms.ComboBox();
            this.ItemsListView = new GL_EditorFramework.FastListView();
            this.btnBack = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // RootListComboBox
            // 
            this.RootListComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.RootListComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.RootListComboBox.FormattingEnabled = true;
            this.RootListComboBox.Location = new System.Drawing.Point(0, 2);
            this.RootListComboBox.Name = "RootListComboBox";
            this.RootListComboBox.Size = new System.Drawing.Size(300, 21);
            this.RootListComboBox.TabIndex = 3;
            this.RootListComboBox.SelectedIndexChanged += new System.EventHandler(this.RootListComboBox_SelectedIndexChanged);
            // 
            // ItemsListView
            // 
            this.ItemsListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ItemsListView.AutoScroll = true;
            this.ItemsListView.BackColor = System.Drawing.SystemColors.Window;
            this.ItemsListView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ItemsListView.Location = new System.Drawing.Point(1, 29);
            this.ItemsListView.Margin = new System.Windows.Forms.Padding(1, 3, 1, 1);
            this.ItemsListView.Name = "ItemsListView";
            this.ItemsListView.Size = new System.Drawing.Size(298, 270);
            this.ItemsListView.TabIndex = 0;
            this.ItemsListView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.ItemsListView_MouseDoubleClick);
            // 
            // btnBack
            // 
            this.btnBack.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBack.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBack.Image = global::GL_EditorFramework.Properties.Resources.BackButtonIcon;
            this.btnBack.Location = new System.Drawing.Point(0, 2);
            this.btnBack.Margin = new System.Windows.Forms.Padding(1);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(29, 21);
            this.btnBack.TabIndex = 2;
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Visible = false;
            this.btnBack.Click += new System.EventHandler(this.BackButton_Click);
            // 
            // SceneListView
            // 
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.RootListComboBox);
            this.Controls.Add(this.ItemsListView);
            this.Name = "SceneListView";
            this.Size = new System.Drawing.Size(300, 300);
            this.ResumeLayout(false);

        }

        #endregion

        private FastListView ItemsListView;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.ComboBox RootListComboBox;
    }
}
