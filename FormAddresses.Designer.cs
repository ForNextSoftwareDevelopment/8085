namespace _8085
{
    partial class FormAddresses
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormAddresses));
            this.buttonOK = new System.Windows.Forms.Button();
            this.labelLoadAddress = new System.Windows.Forms.Label();
            this.textBoxLoadAddress = new System.Windows.Forms.TextBox();
            this.textBoxStartAddress = new System.Windows.Forms.TextBox();
            this.labelStartAddress = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.Location = new System.Drawing.Point(12, 75);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(160, 23);
            this.buttonOK.TabIndex = 3;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // labelLoadAddress
            // 
            this.labelLoadAddress.AutoSize = true;
            this.labelLoadAddress.Location = new System.Drawing.Point(12, 15);
            this.labelLoadAddress.Name = "labelLoadAddress";
            this.labelLoadAddress.Size = new System.Drawing.Size(75, 13);
            this.labelLoadAddress.TabIndex = 4;
            this.labelLoadAddress.Text = "Load Address:";
            // 
            // textBoxLoadAddress
            // 
            this.textBoxLoadAddress.Location = new System.Drawing.Point(108, 12);
            this.textBoxLoadAddress.Name = "textBoxLoadAddress";
            this.textBoxLoadAddress.Size = new System.Drawing.Size(64, 20);
            this.textBoxLoadAddress.TabIndex = 5;
            // 
            // textBoxStartAddress
            // 
            this.textBoxStartAddress.Location = new System.Drawing.Point(108, 38);
            this.textBoxStartAddress.Name = "textBoxStartAddress";
            this.textBoxStartAddress.Size = new System.Drawing.Size(64, 20);
            this.textBoxStartAddress.TabIndex = 7;
            // 
            // labelStartAddress
            // 
            this.labelStartAddress.AutoSize = true;
            this.labelStartAddress.Location = new System.Drawing.Point(12, 41);
            this.labelStartAddress.Name = "labelStartAddress";
            this.labelStartAddress.Size = new System.Drawing.Size(73, 13);
            this.labelStartAddress.TabIndex = 6;
            this.labelStartAddress.Text = "Start Address:";
            // 
            // FormAddresses
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(184, 110);
            this.ControlBox = false;
            this.Controls.Add(this.textBoxStartAddress);
            this.Controls.Add(this.labelStartAddress);
            this.Controls.Add(this.textBoxLoadAddress);
            this.Controls.Add(this.labelLoadAddress);
            this.Controls.Add(this.buttonOK);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormAddresses";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Load/Execute Addresses";
            this.Load += new System.EventHandler(this.FormAddresses_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Label labelLoadAddress;
        private System.Windows.Forms.TextBox textBoxLoadAddress;
        private System.Windows.Forms.TextBox textBoxStartAddress;
        private System.Windows.Forms.Label labelStartAddress;
    }
}