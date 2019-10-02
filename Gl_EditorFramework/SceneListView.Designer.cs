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
            this.listView = new GL_EditorFramework.FastListView();
            this.rootListChangePanel = new GL_EditorFramework.SceneListView.RootListChangePanel();
            this.btnBack = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listView
            // 
            this.listView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView.AutoScroll = true;
            this.listView.BackColor = System.Drawing.SystemColors.Window;
            this.listView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.listView.Location = new System.Drawing.Point(1, 29);
            this.listView.Margin = new System.Windows.Forms.Padding(1, 3, 1, 1);
            this.listView.Name = "listView";
            this.listView.Size = new System.Drawing.Size(298, 270);
            this.listView.TabIndex = 0;
            // 
            // categoryPanel
            // 
            this.rootListChangePanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rootListChangePanel.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.rootListChangePanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rootListChangePanel.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rootListChangePanel.Location = new System.Drawing.Point(1, 1);
            this.rootListChangePanel.Margin = new System.Windows.Forms.Padding(1);
            this.rootListChangePanel.Name = "categoryPanel";
            this.rootListChangePanel.Size = new System.Drawing.Size(298, 24);
            this.rootListChangePanel.TabIndex = 1;
            // 
            // btnBack
            // 
            this.btnBack.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBack.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBack.Location = new System.Drawing.Point(0, 0);
            this.btnBack.Margin = new System.Windows.Forms.Padding(1);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(300, 24);
            this.btnBack.TabIndex = 2;
            this.btnBack.Text = "Back";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Visible = false;
            this.btnBack.Click += new System.EventHandler(this.BtnBack_Click);
            // 
            // SceneListView
            // 
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.rootListChangePanel);
            this.Controls.Add(this.listView);
            this.Name = "SceneListView";
            this.Size = new System.Drawing.Size(300, 300);
            this.ResumeLayout(false);

        }

        #endregion

        private FastListView listView;
        private RootListChangePanel rootListChangePanel;
        private System.Windows.Forms.Button btnBack;
    }
}
