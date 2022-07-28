namespace _8085
{
    partial class FormAbout
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormAbout));
            this.tbAbout = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // tbAbout
            // 
            this.tbAbout.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbAbout.BackColor = System.Drawing.SystemColors.Info;
            this.tbAbout.Cursor = System.Windows.Forms.Cursors.Default;
            this.tbAbout.Font = new System.Drawing.Font("Caladea", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbAbout.Location = new System.Drawing.Point(12, 12);
            this.tbAbout.Multiline = true;
            this.tbAbout.Name = "tbAbout";
            this.tbAbout.ReadOnly = true;
            this.tbAbout.Size = new System.Drawing.Size(416, 252);
            this.tbAbout.TabIndex = 1;
            this.tbAbout.Text = "8085 Simulator\r\nCreated by Dirk Prins\r\ninfo@fornext.nl\r\n\r\nCopyright (c) 2022 D. P" +
    "rins\r\n----------------------------------------------------";
            this.tbAbout.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.BackColor = System.Drawing.SystemColors.ControlLight;
            this.btnOK.Font = new System.Drawing.Font("Caladea", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOK.Location = new System.Drawing.Point(12, 270);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(416, 35);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = false;
            this.btnOK.Click += new System.EventHandler(this.button_Click);
            // 
            // FormAbout
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.ClientSize = new System.Drawing.Size(440, 317);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.tbAbout);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormAbout";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About";
            this.Shown += new System.EventHandler(this.FormAbout_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbAbout;
        private System.Windows.Forms.Button btnOK;
    }
}