namespace _8085
{
    partial class FormTerminal
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormTerminal));
            this.tbTerminal = new System.Windows.Forms.RichTextBox();
            this.tbKeyBuffer = new System.Windows.Forms.TextBox();
            this.lblKeyBuffer = new System.Windows.Forms.Label();
            this.cbBaudRate = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // tbTerminal
            // 
            this.tbTerminal.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbTerminal.Location = new System.Drawing.Point(0, 0);
            this.tbTerminal.Name = "tbTerminal";
            this.tbTerminal.Size = new System.Drawing.Size(464, 208);
            this.tbTerminal.TabIndex = 0;
            this.tbTerminal.Text = "";
            this.tbTerminal.WordWrap = false;
            this.tbTerminal.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbTerminal_KeyPress);
            // 
            // tbKeyBuffer
            // 
            this.tbKeyBuffer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbKeyBuffer.Location = new System.Drawing.Point(74, 214);
            this.tbKeyBuffer.Name = "tbKeyBuffer";
            this.tbKeyBuffer.Size = new System.Drawing.Size(285, 20);
            this.tbKeyBuffer.TabIndex = 1;
            // 
            // lblKeyBuffer
            // 
            this.lblKeyBuffer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblKeyBuffer.AutoSize = true;
            this.lblKeyBuffer.Location = new System.Drawing.Point(12, 217);
            this.lblKeyBuffer.Name = "lblKeyBuffer";
            this.lblKeyBuffer.Size = new System.Drawing.Size(56, 13);
            this.lblKeyBuffer.TabIndex = 2;
            this.lblKeyBuffer.Text = "KeyBuffer:";
            // 
            // cbBaudRate
            // 
            this.cbBaudRate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cbBaudRate.FormattingEnabled = true;
            this.cbBaudRate.Items.AddRange(new object[] {
            "    110 Bd",
            "    150 Bd",
            "    300 Bd",
            "    600 Bd",
            "  1200 Bd",
            "  2400 Bd",
            "  4800 Bd",
            "  9600 Bd",
            "19200 Bd"});
            this.cbBaudRate.Location = new System.Drawing.Point(365, 214);
            this.cbBaudRate.Name = "cbBaudRate";
            this.cbBaudRate.Size = new System.Drawing.Size(92, 21);
            this.cbBaudRate.TabIndex = 3;
            // 
            // FormTerminal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(464, 241);
            this.ControlBox = false;
            this.Controls.Add(this.cbBaudRate);
            this.Controls.Add(this.lblKeyBuffer);
            this.Controls.Add(this.tbKeyBuffer);
            this.Controls.Add(this.tbTerminal);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormTerminal";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Terminal";
            this.Load += new System.EventHandler(this.FormTerminal_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.RichTextBox tbTerminal;
        private System.Windows.Forms.Label lblKeyBuffer;
        private System.Windows.Forms.TextBox tbKeyBuffer;
        private System.Windows.Forms.ComboBox cbBaudRate;
    }
}