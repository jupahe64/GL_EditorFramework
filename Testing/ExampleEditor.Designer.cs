namespace Example
{
    partial class ExampleEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExampleEditor));
            this.btnAdd = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.grabSelectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sceneListView1 = new GL_EditorFramework.SceneListView();
            this.objectUIControl1 = new GL_EditorFramework.ObjectUIControl();
            this.gL_ControlLegacy1 = new GL_EditorFramework.GL_Core.GL_ControlLegacy();
            this.gL_ControlModern1 = new GL_EditorFramework.GL_Core.GL_ControlModern();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(3, 208);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(84, 23);
            this.btnAdd.TabIndex = 3;
            this.btnAdd.Text = "Add Object";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.BtnAdd_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.sceneListView1);
            this.splitContainer1.Panel1.Controls.Add(this.objectUIControl1);
            this.splitContainer1.Panel1.Controls.Add(this.btnAdd);
            this.splitContainer1.Panel1.Controls.Add(this.gL_ControlLegacy1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.gL_ControlModern1);
            this.splitContainer1.Size = new System.Drawing.Size(998, 747);
            this.splitContainer1.SplitterDistance = 332;
            this.splitContainer1.TabIndex = 7;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.grabSelectionToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(151, 26);
            // 
            // grabSelectionToolStripMenuItem
            // 
            this.grabSelectionToolStripMenuItem.Name = "grabSelectionToolStripMenuItem";
            this.grabSelectionToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
            this.grabSelectionToolStripMenuItem.Text = "Grab Selection";
            // 
            // sceneListView1
            // 
            this.sceneListView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sceneListView1.Location = new System.Drawing.Point(3, 237);
            this.sceneListView1.Name = "sceneListView1";
            this.sceneListView1.RootLists = ((System.Collections.Generic.Dictionary<string, System.Collections.IList>)(resources.GetObject("sceneListView1.RootLists")));
            this.sceneListView1.Size = new System.Drawing.Size(326, 226);
            this.sceneListView1.TabIndex = 6;
            this.sceneListView1.ItemDoubleClicked += new GL_EditorFramework.ItemDoubleClickedEventHandler(this.SceneListView1_ItemDoubleClicked);
            // 
            // objectUIControl1
            // 
            this.objectUIControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.objectUIControl1.BackColor = System.Drawing.SystemColors.Control;
            this.objectUIControl1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.objectUIControl1.Location = new System.Drawing.Point(3, 470);
            this.objectUIControl1.Name = "objectUIControl1";
            this.objectUIControl1.Size = new System.Drawing.Size(326, 274);
            this.objectUIControl1.TabIndex = 5;
            // 
            // gL_ControlLegacy1
            // 
            this.gL_ControlLegacy1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gL_ControlLegacy1.BackColor = System.Drawing.Color.Black;
            this.gL_ControlLegacy1.CamRotX = 0F;
            this.gL_ControlLegacy1.CamRotY = 0F;
            this.gL_ControlLegacy1.Fov = 0.7853982F;
            this.gL_ControlLegacy1.Location = new System.Drawing.Point(3, 3);
            this.gL_ControlLegacy1.Name = "gL_ControlLegacy1";
            this.gL_ControlLegacy1.NormPickingDepth = 0F;
            this.gL_ControlLegacy1.ShowOrientationCube = false;
            this.gL_ControlLegacy1.Size = new System.Drawing.Size(326, 199);
            this.gL_ControlLegacy1.Stereoscopy = true;
            this.gL_ControlLegacy1.TabIndex = 2;
            this.gL_ControlLegacy1.VSync = false;
            this.gL_ControlLegacy1.ZFar = 32000F;
            this.gL_ControlLegacy1.ZNear = 0.32F;
            // 
            // gL_ControlModern1
            // 
            this.gL_ControlModern1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gL_ControlModern1.BackColor = System.Drawing.Color.Black;
            this.gL_ControlModern1.CamRotX = 0F;
            this.gL_ControlModern1.CamRotY = 0F;
            this.gL_ControlModern1.ContextMenuStrip = this.contextMenuStrip1;
            this.gL_ControlModern1.CurrentShader = null;
            this.gL_ControlModern1.Fov = 0.7853982F;
            this.gL_ControlModern1.Location = new System.Drawing.Point(3, 3);
            this.gL_ControlModern1.Name = "gL_ControlModern1";
            this.gL_ControlModern1.NormPickingDepth = 0F;
            this.gL_ControlModern1.ShowOrientationCube = true;
            this.gL_ControlModern1.Size = new System.Drawing.Size(656, 741);
            this.gL_ControlModern1.Stereoscopy = false;
            this.gL_ControlModern1.TabIndex = 1;
            this.gL_ControlModern1.VSync = false;
            this.gL_ControlModern1.ZFar = 32000F;
            this.gL_ControlModern1.ZNear = 0.32F;
            // 
            // ExampleEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(998, 747);
            this.Controls.Add(this.splitContainer1);
            this.Name = "ExampleEditor";
            this.Text = "Testing";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private GL_EditorFramework.GL_Core.GL_ControlModern gL_ControlModern1;
        private GL_EditorFramework.GL_Core.GL_ControlLegacy gL_ControlLegacy1;
        private System.Windows.Forms.Button btnAdd;
        private GL_EditorFramework.ObjectUIControl objectUIControl1;
        private GL_EditorFramework.SceneListView sceneListView1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem grabSelectionToolStripMenuItem;
    }
}

