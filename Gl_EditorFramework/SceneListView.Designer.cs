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
            this.btnBack = new System.Windows.Forms.Button();
            this.ItemsListView = new GL_EditorFramework.FastListView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // RootListComboBox
            // 
            this.RootListComboBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RootListComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.RootListComboBox.FormattingEnabled = true;
            this.RootListComboBox.Location = new System.Drawing.Point(53, 0);
            this.RootListComboBox.Name = "RootListComboBox";
            this.RootListComboBox.Size = new System.Drawing.Size(247, 24);
            this.RootListComboBox.TabIndex = 3;
            this.RootListComboBox.SelectedIndexChanged += new System.EventHandler(this.RootListComboBox_SelectedIndexChanged);
            // 
            // btnBack
            // 
            this.btnBack.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnBack.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBack.Image = global::GL_EditorFramework.Properties.Resources.BackButtonIcon;
            this.btnBack.Location = new System.Drawing.Point(0, 0);
            this.btnBack.Margin = new System.Windows.Forms.Padding(1);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(53, 28);
            this.btnBack.TabIndex = 2;
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Visible = false;
            this.btnBack.Click += new System.EventHandler(this.BackButton_Click);
            // 
            // ItemsListView
            // 
            this.ItemsListView.AutoScroll = true;
            this.ItemsListView.BackColor = System.Drawing.SystemColors.Window;
            this.ItemsListView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ItemsListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ItemsListView.Location = new System.Drawing.Point(0, 28);
            this.ItemsListView.Margin = new System.Windows.Forms.Padding(1, 3, 1, 1);
            this.ItemsListView.Name = "ItemsListView";
            this.ItemsListView.Size = new System.Drawing.Size(300, 272);
            this.ItemsListView.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.RootListComboBox);
            this.panel1.Controls.Add(this.btnBack);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(300, 28);
            this.panel1.TabIndex = 4;
            // 
            // SceneListView
            // 
            this.Controls.Add(this.ItemsListView);
            this.Controls.Add(this.panel1);
            this.Name = "SceneListView";
            this.Size = new System.Drawing.Size(300, 300);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private FastListView ItemsListView;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.ComboBox RootListComboBox;
        private System.Windows.Forms.Panel panel1;
    }
}
