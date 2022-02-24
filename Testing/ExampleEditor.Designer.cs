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
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnNewWindow = new System.Windows.Forms.Button();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.hideToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sceneListView1 = new GL_EditorFramework.SceneListView();
            this.objectUIControl1 = new GL_EditorFramework.ObjectUIControl();
            this.gL_ControlLegacy1 = new GL_EditorFramework.GL_Core.GL_ControlLegacy();
            this.documentTabControl1 = new GL_EditorFramework.DocumentTabControl();
            this.gL_Control = new GL_EditorFramework.GL_Core.GL_ControlModern();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.documentTabControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnAdd
            // 
            this.btnAdd.AutoSize = true;
            this.btnAdd.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnAdd.Font = new System.Drawing.Font("Segoe UI", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAdd.Location = new System.Drawing.Point(117, 4);
            this.btnAdd.Margin = new System.Windows.Forms.Padding(4);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(112, 26);
            this.btnAdd.TabIndex = 3;
            this.btnAdd.Text = "Add Object";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.BtnAdd_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(4);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.sceneListView1);
            this.splitContainer1.Panel1.Controls.Add(this.panel2);
            this.splitContainer1.Panel1.Controls.Add(this.panel1);
            this.splitContainer1.Panel1.Controls.Add(this.objectUIControl1);
            this.splitContainer1.Panel1.Controls.Add(this.gL_ControlLegacy1);
            this.splitContainer1.Panel1.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.documentTabControl1);
            this.splitContainer1.Size = new System.Drawing.Size(1331, 919);
            this.splitContainer1.SplitterDistance = 442;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 7;
            // 
            // panel2
            // 
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(3, 577);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(439, 5);
            this.panel2.TabIndex = 9;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnAdd);
            this.panel1.Controls.Add(this.btnNewWindow);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(3, 245);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.panel1.Size = new System.Drawing.Size(439, 34);
            this.panel1.TabIndex = 8;
            // 
            // btnNewWindow
            // 
            this.btnNewWindow.AutoSize = true;
            this.btnNewWindow.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnNewWindow.Font = new System.Drawing.Font("Segoe UI", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnNewWindow.Location = new System.Drawing.Point(0, 4);
            this.btnNewWindow.Margin = new System.Windows.Forms.Padding(4);
            this.btnNewWindow.Name = "btnNewWindow";
            this.btnNewWindow.Size = new System.Drawing.Size(117, 26);
            this.btnNewWindow.TabIndex = 7;
            this.btnNewWindow.Text = "New Window";
            this.btnNewWindow.UseVisualStyleBackColor = true;
            this.btnNewWindow.Click += new System.EventHandler(this.BtnNewWindow_Click);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.hideToolStripMenuItem,
            this.showAllToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(137, 52);
            // 
            // hideToolStripMenuItem
            // 
            this.hideToolStripMenuItem.Name = "hideToolStripMenuItem";
            this.hideToolStripMenuItem.Size = new System.Drawing.Size(136, 24);
            this.hideToolStripMenuItem.Text = "Hide";
            this.hideToolStripMenuItem.Click += new System.EventHandler(this.HideToolStripMenuItem_Click);
            // 
            // showAllToolStripMenuItem
            // 
            this.showAllToolStripMenuItem.Name = "showAllToolStripMenuItem";
            this.showAllToolStripMenuItem.Size = new System.Drawing.Size(136, 24);
            this.showAllToolStripMenuItem.Text = "Show All";
            this.showAllToolStripMenuItem.Click += new System.EventHandler(this.ShowAllToolStripMenuItem_Click);
            // 
            // sceneListView1
            // 
            this.sceneListView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sceneListView1.Font = new System.Drawing.Font("Segoe UI", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.sceneListView1.Location = new System.Drawing.Point(3, 279);
            this.sceneListView1.Margin = new System.Windows.Forms.Padding(4);
            this.sceneListView1.Name = "sceneListView1";
            this.sceneListView1.RootLists = ((System.Collections.Generic.Dictionary<string, System.Collections.IList>)(resources.GetObject("sceneListView1.RootLists")));
            this.sceneListView1.Size = new System.Drawing.Size(439, 298);
            this.sceneListView1.TabIndex = 6;
            this.sceneListView1.ItemClicked += new GL_EditorFramework.ItemClickedEventHandler(this.SceneListView1_ItemDoubleClicked);
            // 
            // objectUIControl1
            // 
            this.objectUIControl1.BackColor = System.Drawing.SystemColors.Control;
            this.objectUIControl1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.objectUIControl1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.objectUIControl1.Font = new System.Drawing.Font("Segoe UI", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.objectUIControl1.Location = new System.Drawing.Point(3, 582);
            this.objectUIControl1.Margin = new System.Windows.Forms.Padding(5);
            this.objectUIControl1.Name = "objectUIControl1";
            this.objectUIControl1.Size = new System.Drawing.Size(439, 337);
            this.objectUIControl1.TabIndex = 5;
            // 
            // gL_ControlLegacy1
            // 
            this.gL_ControlLegacy1.BackColor = System.Drawing.Color.Black;
            this.gL_ControlLegacy1.CameraDistance = 10F;
            this.gL_ControlLegacy1.CameraTarget = ((OpenTK.Vector3)(resources.GetObject("gL_ControlLegacy1.CameraTarget")));
            this.gL_ControlLegacy1.CamRotX = 0F;
            this.gL_ControlLegacy1.CamRotY = 0F;
            this.gL_ControlLegacy1.Dock = System.Windows.Forms.DockStyle.Top;
            this.gL_ControlLegacy1.Fov = 0.7853982F;
            this.gL_ControlLegacy1.Location = new System.Drawing.Point(3, 0);
            this.gL_ControlLegacy1.Margin = new System.Windows.Forms.Padding(5);
            this.gL_ControlLegacy1.Name = "gL_ControlLegacy1";
            this.gL_ControlLegacy1.NormPickingDepth = 0F;
            this.gL_ControlLegacy1.ShowOrientationCube = false;
            this.gL_ControlLegacy1.Size = new System.Drawing.Size(439, 245);
            this.gL_ControlLegacy1.Stereoscopy = GL_EditorFramework.GL_Core.GL_ControlBase.StereoscopyType.CROSS_EYE;
            this.gL_ControlLegacy1.TabIndex = 2;
            this.gL_ControlLegacy1.VSync = false;
            this.gL_ControlLegacy1.ZFar = 32000F;
            this.gL_ControlLegacy1.ZNear = 0.32F;
            // 
            // documentTabControl1
            // 
            this.documentTabControl1.Controls.Add(this.gL_Control);
            this.documentTabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.documentTabControl1.Location = new System.Drawing.Point(0, 0);
            this.documentTabControl1.Name = "documentTabControl1";
            this.documentTabControl1.Padding = new System.Windows.Forms.Padding(1, 19, 1, 1);
            this.documentTabControl1.Size = new System.Drawing.Size(884, 919);
            this.documentTabControl1.TabIndex = 2;
            // 
            // gL_Control
            // 
            this.gL_Control.BackColor = System.Drawing.Color.Black;
            this.gL_Control.CameraDistance = 10F;
            this.gL_Control.CameraTarget = ((OpenTK.Vector3)(resources.GetObject("gL_Control.CameraTarget")));
            this.gL_Control.CamRotX = 0F;
            this.gL_Control.CamRotY = 0F;
            this.gL_Control.ContextMenuStrip = this.contextMenuStrip1;
            this.gL_Control.CurrentShader = null;
            this.gL_Control.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gL_Control.Fov = 0.7853982F;
            this.gL_Control.Location = new System.Drawing.Point(1, 19);
            this.gL_Control.Margin = new System.Windows.Forms.Padding(5);
            this.gL_Control.Name = "gL_Control";
            this.gL_Control.NormPickingDepth = 0F;
            this.gL_Control.ShowOrientationCube = true;
            this.gL_Control.Size = new System.Drawing.Size(882, 899);
            this.gL_Control.Stereoscopy = GL_EditorFramework.GL_Core.GL_ControlBase.StereoscopyType.DISABLED;
            this.gL_Control.TabIndex = 1;
            this.gL_Control.VSync = false;
            this.gL_Control.ZFar = 32000F;
            this.gL_Control.ZNear = 0.32F;
            // 
            // ExampleEditor
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1331, 919);
            this.Controls.Add(this.splitContainer1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "ExampleEditor";
            this.Text = "Testing";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            this.documentTabControl1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private GL_EditorFramework.GL_Core.GL_ControlLegacy gL_ControlLegacy1;
        private System.Windows.Forms.Button btnAdd;
        private GL_EditorFramework.ObjectUIControl objectUIControl1;
        private GL_EditorFramework.SceneListView sceneListView1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem hideToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showAllToolStripMenuItem;
        private GL_EditorFramework.GL_Core.GL_ControlModern gL_Control;
        private System.Windows.Forms.Button btnNewWindow;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private GL_EditorFramework.DocumentTabControl documentTabControl1;
    }
}

