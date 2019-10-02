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
            this.btnAdd = new System.Windows.Forms.Button();
            this.sceneListView1 = new GL_EditorFramework.SceneListView();
            this.objectUIControl1 = new GL_EditorFramework.ObjectUIControl();
            this.gL_ControlLegacy1 = new GL_EditorFramework.GL_Core.GL_ControlLegacy();
            this.gL_ControlModern1 = new GL_EditorFramework.GL_Core.GL_ControlModern();
            this.SuspendLayout();
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(12, 208);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 3;
            this.btnAdd.Text = "Add Object";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.BtnAdd_Click);
            // 
            // sceneListView1
            // 
            this.sceneListView1.Location = new System.Drawing.Point(12, 237);
            this.sceneListView1.Name = "sceneListView1";
            this.sceneListView1.Size = new System.Drawing.Size(279, 226);
            this.sceneListView1.TabIndex = 6;
            // 
            // objectPropertyControl1
            // 
            this.objectUIControl1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.objectUIControl1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.objectUIControl1.CurrentObjectUIProvider = null;
            this.objectUIControl1.Location = new System.Drawing.Point(13, 470);
            this.objectUIControl1.Name = "objectPropertyControl1";
            this.objectUIControl1.Size = new System.Drawing.Size(278, 265);
            this.objectUIControl1.TabIndex = 5;
            // 
            // gL_ControlLegacy1
            // 
            this.gL_ControlLegacy1.BackColor = System.Drawing.Color.Black;
            this.gL_ControlLegacy1.CamRotX = 0F;
            this.gL_ControlLegacy1.CamRotY = 0F;
            this.gL_ControlLegacy1.Fov = 0.7853982F;
            this.gL_ControlLegacy1.Location = new System.Drawing.Point(13, 20);
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
            this.gL_ControlModern1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gL_ControlModern1.BackColor = System.Drawing.Color.Black;
            this.gL_ControlModern1.CamRotX = 0F;
            this.gL_ControlModern1.CamRotY = 0F;
            this.gL_ControlModern1.CurrentShader = null;
            this.gL_ControlModern1.Fov = 0.7853982F;
            this.gL_ControlModern1.Location = new System.Drawing.Point(297, 20);
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
            // ExampleEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(998, 747);
            this.Controls.Add(this.sceneListView1);
            this.Controls.Add(this.objectUIControl1);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.gL_ControlLegacy1);
            this.Controls.Add(this.gL_ControlModern1);
            this.Name = "ExampleEditor";
            this.Text = "Testing";
            this.ResumeLayout(false);

        }

        #endregion
        private GL_EditorFramework.GL_Core.GL_ControlModern gL_ControlModern1;
        private GL_EditorFramework.GL_Core.GL_ControlLegacy gL_ControlLegacy1;
        private System.Windows.Forms.Button btnAdd;
        private GL_EditorFramework.ObjectUIControl objectUIControl1;
        private GL_EditorFramework.SceneListView sceneListView1;
    }
}

