namespace _8085
{
    partial class FormDisAssembler
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormDisAssembler));
            this.textBoxBinary = new System.Windows.Forms.TextBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.textBoxProgram = new System.Windows.Forms.TextBox();
            this.textBoxExeAddress = new System.Windows.Forms.TextBox();
            this.btnAddExeAddress = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textBoxBinary
            // 
            this.textBoxBinary.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.textBoxBinary.BackColor = System.Drawing.SystemColors.Window;
            this.textBoxBinary.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxBinary.Location = new System.Drawing.Point(12, 12);
            this.textBoxBinary.Multiline = true;
            this.textBoxBinary.Name = "textBoxBinary";
            this.textBoxBinary.ReadOnly = true;
            this.textBoxBinary.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxBinary.Size = new System.Drawing.Size(334, 397);
            this.textBoxBinary.TabIndex = 0;
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(709, 415);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(79, 23);
            this.buttonOK.TabIndex = 2;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(12, 415);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(82, 23);
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // textBoxProgram
            // 
            this.textBoxProgram.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxProgram.BackColor = System.Drawing.SystemColors.Info;
            this.textBoxProgram.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxProgram.Location = new System.Drawing.Point(352, 12);
            this.textBoxProgram.Multiline = true;
            this.textBoxProgram.Name = "textBoxProgram";
            this.textBoxProgram.ReadOnly = true;
            this.textBoxProgram.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxProgram.Size = new System.Drawing.Size(436, 397);
            this.textBoxProgram.TabIndex = 4;
            this.textBoxProgram.MouseDown += new System.Windows.Forms.MouseEventHandler(this.textBoxProgram_MouseDown);
            // 
            // textBoxExeAddress
            // 
            this.textBoxExeAddress.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.textBoxExeAddress.Location = new System.Drawing.Point(442, 418);
            this.textBoxExeAddress.Name = "textBoxExeAddress";
            this.textBoxExeAddress.Size = new System.Drawing.Size(64, 20);
            this.textBoxExeAddress.TabIndex = 10;
            // 
            // btnAddExeAddress
            // 
            this.btnAddExeAddress.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnAddExeAddress.Location = new System.Drawing.Point(228, 416);
            this.btnAddExeAddress.Name = "btnAddExeAddress";
            this.btnAddExeAddress.Size = new System.Drawing.Size(208, 23);
            this.btnAddExeAddress.TabIndex = 8;
            this.btnAddExeAddress.Text = "Add Executable Address (Hexadecimal):";
            this.btnAddExeAddress.UseVisualStyleBackColor = true;
            this.btnAddExeAddress.Click += new System.EventHandler(this.btnAddExeAddress_Click);
            // 
            // FormDisAssembler
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.textBoxExeAddress);
            this.Controls.Add(this.btnAddExeAddress);
            this.Controls.Add(this.textBoxProgram);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.textBoxBinary);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormDisAssembler";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "DisAssemblerForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxBinary;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.TextBox textBoxProgram;
        private System.Windows.Forms.TextBox textBoxExeAddress;
        private System.Windows.Forms.Button btnAddExeAddress;
    }
}