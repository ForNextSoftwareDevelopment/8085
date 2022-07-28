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
    public partial class FormDisAssembler : Form
    {
        #region Members

        private DisAssembler85 disAssembler85;
        public UInt16 loadAddress;
        public UInt16 startAddress;
        public int programSize;
        public string program;
        public string lines;

        #endregion

        #region Constructor

        public FormDisAssembler(byte[] bytes, UInt16 loadAddress, UInt16 startAddress)
        {
            InitializeComponent();

            program = "";
            programSize = bytes.Length;

            this.textBoxExeAddress.Text = "0000";

            UInt16 address = loadAddress;
            for (int i=0; i<bytes.Length; i++)
            {
                if ((i % 8) == 0)
                {
                    if (i != 0) textBoxBinary.Text += "\r\n";
                    textBoxBinary.Text += (address + i).ToString("X4") + ": ";
                }

                textBoxBinary.Text += bytes[i].ToString("X2") + " ";
            }

            textBoxBinary.SelectionStart = 0;
            textBoxBinary.SelectionLength = 0;

            disAssembler85 = new DisAssembler85(bytes, loadAddress, startAddress);
            program = disAssembler85.Parse();
            textBoxProgram.Text = disAssembler85.linedprogram;
        }

        #endregion

        #region EventHandlers

        /// <summary>
        /// Add extra address to disassemble
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddExeAddress_Click(object sender, EventArgs e)
        {
            UInt16 exeAddress;

            // Get current position
            int index = textBoxProgram.GetFirstCharIndexOfCurrentLine();

            try
            {
                exeAddress = UInt16.Parse(textBoxExeAddress.Text, System.Globalization.NumberStyles.HexNumber);
            } catch (Exception)
            {
                MessageBox.Show("Not a valid number as address", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            program = disAssembler85.Parse(exeAddress);
            textBoxProgram.Text = disAssembler85.linedprogram;

            // Set newly formed code at the top of textbox
            textBoxProgram.SelectionStart = textBoxProgram.TextLength - 1;
            textBoxProgram.ScrollToCaret();

            textBoxProgram.SelectionStart = index;
            textBoxProgram.SelectionLength = 4;
            textBoxProgram.ScrollToCaret();
            textBoxProgram.Focus();
        }

        /// <summary>
        /// Mouse button clicked in textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>        
        private void textBoxProgram_MouseDown(object sender, MouseEventArgs e)
        {
            // Get character index from start of line at cursor position
            int index = textBoxProgram.GetFirstCharIndexOfCurrentLine();

            // Get address
            string str = textBoxProgram.Text.Substring(index, 4);

            // If valid, put in textbox for adding exe addresses    
            try
            {
                int exeAddress = UInt16.Parse(str, System.Globalization.NumberStyles.HexNumber);
            } catch (Exception)
            {
                return;
            }

            textBoxExeAddress.Text = str;
        }

        #endregion
    }
}
