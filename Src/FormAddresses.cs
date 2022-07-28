using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _8085
{
    public partial class FormAddresses : Form
    {
        #region Members

        public UInt16 loadAddress;
        public UInt16 startAddress;

        #endregion

        #region Constructor

        public FormAddresses()
        {
            InitializeComponent();

            loadAddress = 0;
            startAddress = 0;
        }

        #endregion

        #region EventHandlers

        private void FormAddresses_Load(object sender, EventArgs e)
        {
            this.textBoxLoadAddress.Text = "0x" + loadAddress.ToString("X4");
            this.textBoxStartAddress.Text = "0x" + startAddress.ToString("X4");
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            bool result;
            if (textBoxLoadAddress.Text.ToLower().Trim().StartsWith("0x"))
            {
                try
                {
                    loadAddress = UInt16.Parse(textBoxLoadAddress.Text.Substring(2), System.Globalization.NumberStyles.HexNumber);
                } catch (Exception)
                {
                    MessageBox.Show("Not a valid number as load address", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            } else if (textBoxLoadAddress.Text.ToUpper().Trim().EndsWith("H"))
            {
                try
                {
                    loadAddress = UInt16.Parse(textBoxLoadAddress.Text.Substring(2), System.Globalization.NumberStyles.HexNumber);
                } catch (Exception)
                {
                    MessageBox.Show("Not a valid number as load address", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            } else
            {
                result = UInt16.TryParse(textBoxLoadAddress.Text, out loadAddress);
                if (!result)
                {
                    MessageBox.Show("Not a valid number as load address", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (textBoxStartAddress.Text.ToLower().Trim().StartsWith("0x"))
            {
                try
                {
                    startAddress = UInt16.Parse(textBoxStartAddress.Text.Substring(2), System.Globalization.NumberStyles.HexNumber);
                } catch (Exception)
                {
                    MessageBox.Show("Not a valid number as start address", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            } else if (textBoxStartAddress.Text.ToUpper().Trim().EndsWith("H"))
            {
                try
                {
                    startAddress = UInt16.Parse(textBoxStartAddress.Text.Substring(2), System.Globalization.NumberStyles.HexNumber);
                } catch (Exception)
                {
                    MessageBox.Show("Not a valid number as start address", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            } else
            {
                result = UInt16.TryParse(textBoxStartAddress.Text, out startAddress);
                if (!result)
                {
                    MessageBox.Show("Not a valid number as start address", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (startAddress < loadAddress)
            {
                MessageBox.Show("The start address can't be in front of the load address", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
        }

        #endregion
    }
}
