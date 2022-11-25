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
        byte[] bytes;
        public UInt16 loadAddress;
        public UInt16 startAddress;
        public bool useLabels;
        public int programSize;
        public string program;
        public string lines;

        #endregion

        #region Constructor

        public FormDisAssembler(byte[] bytes, UInt16 loadAddress, UInt16 startAddress, bool useLabels)
        {
            InitializeComponent();

            this.bytes = bytes;
            this.loadAddress = loadAddress;
            this.startAddress = startAddress;
            this.useLabels = useLabels;
        }

        #endregion

        #region EventHandlers

        /// <summary>
        /// Form loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>        
        private void FormDisAssembler_Load(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;

            program = "";
            programSize = bytes.Length;

            this.textBoxExeAddress.Text = "0000";

            UInt16 address = loadAddress;
            for (int i = 0; i < bytes.Length; i++)
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

            disAssembler85 = new DisAssembler85(bytes, loadAddress, startAddress, useLabels);
            program = disAssembler85.Parse();
            richTextBoxProgram.Text = disAssembler85.linedprogram;

            // Highlight warnings in pink
            HighlightProgram();

            Cursor.Current = Cursors.Arrow;
        }

        /// <summary>
        /// Add extra address to disassemble
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddExeAddress_Click(object sender, EventArgs e)
        {
            UInt16 exeAddress;

            // Get current position
            int index = richTextBoxProgram.GetFirstCharIndexOfCurrentLine();

            try
            {
                exeAddress = UInt16.Parse(textBoxExeAddress.Text, System.Globalization.NumberStyles.HexNumber);
            } catch (Exception)
            {
                MessageBox.Show("Not a valid number as address", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            program = disAssembler85.Parse(exeAddress);
            richTextBoxProgram.Text = disAssembler85.linedprogram;

            // Highlight warnings in pink
            HighlightProgram();

            // Set newly formed code at the top of textbox
            richTextBoxProgram.SelectionStart = richTextBoxProgram.TextLength - 1;
            richTextBoxProgram.ScrollToCaret();

            richTextBoxProgram.SelectionStart = index;
            richTextBoxProgram.SelectionLength = 4;
            richTextBoxProgram.ScrollToCaret();
            richTextBoxProgram.Focus();
        }

        /// <summary>
        /// Mouse button clicked in textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>        
        private void richTextBoxProgram_MouseDown(object sender, MouseEventArgs e)
        {
            // Get character index from start of line at cursor position
            int index = richTextBoxProgram.GetFirstCharIndexOfCurrentLine();

            // Get address
            string str = richTextBoxProgram.Text.Substring(index, 4);

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

        #region Methods

        /// <summary>
        /// Highlight warnings in pink
        /// </summary>
        private void HighlightProgram()
        {
            // Make lines red if warning
            for (int i = 0; i < richTextBoxProgram.Lines.Length; i++)
            {
                if (richTextBoxProgram.Lines[i].Contains("Warning"))
                {
                    int firstcharindex = richTextBoxProgram.GetFirstCharIndexFromLine(i);
                    string currentlinetext = richTextBoxProgram.Lines[i];

                    // Select line and color to pink
                    richTextBoxProgram.SelectionStart = firstcharindex;
                    richTextBoxProgram.SelectionLength = currentlinetext.Length;
                    richTextBoxProgram.SelectionBackColor = System.Drawing.Color.LightPink;

                    // Reset selection
                    richTextBoxProgram.SelectionStart = firstcharindex;
                    richTextBoxProgram.SelectionLength = 0;
                }
            }
        }

        #endregion
    }
}
