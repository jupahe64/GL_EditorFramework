namespace Testing
{
    partial class TestingForm
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
            this.button1 = new System.Windows.Forms.Button();
            this.sceneListView1 = new GL_EditorFramework.SceneListView();
            this.objectPropertyControl1 = new GL_EditorFramework.ObjectPropertyControl();
            this.gL_ControlLegacy1 = new GL_EditorFramework.GL_Core.GL_ControlLegacy();
            this.gL_ControlModern1 = new GL_EditorFramework.GL_Core.GL_ControlModern();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 208);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "Add Object";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // sceneListView1
            // 
            this.sceneListView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.sceneListView1.CurrentCategory = "None";
            this.sceneListView1.Location = new System.Drawing.Point(12, 237);
            this.sceneListView1.Name = "sceneListView1";
            this.sceneListView1.SelectedItems = null;
            this.sceneListView1.Size = new System.Drawing.Size(279, 226);
            this.sceneListView1.TabIndex = 6;
            // 
            // objectPropertyControl1
            // 
            this.objectPropertyControl1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.objectPropertyControl1.CurrentPropertyContainer = null;
            this.objectPropertyControl1.Location = new System.Drawing.Point(13, 470);
            this.objectPropertyControl1.Name = "objectPropertyControl1";
            this.objectPropertyControl1.Size = new System.Drawing.Size(278, 265);
            this.objectPropertyControl1.TabIndex = 5;
            // 
            // gL_ControlLegacy1
            // 
            this.gL_ControlLegacy1.ActiveCamera = null;
            this.gL_ControlLegacy1.BackColor = System.Drawing.Color.Black;
            this.gL_ControlLegacy1.CamRotX = 0F;
            this.gL_ControlLegacy1.CamRotY = 0F;
            this.gL_ControlLegacy1.DragStartPos = new System.Drawing.Point(0, 0);
            this.gL_ControlLegacy1.Fov = 0.7853982F;
            this.gL_ControlLegacy1.Location = new System.Drawing.Point(13, 20);
            this.gL_ControlLegacy1.MainDrawable = null;
            this.gL_ControlLegacy1.Name = "gL_ControlLegacy1";
            this.gL_ControlLegacy1.NormPickingDepth = 0F;
            this.gL_ControlLegacy1.ShowOrientationCube = false;
            this.gL_ControlLegacy1.Size = new System.Drawing.Size(278, 182);
            this.gL_ControlLegacy1.Stereoscopy = true;
            this.gL_ControlLegacy1.TabIndex = 2;
            this.gL_ControlLegacy1.VSync = false;
            this.gL_ControlLegacy1.ZFar = 32000F;
            this.gL_ControlLegacy1.ZNear = 0.32F;
            // 
            // gL_ControlModern1
            // 
            this.gL_ControlModern1.ActiveCamera = null;
            this.gL_ControlModern1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gL_ControlModern1.BackColor = System.Drawing.Color.Black;
            this.gL_ControlModern1.CamRotX = 0F;
            this.gL_ControlModern1.CamRotY = 0F;
            this.gL_ControlModern1.CurrentShader = null;
            this.gL_ControlModern1.DragStartPos = new System.Drawing.Point(0, 0);
            this.gL_ControlModern1.Fov = 0.7853982F;
            this.gL_ControlModern1.Location = new System.Drawing.Point(297, 20);
            this.gL_ControlModern1.MainDrawable = null;
            this.gL_ControlModern1.Name = "gL_ControlModern1";
            this.gL_ControlModern1.NormPickingDepth = 0F;
            this.gL_ControlModern1.ShowOrientationCube = true;
            this.gL_ControlModern1.Size = new System.Drawing.Size(689, 715);
            this.gL_ControlModern1.Stereoscopy = false;
            this.gL_ControlModern1.TabIndex = 1;
            this.gL_ControlModern1.VSync = false;
            this.gL_ControlModern1.ZFar = 32000F;
            this.gL_ControlModern1.ZNear = 0.32F;
            // 
            // TestingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(998, 747);
            this.Controls.Add(this.sceneListView1);
            this.Controls.Add(this.objectPropertyControl1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.gL_ControlLegacy1);
            this.Controls.Add(this.gL_ControlModern1);
            this.Name = "TestingForm";
            this.Text = "Testing";
            this.ResumeLayout(false);

        }

        #endregion
        private GL_EditorFramework.GL_Core.GL_ControlModern gL_ControlModern1;
        private GL_EditorFramework.GL_Core.GL_ControlLegacy gL_ControlLegacy1;
        private System.Windows.Forms.Button button1;
        private GL_EditorFramework.ObjectPropertyControl objectPropertyControl1;
        private GL_EditorFramework.SceneListView sceneListView1;
    }
}

