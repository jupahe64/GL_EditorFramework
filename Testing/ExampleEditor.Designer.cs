﻿namespace Example
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
            this.btnNewWindow = new System.Windows.Forms.Button();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.hideToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sceneListView1 = new GL_EditorFramework.SceneListView();
            this.objectUIControl1 = new GL_EditorFramework.ObjectUIControl();
            this.gL_ControlLegacy1 = new GL_EditorFramework.GL_Core.GL_ControlLegacy();
            this.gL_Control = new GL_EditorFramework.GL_Core.GL_ControlModern();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(12, 208);
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
            this.splitContainer1.Panel1.Controls.Add(this.btnNewWindow);
            this.splitContainer1.Panel1.Controls.Add(this.sceneListView1);
            this.splitContainer1.Panel1.Controls.Add(this.objectUIControl1);
            this.splitContainer1.Panel1.Controls.Add(this.btnAdd);
            this.splitContainer1.Panel1.Controls.Add(this.gL_ControlLegacy1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.gL_Control);
            this.splitContainer1.Size = new System.Drawing.Size(998, 747);
            this.splitContainer1.SplitterDistance = 332;
            this.splitContainer1.TabIndex = 7;
            // 
            // btnNewWindow
            // 
            this.btnNewWindow.Location = new System.Drawing.Point(102, 208);
            this.btnNewWindow.Name = "btnNewWindow";
            this.btnNewWindow.Size = new System.Drawing.Size(84, 23);
            this.btnNewWindow.TabIndex = 7;
            this.btnNewWindow.Text = "New Window";
            this.btnNewWindow.UseVisualStyleBackColor = true;
            this.btnNewWindow.Click += new System.EventHandler(this.BtnNewWindow_Click);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.hideToolStripMenuItem,
            this.showAllToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(121, 48);
            // 
            // hideToolStripMenuItem
            // 
            this.hideToolStripMenuItem.Name = "hideToolStripMenuItem";
            this.hideToolStripMenuItem.Size = new System.Drawing.Size(120, 22);
            this.hideToolStripMenuItem.Text = "Hide";
            this.hideToolStripMenuItem.Click += new System.EventHandler(this.HideToolStripMenuItem_Click);
            // 
            // showAllToolStripMenuItem
            // 
            this.showAllToolStripMenuItem.Name = "showAllToolStripMenuItem";
            this.showAllToolStripMenuItem.Size = new System.Drawing.Size(120, 22);
            this.showAllToolStripMenuItem.Text = "Show All";
            this.showAllToolStripMenuItem.Click += new System.EventHandler(this.ShowAllToolStripMenuItem_Click);
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
            this.sceneListView1.ItemClicked += new GL_EditorFramework.ItemClickedEventHandler(this.SceneListView1_ItemDoubleClicked);
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
            this.gL_ControlLegacy1.CameraDistance = 10F;
            this.gL_ControlLegacy1.CameraTarget = ((OpenTK.Vector3)(resources.GetObject("gL_ControlLegacy1.CameraTarget")));
            this.gL_ControlLegacy1.CamRotX = 0F;
            this.gL_ControlLegacy1.CamRotY = 0F;
            this.gL_ControlLegacy1.Fov = 0.7853982F;
            this.gL_ControlLegacy1.Location = new System.Drawing.Point(3, 3);
            this.gL_ControlLegacy1.Name = "gL_ControlLegacy1";
            this.gL_ControlLegacy1.NormPickingDepth = 0F;
            this.gL_ControlLegacy1.ShowOrientationCube = false;
            this.gL_ControlLegacy1.Size = new System.Drawing.Size(326, 199);
            this.gL_ControlLegacy1.Stereoscopy = GL_EditorFramework.GL_Core.GL_ControlBase.StereoscopyType.CROSS_EYE;
            this.gL_ControlLegacy1.TabIndex = 2;
            this.gL_ControlLegacy1.VSync = false;
            this.gL_ControlLegacy1.ZFar = 32000F;
            this.gL_ControlLegacy1.ZNear = 0.32F;
            // 
            // gL_Control
            // 
            this.gL_Control.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gL_Control.BackColor = System.Drawing.Color.Black;
            this.gL_Control.CameraDistance = 10F;
            this.gL_Control.CameraTarget = ((OpenTK.Vector3)(resources.GetObject("gL_Control.CameraTarget")));
            this.gL_Control.CamRotX = 0F;
            this.gL_Control.CamRotY = 0F;
            this.gL_Control.ContextMenuStrip = this.contextMenuStrip1;
            this.gL_Control.CurrentShader = null;
            this.gL_Control.Fov = 0.7853982F;
            this.gL_Control.Location = new System.Drawing.Point(3, 3);
            this.gL_Control.Name = "gL_Control";
            this.gL_Control.NormPickingDepth = 0F;
            this.gL_Control.ShowOrientationCube = true;
            this.gL_Control.Size = new System.Drawing.Size(656, 741);
            this.gL_Control.Stereoscopy = GL_EditorFramework.GL_Core.GL_ControlBase.StereoscopyType.DISABLED;
            this.gL_Control.TabIndex = 1;
            this.gL_Control.VSync = false;
            this.gL_Control.ZFar = 32000F;
            this.gL_Control.ZNear = 0.32F;
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
    }
}

