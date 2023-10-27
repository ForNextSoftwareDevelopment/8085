namespace _8085
{
    partial class FormSerial
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSerial));
            this.lblSID = new System.Windows.Forms.Label();
            this.pbSID = new System.Windows.Forms.PictureBox();
            this.pbSOD = new System.Windows.Forms.PictureBox();
            this.lblSOD = new System.Windows.Forms.Label();
            this.hScrollBar = new System.Windows.Forms.HScrollBar();
            this.chkKeepUp = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbSID)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbSOD)).BeginInit();
            this.SuspendLayout();
            // 
            // lblSID
            // 
            this.lblSID.AutoSize = true;
            this.lblSID.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSID.Location = new System.Drawing.Point(12, 9);
            this.lblSID.Name = "lblSID";
            this.lblSID.Size = new System.Drawing.Size(44, 18);
            this.lblSID.TabIndex = 10;
            this.lblSID.Text = "SID:";
            // 
            // pbSID
            // 
            this.pbSID.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbSID.BackColor = System.Drawing.Color.WhiteSmoke;
            this.pbSID.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pbSID.Location = new System.Drawing.Point(12, 30);
            this.pbSID.Name = "pbSID";
            this.pbSID.Size = new System.Drawing.Size(360, 50);
            this.pbSID.TabIndex = 11;
            this.pbSID.TabStop = false;
            // 
            // pbSOD
            // 
            this.pbSOD.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbSOD.BackColor = System.Drawing.Color.WhiteSmoke;
            this.pbSOD.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pbSOD.Location = new System.Drawing.Point(12, 107);
            this.pbSOD.Name = "pbSOD";
            this.pbSOD.Size = new System.Drawing.Size(360, 50);
            this.pbSOD.TabIndex = 0;
            this.pbSOD.TabStop = false;
            // 
            // lblSOD
            // 
            this.lblSOD.AutoSize = true;
            this.lblSOD.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSOD.Location = new System.Drawing.Point(12, 86);
            this.lblSOD.Name = "lblSOD";
            this.lblSOD.Size = new System.Drawing.Size(49, 18);
            this.lblSOD.TabIndex = 12;
            this.lblSOD.Text = "SOD:";
            // 
            // hScrollBar
            // 
            this.hScrollBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.hScrollBar.Location = new System.Drawing.Point(15, 175);
            this.hScrollBar.Name = "hScrollBar";
            this.hScrollBar.Size = new System.Drawing.Size(360, 17);
            this.hScrollBar.TabIndex = 13;
            this.hScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hScrollBar_Scroll);
            this.hScrollBar.ValueChanged += new System.EventHandler(this.hScrollBar_ValueChanged);
            // 
            // chkKeepUp
            // 
            this.chkKeepUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkKeepUp.AutoSize = true;
            this.chkKeepUp.Checked = true;
            this.chkKeepUp.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkKeepUp.Location = new System.Drawing.Point(304, 12);
            this.chkKeepUp.Name = "chkKeepUp";
            this.chkKeepUp.Size = new System.Drawing.Size(68, 17);
            this.chkKeepUp.TabIndex = 14;
            this.chkKeepUp.Text = "Keep Up";
            this.chkKeepUp.UseVisualStyleBackColor = true;
            // 
            // FormSerial
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Silver;
            this.ClientSize = new System.Drawing.Size(384, 201);
            this.ControlBox = false;
            this.Controls.Add(this.chkKeepUp);
            this.Controls.Add(this.hScrollBar);
            this.Controls.Add(this.lblSOD);
            this.Controls.Add(this.pbSID);
            this.Controls.Add(this.lblSID);
            this.Controls.Add(this.pbSOD);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(1600, 240);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(400, 240);
            this.Name = "FormSerial";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Serial (SID & SOD)";
            this.Load += new System.EventHandler(this.FormSerial_Load);
            this.Resize += new System.EventHandler(this.FormSerial_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.pbSID)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbSOD)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pbSOD;
        private System.Windows.Forms.Label lblSID;
        private System.Windows.Forms.PictureBox pbSID;
        private System.Windows.Forms.Label lblSOD;
        private System.Windows.Forms.HScrollBar hScrollBar;
        private System.Windows.Forms.CheckBox chkKeepUp;
    }
}