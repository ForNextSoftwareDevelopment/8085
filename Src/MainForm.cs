using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace _8085
{
    public partial class MainForm : Form
    {
        #region Members

        // Structure to describe the commands for the assembler
        private struct CommandDescription
        {
            public string Instruction;
            public string Operand;
            public string Description;

            public CommandDescription(string instruction = "", string operand = "", string description = "")
            {
                Instruction = instruction;
                Operand = operand;
                Description = description;
            }
            public override string ToString() => Instruction + " " + Operand + "; " + Description;
        }

        // Assembler object
        private Assembler85 assembler85;
        
        // Rows of memory panel   
        private Label[] memoryAddressLabels = new Label[0x10];

        // Columns of memory panel
        private Label[] memoryAddressIndexLabels = new Label[0x10];

        // Contents of memory panel table
        private Label[,] memoryTableLabels = new Label[0x10, 0x10];

        // Rows of ports panel   
        private Label[] portAddressLabels = new Label[0x10];

        // Columns of ports panel
        private Label[] portAddressIndexLabels = new Label[0x10];

        // Contents of ports panel table
        private Label[,] portTableLabels = new Label[0x10, 0x10];

        // File selected for loading/saving 
        private string sourceFile = "";

        // Next instruction address
        private UInt16 nextInstrAddress = 0;

        // Line on which a breakpoint has been set
        private int lineBreakPoint = -1;

        // Tooltip for menu items
        private ToolTip toolTip;

        // Delay for running program
        private Timer timer = new Timer();

        // SDk-85 interface
        private FormSDK_85 formSDK_85 = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            toolStripButtonRun.Enabled = false;
            toolStripButtonStep.Enabled = false;

            pbBreakPoint.Image = new Bitmap(pbBreakPoint.Height, pbBreakPoint.Width);
            Graphics g = pbBreakPoint.CreateGraphics();
            g.Clear(Color.LightGray);

            // Scroll memory panel with mousewheel
            this.panelMemory.MouseWheel += PanelMemory_MouseWheel;
        }

        #endregion

        #region EventHandlers

        /// <summary>
        /// MainForm loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            // Set location of mainform
            this.Location = new Point(20, 20);

            // Tooltip with line (address) info
            toolTip = new ToolTip();
            toolTip.OwnerDraw = true;
            toolTip.IsBalloon = false;
            toolTip.BackColor = Color.Azure;
            toolTip.Draw += ToolTip_Draw;
            toolTip.Popup += ToolTip_Popup;

            // Create font for header text
            Font font = new Font("Tahoma", 9.75F, FontStyle.Bold);

            // We can view 256 bytes of memory at a time, it will be in form of 16 X 16
            for (int i = 0; i < 0x10; i++)
            {
                Label label = new Label();
                label.Name = "memoryAddressLabel" + i.ToString("X");
                label.Font = font;
                label.Text = (i * 16).ToString("X").PadLeft(4, '0');
                label.TextAlign = ContentAlignment.MiddleCenter;
                label.Visible = true;
                label.Size = new System.Drawing.Size(44, 15);
                label.Location = new Point(10, 20 + 20 * i);
                label.BackColor = SystemColors.GradientInactiveCaption;
                panelMemoryInfo.Controls.Add(label);

                memoryAddressLabels[i] = label;
            }

            // MemoryAddressIndexLabels, display the top row required for the memory table
            for (int i = 0; i < 0x10; i++)
            {
                Label label = new Label();
                label.Name = "memoryAddressIndexLabel" + i.ToString("X");
                label.Font = font;
                label.Text = i.ToString("X");
                label.TextAlign = ContentAlignment.MiddleCenter;
                label.Visible = true;
                label.Size = new System.Drawing.Size(20, 15);
                label.Location = new Point(60 + 30 * i, 0);
                label.BackColor = SystemColors.GradientInactiveCaption;
                panelMemoryInfo.Controls.Add(label);

                memoryAddressIndexLabels[i] = label;
            }

            // MemoryTableLabels, display the memory contents
            for (int i = 0; i < 0x10; i++)
            {
                for (int j = 0; j < 0x10; j++)
                {
                    Label label = new Label();
                    int address = 16 * i + j;
                    label.Name = "memoryTableLabel" + address.ToString("X").PadLeft(2, '0');
                    label.Text = null;
                    label.TextAlign = ContentAlignment.MiddleCenter;
                    label.Visible = true;
                    label.Size = new System.Drawing.Size(24, 15);
                    label.Location = new Point(60 + 30 * j, 20 + 20 * i);
                    panelMemoryInfo.Controls.Add(label);

                    memoryTableLabels[i, j] = label;
                }
            }

            // PortAddressLabels, display initial labels from 0x00 to 0x10 
            for (int i = 0; i < 0x10; i++)
            {
                Label label = new Label();
                label.Name = "portAddressLabel" + i.ToString("X");
                label.Font = font;
                label.Text = (i * 16).ToString("X").PadLeft(2, '0');
                label.TextAlign = ContentAlignment.MiddleCenter;
                label.Visible = true;
                label.Size = new System.Drawing.Size(40, 15);
                label.Location = new Point(10, 20 + 20 * i);
                label.BackColor = SystemColors.GradientInactiveCaption;
                panelPortInfo.Controls.Add(label);

                portAddressLabels[i] = label;
            }

            // portAddressIndexLabels, display the top row required for the port table
            for (int i = 0; i < 0x10; i++)
            {
                Label label = new Label();
                label.Name = "portAddressIndexLabel" + i.ToString("X");
                label.Font = font;
                label.Text = (i * 16).ToString("X");
                label.TextAlign = ContentAlignment.MiddleCenter;
                label.Visible = true;
                label.Size = new System.Drawing.Size(20, 15);
                label.Location = new Point(60 + 30 * i, 0);
                label.BackColor = SystemColors.GradientInactiveCaption;
                panelPortInfo.Controls.Add(label);

                portAddressIndexLabels[i] = label;
            }

            // portTableLabels, display the port contents
            for (int i = 0; i < 0x10; i++)
            {
                for (int j = 0; j < 0x10; j++)
                {
                    Label label = new Label();
                    int address = 16 * i + j;
                    label.Name = "portTableLabel" + address.ToString("X");
                    label.Text = null;
                    label.Visible = true;
                    label.Size = new System.Drawing.Size(30, 15);
                    label.Location = new Point(60 + 30 * j, 20 + 20 * i);
                    panelPortInfo.Controls.Add(label);

                    portTableLabels[i, j] = label;
                }
            }

            timer.Interval = Convert.ToInt32(numericUpDownDelay.Value);
            timer.Tick += new EventHandler(TimerEventProcessor);

            // Initialize the buttons (add a tag with info)
            InitButtons();
        }

        /// <summary>
        /// Timer event handler
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void TimerEventProcessor(Object obj, EventArgs myEventArgs)
        {
            Timer timer = (Timer)obj;

            UInt16 currentInstrAddress = nextInstrAddress;

            string error = assembler85.RunInstruction(currentInstrAddress, ref nextInstrAddress);

            UInt16 startViewAddress = Convert.ToUInt16(memoryAddressLabels[0].Text, 16);

            if (!chkLock.Checked)
            {
                if (nextInstrAddress > startViewAddress + 0x100) startViewAddress = (UInt16)(nextInstrAddress & 0xFFF0);
                if (nextInstrAddress < startViewAddress) startViewAddress = (UInt16)(nextInstrAddress & 0xFFF0);
            }

            UpdateMemoryPanel(startViewAddress, nextInstrAddress);
            UpdatePortPanel();
            UpdateRegisters();
            UpdateFlags();
            UpdateInterrupts();
            UpdateDisplay();
            UpdateKeyboard();

            if (error == "")
            {
                ChangeColorRTBLine(assembler85.RAMprogramLine[currentInstrAddress], false);

                if (assembler85.RAMprogramLine[nextInstrAddress] == lineBreakPoint)
                {
                    timer.Enabled = false;

                    // Enable event handler for updating row/column 
                    richTextBoxProgram.SelectionChanged += new EventHandler(richTextBoxProgram_SelectionChanged);

                    ChangeColorRTBLine(assembler85.RAMprogramLine[nextInstrAddress], false);
                    if (chkLock.Checked)
                    {
                        UpdateMemoryPanel(GetTextBoxMemoryStartAddress(), nextInstrAddress);
                    } else
                    {
                        UpdateMemoryPanel(currentInstrAddress, nextInstrAddress);
                    }
                    UpdatePortPanel();
                    UpdateRegisters();
                    UpdateFlags();
                    UpdateInterrupts();

                    toolStripButtonRun.Enabled = true;
                    toolStripButtonStep.Enabled = true;
                }
            } else
            {
                timer.Enabled = false;

                toolStripButtonRun.Enabled = false;
                toolStripButtonStep.Enabled = false;

                // Enable event handler for updating row/column 
                richTextBoxProgram.SelectionChanged += new EventHandler(richTextBoxProgram_SelectionChanged);

                ChangeColorRTBLine(assembler85.RAMprogramLine[currentInstrAddress], true);
                MessageBox.Show(error, "RUNTIME ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            Application.DoEvents();
        }

        /// <summary>
        /// Show/Hide SDK-85 interface
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkSDK85_CheckedChanged(object sender, EventArgs e)
        {
            if (formSDK_85 == null)
            {
                int x = this.Location.X + this.Width + 10;
                int y = this.Location.Y;

                if (x > Screen.PrimaryScreen.WorkingArea.Width)
                {
                    x = this.Width / 2;
                    y = this.Height / 2;
                }

                formSDK_85 = new FormSDK_85(x, y);
                formSDK_85.Show();
            } else
            {
                formSDK_85.Close();
                formSDK_85 = null;
            }
        }

        /// <summary>
        /// Memory startaddress changing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbMemoryStartAddress_TextChanged(object sender, EventArgs e)
        {
            string hexdigits = "1234567890ABCDEFabcdef";
            bool noHex = false;
            foreach (char c in tbMemoryStartAddress.Text)
            {
                if (hexdigits.IndexOf(c) < 0)
                {
                    MessageBox.Show("Only hexadecimal values", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    noHex = true;
                }
            }

            if (noHex) tbMemoryStartAddress.Text = "0000";
        }

        /// <summary>
        /// View memory from this address
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbMemoryStartAddress_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                UpdateMemoryPanel(GetTextBoxMemoryStartAddress(), nextInstrAddress);
            }
        }

        /// <summary>
        /// Startaddress changing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbStartAddress_TextChanged(object sender, EventArgs e)
        {
            string hexdigits = "1234567890ABCDEFabcdef";
            bool noHex = false;
            foreach (char c in tbStartAddress.Text)
            {
                if (hexdigits.IndexOf(c) < 0)
                {
                    MessageBox.Show("Only hexadecimal values", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    noHex = true;
                }
            }

            if (noHex) tbStartAddress.Text = "0000";
        }

        /// <summary>
        /// Startaddress changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbStartAddress_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                nextInstrAddress = Convert.ToUInt16(tbStartAddress.Text, 16);
                ChangeColorRTBLine(assembler85.RAMprogramLine[nextInstrAddress], false);
            }
        }

        /// <summary>
        /// Timer delay while running
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void numericUpDownDelay_ValueChanged(object sender, EventArgs e)
        {
            timer.Interval = Convert.ToInt32(numericUpDownDelay.Value);
        }

        /// <summary>
        /// Add/Change breakpoint
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>        
        private void pbBreakPoint_MouseClick(object sender, MouseEventArgs e)
        {
            // Get character index of mouse Y position in current program
            int index = richTextBoxProgram.GetCharIndexFromPosition(new Point(0, e.Y));

            // Get line number
            lineBreakPoint = richTextBoxProgram.GetLineFromCharIndex(index);

            UpdateBreakPoint();
        }

        /// <summary>
        /// Draw tooltip with specific font
        /// </summary>
        private void ToolTip_Draw(object sender, DrawToolTipEventArgs e)
        {
            Font font = new Font(FontFamily.GenericMonospace, 12.0f);
            e.DrawBackground();
            e.DrawBorder();
            e.Graphics.DrawString(e.ToolTipText, font, Brushes.Black, new Point(2, 2));
        }

        /// <summary>
        /// Set font for the tooltip popup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolTip_Popup(object sender, PopupEventArgs e)
        {
            Font font = new Font(FontFamily.GenericMonospace, 12.0f);
            Size size = TextRenderer.MeasureText(toolTip.GetToolTip(e.AssociatedControl), font);
            e.ToolTipSize = new Size(size.Width + 3, size.Height + 3);
        }

        /// <summary>
        /// Change memory view range
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PanelMemory_MouseWheel(object sender, MouseEventArgs e)
        {
            if (assembler85 != null)
            {
                if (e.Delta < 0)
                {
                    if (Convert.ToUInt16(memoryAddressLabels[0].Text, 16) < 0xFFF0)
                    {
                        UInt16 n = (UInt16)(Convert.ToUInt16(memoryAddressLabels[0].Text, 16) + 0x0010);

                        tbMemoryStartAddress.Text = n.ToString("X");
                        UpdateMemoryPanel(n, nextInstrAddress);
                    }
                }

                if (e.Delta > 0)
                {
                    if (Convert.ToUInt16(memoryAddressLabels[0].Text, 16) >= 0x0010)
                    {
                        UInt16 n = (UInt16)(Convert.ToUInt16(memoryAddressLabels[0].Text, 16) - 0x0010);

                        tbMemoryStartAddress.Text = n.ToString("X");
                        UpdateMemoryPanel(n, nextInstrAddress);
                    }
                }
            }
        }

        /// <summary>
        /// Change sign flag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkFlagS_CheckedChanged(object sender, EventArgs e)
        {
            if (assembler85 != null) assembler85.flagS = chkFlagS.Checked;
        }

        /// <summary>
        /// Change zero flag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkFlagZ_CheckedChanged(object sender, EventArgs e)
        {
            if (assembler85 != null) assembler85.flagZ = chkFlagZ.Checked;
        }

        /// <summary>
        /// Change auxiliary carry flag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkFlagAC_CheckedChanged(object sender, EventArgs e)
        {
            if (assembler85 != null) assembler85.flagAC = chkFlagAC.Checked;
        }

        /// <summary>
        /// Change parity flag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkFlagP_CheckedChanged(object sender, EventArgs e)
        {
            if (assembler85 != null) assembler85.flagP = chkFlagP.Checked;
        }

        /// <summary>
        /// Change carry flag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkFlagC_CheckedChanged(object sender, EventArgs e)
        {
            if (assembler85 != null) assembler85.flagC = chkFlagC.Checked;
        }

        #endregion

        #region EventHandlers (Menu)

        private void open_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "Select Assembly File";
            fileDialog.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            fileDialog.FileName = "";
            fileDialog.Filter = "8085 assembly|*.asm;*.a80;*.a85|All Files|*.*";

            if (fileDialog.ShowDialog() != DialogResult.Cancel)
            {
                sourceFile = fileDialog.FileName;
                System.IO.StreamReader asmProgramReader;
                asmProgramReader = new System.IO.StreamReader(sourceFile);
                richTextBoxProgram.Text = asmProgramReader.ReadToEnd();
                asmProgramReader.Close();
            }
        }

        private void save_Click(object sender, EventArgs e)
        {
            if (sourceFile == "")
            {
                OpenFileDialog fileDialog = new OpenFileDialog();
                fileDialog.Title = "Save File As";
                fileDialog.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                fileDialog.FileName = "";
                fileDialog.Filter = "8085 assembly|*.asm;*.a80;*.a85|All Files|*.*";

                if (fileDialog.ShowDialog() != DialogResult.Cancel)
                {
                    sourceFile = fileDialog.FileName;
                    System.IO.StreamWriter asmProgramWriter;
                    asmProgramWriter = new System.IO.StreamWriter(sourceFile);
                    asmProgramWriter.Write(richTextBoxProgram.Text);
                    asmProgramWriter.Close();
                }
            } else
            {
                System.IO.StreamWriter asmProgramWriter;
                asmProgramWriter = new System.IO.StreamWriter(sourceFile);
                asmProgramWriter.Write(richTextBoxProgram.Text);
                asmProgramWriter.Close();
            }
        }

        private void saveAs_Click(object sender, EventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Title = "Save File As";
            fileDialog.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            fileDialog.FileName = "";
            fileDialog.Filter = "8085 assembly|*.asm|All Files|*.*";

            if (fileDialog.ShowDialog() != DialogResult.Cancel)
            {
                sourceFile = fileDialog.FileName;
                System.IO.StreamWriter asmProgramWriter;
                asmProgramWriter = new System.IO.StreamWriter(sourceFile);
                asmProgramWriter.Write(richTextBoxProgram.Text);
                asmProgramWriter.Close();
            }
        }

        private void saveBinary_Click(object sender, EventArgs e)
        {
            if (assembler85.programRun == null)
            {
                MessageBox.Show("Nothing yet to save", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Title = "Save Binary File As";
            fileDialog.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            fileDialog.FileName = "";
            fileDialog.Filter = "Binary|*.bin|All Files|*.*";

            if (fileDialog.ShowDialog() != DialogResult.Cancel)
            {
                int start = -1;
                int end = -1;

                // Find start address of code
                for (int i = 0; i < assembler85.RAM.Length; i++)
                {
                    if ((assembler85.RAM[i] != 0) && (start == -1)) start = i;
                }

                // Find end address of code
                for (int i = assembler85.RAM.Length - 1; i >= 0; i--)
                {
                    if ((assembler85.RAM[i] != 0) && (end == -1)) end = i;
                }

                if ((start == -1) || (end == -1))
                {
                    MessageBox.Show("Nothing to save", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // New byte array with only used code 
                byte[] bytes = new byte[end - start + 1];
                for (int i = 0; i < end - start + 1; i++)
                {
                    bytes[i] = assembler85.RAM[start + i];
                }

                // Save binary file
                File.WriteAllBytes(fileDialog.FileName, bytes);

                MessageBox.Show("Binary file saved as\r\n" + fileDialog.FileName, "SAVED", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void openBinary_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "Select Binary File";
            fileDialog.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            fileDialog.FileName = "";
            fileDialog.Filter = "8085 binary|*.bin|All Files|*.*";

            if (fileDialog.ShowDialog() != DialogResult.Cancel)
            {
                sourceFile = fileDialog.FileName;
                byte[] bytes = File.ReadAllBytes(sourceFile);

                FormAddresses formAddresses = new FormAddresses();
                formAddresses.ShowDialog();

                FormDisAssembler disAssemblerForm = new FormDisAssembler(bytes, formAddresses.loadAddress, formAddresses.startAddress);
                DialogResult dialogResult = disAssemblerForm.ShowDialog();
                if (dialogResult == DialogResult.OK)
                {
                    richTextBoxProgram.Text = disAssemblerForm.program;
                }
            }
        }

        private void quit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void resetSimulator_Click(object sender, EventArgs e)
        {
            if (timer.Enabled)
            {
                timer.Enabled = false;

                // Enable event handler for updating row/column 
                richTextBoxProgram.SelectionChanged += new EventHandler(richTextBoxProgram_SelectionChanged);
            }

            assembler85 = null;
            UpdateMemoryPanel(0x0000, 0x0000);
            UpdatePortPanel();
            UpdateRegisters();
            UpdateFlags();
            UpdateInterrupts();
            ClearDisplay();

            // Reset color
            richTextBoxProgram.SelectionStart = 0;
            richTextBoxProgram.SelectionLength = richTextBoxProgram.Text.Length;
            richTextBoxProgram.SelectionBackColor = System.Drawing.Color.White;

            tbStartAddress.Text = "0000";
            tbMemoryStartAddress.Text = "0000";
            tbMemoryUpdateByte.Text = "00";
            numMemoryAddress.Value = 0000;
            numPort.Value = 0;
            tbPortUpdateByte.Text = "00";

            toolStripButtonRun.Enabled = false;
            toolStripButtonStep.Enabled = false;

            lineBreakPoint = -1;

            Graphics g = pbBreakPoint.CreateGraphics();
            g.Clear(Color.LightGray);
        }

        private void resetRAM_Click(object sender, EventArgs e)
        {
            assembler85.ClearRam();
            nextInstrAddress = Convert.ToUInt16(tbMemoryStartAddress.Text, 16);
            UpdateMemoryPanel(GetTextBoxMemoryStartAddress(), nextInstrAddress);
        }

        private void resetPorts_Click(object sender, EventArgs e)
        {
            assembler85.ClearPorts();
            UpdatePortPanel();
        }

        private void new_Click(object sender, EventArgs e)
        {
            assembler85 = null;
            UpdateMemoryPanel(0x0000, 0x0000);
            UpdatePortPanel();
            UpdateRegisters();
            UpdateFlags();
            UpdateInterrupts();
            ClearDisplay();

            richTextBoxProgram.Clear();
            sourceFile = "";

            tbStartAddress.Text = "0000";
            tbMemoryStartAddress.Text = "0000";
            tbMemoryUpdateByte.Text = "00";
            numMemoryAddress.Value = 0000;
            numPort.Value = 0;
            tbPortUpdateByte.Text = "00";

            toolStripButtonRun.Enabled = false;
            toolStripButtonStep.Enabled = false;

            lineBreakPoint = -1;

            Graphics g = pbBreakPoint.CreateGraphics();
            g.Clear(Color.LightGray);
        }

        private void startDebug_Click(object sender, EventArgs e)
        {
            assembler85 = new Assembler85(richTextBoxProgram.Lines);
            nextInstrAddress = 0;
            try
            {
                // Run the first Pass of assembler
                string message = assembler85.FirstPass();
                if (message != "OK")
                {
                    MessageBox.Show(this, message, "FIRSTPASS", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // Check if a linenumber has been given
                    string[] fields = message.Split(' ');
                    bool result = Int32.TryParse(fields[fields.Length - 1], out int line);
                    if (result)
                    {
                        // Show where the error is (remember the linenumber returned starts with 1 in stead of 0)
                        ChangeColorRTBLine(line - 1, true);
                    }

                    return;
                }

                // Run second pass
                message = assembler85.SecondPass();
                if (message != "OK")
                {
                    MessageBox.Show(this, message, "SECONDPASS", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // Check if a linenumber has been given
                    string[] fields = message.Split(' ');
                    bool result = Int32.TryParse(fields[fields.Length - 1], out int line);
                    if (result)
                    {
                        // Show where the error is (remember the linenumber returned starts with 1 in stead of 0)
                        ChangeColorRTBLine(line - 1, true);
                    }

                    return;
                }

                // Try to get startadres of program execution
                int startline = -1;
                for (int index = 0; (index < assembler85.RAMprogramLine.Length) && (startline == -1); index++)
                {
                    if (assembler85.RAMprogramLine[index] != -1) startline = index;
                }

                if (startline != -1)
                {
                    tbMemoryStartAddress.Text = startline.ToString("X");
                    tbStartAddress.Text = startline.ToString("X");
                }

                // Show Updated memory
                UpdateMemoryPanel(GetTextBoxMemoryStartAddress(), nextInstrAddress);

            } catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "startDebug_Click", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            nextInstrAddress = Convert.ToUInt16(tbStartAddress.Text, 16);
            ChangeColorRTBLine(assembler85.RAMprogramLine[nextInstrAddress], false);

            UpdateMemoryPanel(GetTextBoxMemoryStartAddress(), nextInstrAddress);
            UpdatePortPanel();
            UpdateRegisters();
            UpdateFlags();
            ClearDisplay();

            toolStripButtonRun.Enabled = true;
            toolStripButtonStep.Enabled = true;
        }

        private void startRun_Click(object sender, EventArgs e)
        {
            toolStripButtonRun.Enabled = false;
            toolStripButtonStep.Enabled = false;

            // Disable event handler for updating row/column 
            richTextBoxProgram.SelectionChanged -= richTextBoxProgram_SelectionChanged;

            timer.Interval = Convert.ToInt32(numericUpDownDelay.Value);
            timer.Enabled = true;
        }

        private void startStep_Click(object sender, EventArgs e)
        {
            UInt16 currentInstrAddress = nextInstrAddress;
            string error = assembler85.RunInstruction(currentInstrAddress, ref nextInstrAddress);

            UInt16 startViewAddress = Convert.ToUInt16(memoryAddressLabels[0].Text, 16);

            if (!chkLock.Checked)
            {
                if (nextInstrAddress > startViewAddress + 0x100) startViewAddress = (UInt16)(nextInstrAddress & 0xFFF0);
                if (nextInstrAddress < startViewAddress - 0x100) startViewAddress = (UInt16)(nextInstrAddress & 0xFFF0);
            }

            UpdateMemoryPanel(startViewAddress, nextInstrAddress);
            UpdatePortPanel();
            UpdateRegisters();
            UpdateFlags();
            UpdateDisplay();
            UpdateKeyboard();
            UpdateInterrupts();

            if (error == "")
            {
                ChangeColorRTBLine(assembler85.RAMprogramLine[nextInstrAddress], false);
            } else
            {
                ChangeColorRTBLine(assembler85.RAMprogramLine[currentInstrAddress], true);
                MessageBox.Show(error, "RUNTIME ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // Get index of cursor in current program
            int index = richTextBoxProgram.SelectionStart;

            // Get line number
            int line = richTextBoxProgram.GetLineFromCharIndex(index);
            lblLine.Text = (line + 1).ToString();

            int column = richTextBoxProgram.SelectionStart - richTextBoxProgram.GetFirstCharIndexFromLine(line);
            lblColumn.Text = (column + 1).ToString();
        }

        private void stop_Click(object sender, EventArgs e)
        {
            if (assembler85 != null)
            {
                timer.Enabled = false;

                // Enable event handler for updating row/column 
                richTextBoxProgram.SelectionChanged += new EventHandler(richTextBoxProgram_SelectionChanged);

                ChangeColorRTBLine(assembler85.RAMprogramLine[nextInstrAddress], false);

                toolStripButtonRun.Enabled = true;
                toolStripButtonStep.Enabled = true;
            }
        }

        private void viewHelp_Click(object sender, EventArgs e)
        {
            FormHelp formHelp = new FormHelp();
            formHelp.ShowDialog();
        }

        private void about_Click(object sender, EventArgs e)
        {
            FormAbout formAbout = new FormAbout();
            formAbout.ShowDialog();
        }

        #endregion

        #region EventHandlers (Labels)

        private void labelARegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelARegister);
        }

        private void labelBRegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelBRegister);
        }

        private void labelCRegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelCRegister);
        }

        private void labelDRegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelDRegister);
        }

        private void labelERegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelERegister);
        }

        private void labelHRegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelHRegister);
        }

        private void labelLRegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelLRegister);
        }

        private void labelPCRegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelPCRegister);
        }

        private void labelSPRegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelSPRegister);
        }

        #endregion

        #region EventHandlers (Buttons)

        /// <summary>
        /// Command buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCommand_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            if (button.Tag != null)
            {
                CommandDescription commandDescription = (CommandDescription)button.Tag;

                string command = "";
                command = Interaction.InputBox(commandDescription.Description, commandDescription.Instruction, commandDescription.Instruction + " " + commandDescription.Operand);
                if (command != "")
                {
                    if (richTextBoxProgram.SelectionStart == 0)
                    {
                        richTextBoxProgram.AppendText(command);
                    } else
                    {
                        richTextBoxProgram.AppendText(Environment.NewLine + command);
                    }
                }
            }
        }

        /// <summary>
        /// View symbol table
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnViewSymbolTable_Click(object sender, EventArgs e)
        {
            if ((assembler85 != null) && (assembler85.programRun != null))
            {
                string addressSymbolTable = "";

                // Check max length of labels
                int maxLabelSize = 0;
                foreach (KeyValuePair<string, int> keyValuePair in assembler85.addressSymbolTable)
                {
                    if (keyValuePair.Key.Length > maxLabelSize) maxLabelSize = keyValuePair.Key.Length;
                }

                // Add to table
                foreach (KeyValuePair<string, int> keyValuePair in assembler85.addressSymbolTable)
                {
                    addressSymbolTable += keyValuePair.Key;
                    for (int i=keyValuePair.Key.Length; i< maxLabelSize + 1; i++)
                    {
                        addressSymbolTable += " ";
                    }

                    addressSymbolTable += ": " + keyValuePair.Value.ToString("X4") + "\r\n";
                }

                // Create form for display of results                
                Form addressSymbolTableForm = new Form();
                addressSymbolTableForm.Name = "FormSymbolTable";
                addressSymbolTableForm.Text = "SymbolTable";
                addressSymbolTableForm.ShowIcon = false;
                addressSymbolTableForm.Size = new Size(300, 600);
                addressSymbolTableForm.MinimumSize = new Size(300, 600);
                addressSymbolTableForm.MaximumSize = new Size(300, 600);
                addressSymbolTableForm.MaximizeBox = false;
                addressSymbolTableForm.MinimizeBox = false;
                addressSymbolTableForm.StartPosition = FormStartPosition.CenterScreen;

                // Create button for closing (dialog)form
                Button btnOk = new Button();
                btnOk.Text = "OK";
                btnOk.Location = new Point(204, 530);
                btnOk.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
                btnOk.Visible = true;
                btnOk.Click += new EventHandler((object o, EventArgs a) =>
                {
                    addressSymbolTableForm.Close();
                });

                Font font = new Font(FontFamily.GenericMonospace, 10.25F);

                // Sort alphabetically
                string[] tempArray = addressSymbolTable.Split('\n');
                Array.Sort(tempArray, StringComparer.InvariantCulture);
                addressSymbolTable = "";
                foreach (string line in tempArray)
                {
                    addressSymbolTable += line + '\n';
                }

                // Add controls to form
                TextBox textBox = new TextBox();
                textBox.Multiline = true;
                textBox.WordWrap = false;
                textBox.ScrollBars = ScrollBars.Vertical;
                textBox.ReadOnly = true;
                textBox.BackColor = Color.LightYellow;
                textBox.Size = new Size(268, 510);
                textBox.Text = addressSymbolTable;
                textBox.Font = font;
                textBox.BorderStyle = BorderStyle.None;
                textBox.Location = new Point(10, 10);
                textBox.Select(0, 0);

                addressSymbolTableForm.Controls.Add(textBox);
                addressSymbolTableForm.Controls.Add(btnOk);

                // Show form
                addressSymbolTableForm.Show();
            }
        }

        /// <summary>
        /// View program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnViewProgram_Click(object sender, EventArgs e)
        {
            if ((assembler85 != null) && (assembler85.programRun != null))
            {
                // Create form for display of results                
                Form formProgram = new Form();
                formProgram.Name = "FormProgram";
                formProgram.Text = "Program";
                formProgram.ShowIcon = false;
                formProgram.Size = new Size(500, 600);
                formProgram.MinimumSize = new Size(500, 600);
                formProgram.MaximizeBox = false;
                formProgram.MinimizeBox = false;
                formProgram.StartPosition = FormStartPosition.CenterScreen;

                // Create button for closing (dialog)form
                Button btnOk = new Button();
                btnOk.Text = "OK";
                btnOk.Location = new Point(400, 530);
                btnOk.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
                btnOk.Visible = true;
                btnOk.Click += new EventHandler((object o, EventArgs a) =>
                {
                    formProgram.Close();
                });

                string program = "";
                foreach (string line in assembler85.programView)
                {
                    if ((line != null) && (line != "") && (line != "\r") && (line != "\n") && (line != "\r\n")) program += line + "\r\n";
                }

                Font font = new Font(FontFamily.GenericMonospace, 10.25F);

                // Add controls to form
                TextBox textBox = new TextBox();
                textBox.Multiline = true;
                textBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                textBox.WordWrap = false;
                textBox.ScrollBars = ScrollBars.Vertical;
                textBox.ReadOnly = true;
                textBox.BackColor = Color.LightYellow;
                textBox.Size = new Size(464, 510);
                textBox.Text = program;
                textBox.Font = font;
                textBox.BorderStyle = BorderStyle.None;
                textBox.Location = new Point(10, 10);
                textBox.Select(0, 0);

                formProgram.Controls.Add(textBox);
                formProgram.Controls.Add(btnOk);

                // Show form
                formProgram.Show();
            }
        }

        /// <summary>
        /// Set memory start address to view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMemoryStartAddress_Click(object sender, EventArgs e)
        {
            UpdateMemoryPanel(GetTextBoxMemoryStartAddress(), nextInstrAddress);
        }

        /// <summary>
        /// Set memory start address to view to Program Counter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnViewPC_Click(object sender, EventArgs e)
        {
            if (assembler85 != null)
            {
                UpdateMemoryPanel(assembler85.registerPC, nextInstrAddress);
            }
        }

        /// <summary>
        /// Set memory start address to view to Stack Pointer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnViewSP_Click(object sender, EventArgs e)
        {
            if (assembler85 != null)
            {
                UpdateMemoryPanel(assembler85.registerSP, nextInstrAddress);
            }
        }

        /// <summary>
        /// Previous memory to view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPrevPage_Click(object sender, EventArgs e)
        {
            if (assembler85 != null)
            {
                if (Convert.ToUInt16(memoryAddressLabels[0].Text, 16) >= 0x0100)
                {
                    UInt16 n = (UInt16)(Convert.ToUInt16(memoryAddressLabels[0].Text, 16) - 0x0100);

                    tbMemoryStartAddress.Text = n.ToString("X");
                    UpdateMemoryPanel(n, nextInstrAddress);
                }
            }
        }

        /// <summary>
        /// Next memory to view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNextPage_Click(object sender, EventArgs e)
        {
            if (assembler85 != null)
            {
                if (Convert.ToUInt16(memoryAddressLabels[0].Text, 16) < 0xFF00)
                {
                    UInt16 n = (UInt16)(Convert.ToUInt16(memoryAddressLabels[0].Text, 16) + 0x0100);

                    tbMemoryStartAddress.Text = n.ToString("X");
                    UpdateMemoryPanel(n, nextInstrAddress);
                }
            }
        }

        /// <summary>
        /// Write value to memory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMemoryWrite_Click(object sender, EventArgs e)
        {
            if (assembler85 != null)
            {
                assembler85.RAM[(int)numMemoryAddress.Value] = Convert.ToByte(tbMemoryUpdateByte.Text, 16);

                // Convert memoryaddress(hex) to int, convert string from textbox to BYTE
                if (
                    (((int)numMemoryAddress.Value) >= GetTextBoxMemoryStartAddress()) &&
                    (((int)numMemoryAddress.Value) <= GetTextBoxMemoryStartAddress() + 0x7F)
                   )
                {
                    nextInstrAddress = Convert.ToUInt16(tbMemoryStartAddress.Text, 16);
                    UpdateMemoryPanel(GetTextBoxMemoryStartAddress(), nextInstrAddress);
                }
            }
        }

        /// <summary>
        /// Clear all ports
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClearPORT_Click(object sender, EventArgs e)
        {
            if (assembler85 != null)
            {
                assembler85.ClearPorts();
                UpdatePortPanel();
            }
        }

        /// <summary>
        ///  Write value to port
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPortWrite_Click(object sender, EventArgs e)
        {
            if (assembler85 != null)
            {
                assembler85.PORT[(int)numPort.Value] = Convert.ToByte(tbPortUpdateByte.Text, 16);

                UpdatePortPanel();
            }
        }

        /// <summary>
        /// Clear breakpoint
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClearBreakPoints_Click(object sender, EventArgs e)
        {
            lineBreakPoint = -1;

            Graphics g = pbBreakPoint.CreateGraphics();
            g.Clear(Color.LightGray);
        }

        #endregion

        #region EventHandlers (RichTextBox)

        private void richTextBoxProgram_SelectionChanged(object sender, EventArgs e)
        {
            // Get index of cursor in current program
            int index = richTextBoxProgram.SelectionStart;

            // Get line number
            int line = richTextBoxProgram.GetLineFromCharIndex(index);
            lblLine.Text = (line + 1).ToString();

            int column = richTextBoxProgram.SelectionStart - richTextBoxProgram.GetFirstCharIndexFromLine(line);
            lblColumn.Text = (column + 1).ToString();
        }

        // Program adjusted, remove highlight
        private void richTextBoxProgram_TextChanged(object sender, EventArgs e)
        {
            if (toolStripButtonRun.Enabled)
            {
                int pos = richTextBoxProgram.SelectionStart;

                // Reset color
                richTextBoxProgram.SelectionStart = 0;
                richTextBoxProgram.SelectionLength = richTextBoxProgram.Text.Length;
                richTextBoxProgram.SelectionBackColor = System.Drawing.Color.White;

                richTextBoxProgram.SelectionLength = 0;

                richTextBoxProgram.SelectionStart = pos;

                toolStripButtonRun.Enabled = false;
                toolStripButtonStep.Enabled = false;
            }

            lineBreakPoint = -1;

            Graphics g = pbBreakPoint.CreateGraphics();
            g.Clear(Color.LightGray);
        }

        /// <summary>
        /// Mouse button clicked in control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void richTextBoxProgram_MouseDown(object sender, MouseEventArgs e)
        {
            int x = e.Location.X;
            int y = e.Location.Y;

            int charIndex = richTextBoxProgram.GetCharIndexFromPosition(new Point(x, y));
            int lineIndex = richTextBoxProgram.GetLineFromCharIndex(charIndex);

            if (assembler85 != null)
            {
                bool found = false;
                for (int address = 0; (address < assembler85.RAMprogramLine.Length) && !found; address++)
                {
                    if (assembler85.RAMprogramLine[address] == lineIndex)
                    {
                        found = true;
                        int startAddress = GetTextBoxMemoryStartAddress();

                        int row = (address - startAddress) / 16;
                        int col = (address - startAddress) % 16;

                        foreach (Label lbl in memoryTableLabels)
                        {
                            if (lbl.BackColor != Color.LightGreen) lbl.BackColor = SystemColors.Info;
                        }

                        if ((row >= 0) && (col >= 0) && (row <= 16) && (col <= 16))
                        {
                            if (memoryTableLabels[row, col].BackColor != Color.LightGreen) memoryTableLabels[row, col].BackColor = SystemColors.GradientInactiveCaption;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Mouse enters control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void richTextBoxProgram_MouseEnter(object sender, EventArgs e)
        {
            toolTip.Active = true;
        }

        /// <summary>
        /// Mouse leaves control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void richTextBoxProgram_MouseLeave(object sender, EventArgs e)
        {
            toolTip.Hide(richTextBoxProgram);
            toolTip.Active = false;
        }

        /// <summary>
        /// Disable tooltip
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>        
        private void richTextBoxProgram_MouseMove(object sender, MouseEventArgs e)
        {
            if ((toolTip != null) && (assembler85 != null))
            {
                int x = e.Location.X;
                int y = e.Location.Y;

                int charIndex = richTextBoxProgram.GetCharIndexFromPosition(new Point(x, y));
                int lineIndex = richTextBoxProgram.GetLineFromCharIndex(charIndex);

                bool found = false;
                for (int index = 0; (index < assembler85.RAMprogramLine.Length) && !found; index++)
                {
                    if (assembler85.RAMprogramLine[index] == lineIndex)
                    {
                        found = true;
                        if (toolTip.GetToolTip(richTextBoxProgram) != index.ToString("X4")) 
                        {
                            toolTip.Show(index.ToString("X4"), richTextBoxProgram, -50, richTextBoxProgram.GetPositionFromCharIndex(charIndex).Y, 50000);
                        } 
                    }
                }
            }
        }

        private void richTextBoxProgram_SizeChanged(object sender, EventArgs e)
        {
            UpdateBreakPoint();
        }

        private void richTextBoxProgram_VScroll(object sender, EventArgs e)
        {
            UpdateBreakPoint();
            toolTip.Hide(richTextBoxProgram);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Updating the Registers
        /// </summary>
        private void UpdateRegisters()
        {
            if (assembler85 != null)
            {
                labelARegister.Text = assembler85.registerA.ToString("X").PadLeft(2, '0');
                labelBRegister.Text = assembler85.registerB.ToString("X").PadLeft(2, '0');
                labelCRegister.Text = assembler85.registerC.ToString("X").PadLeft(2, '0');
                labelDRegister.Text = assembler85.registerD.ToString("X").PadLeft(2, '0');
                labelERegister.Text = assembler85.registerE.ToString("X").PadLeft(2, '0');
                labelHRegister.Text = assembler85.registerH.ToString("X").PadLeft(2, '0');
                labelLRegister.Text = assembler85.registerL.ToString("X").PadLeft(2, '0');

                labelPCRegister.Text = assembler85.registerPC.ToString("X").PadLeft(4, '0');
                labelSPRegister.Text = assembler85.registerSP.ToString("X").PadLeft(4, '0');
            } else
            {
                labelARegister.Text = "00";
                labelBRegister.Text = "00";
                labelCRegister.Text = "00";
                labelDRegister.Text = "00";
                labelERegister.Text = "00";
                labelHRegister.Text = "00";
                labelLRegister.Text = "00";

                labelPCRegister.Text = "0000";
                labelSPRegister.Text = "FFFF";
            }
        }

        /// <summary>
        /// Update the Flags
        /// </summary>
        private void UpdateFlags()
        {
            if (assembler85 != null)
            {
                chkFlagC.Checked = assembler85.flagC;
                chkFlagV.Checked = assembler85.flagV;
                chkFlagAC.Checked = assembler85.flagAC;
                chkFlagP.Checked = assembler85.flagP;
                chkFlagK.Checked = assembler85.flagK;
                chkFlagZ.Checked = assembler85.flagZ;
                chkFlagS.Checked = assembler85.flagS;
            } else
            {
                chkFlagC.Checked = false;
                chkFlagV.Checked = false;
                chkFlagAC.Checked = false;
                chkFlagP.Checked = false;
                chkFlagK.Checked = false;
                chkFlagZ.Checked = false;
                chkFlagS.Checked = false;
            }
        }

        /// <summary>
        /// Update interrupt masks
        /// </summary>
        private void UpdateInterrupts()
        {
            if (assembler85 != null)
            {
                if (assembler85.intrIE)
                {
                    lblInterrupts.BackColor = Color.LightGreen;
                    chkIE.Checked = true;
                } else
                {
                    lblInterrupts.BackColor = Color.LightPink;
                    chkIE.Checked = false;
                }

                chkM55.Checked = assembler85.intrM55;
                chkM65.Checked = assembler85.intrM65;
                chkM75.Checked = assembler85.intrM75;
                chkP55.Checked = assembler85.intrP55;
                chkP65.Checked = assembler85.intrP65;
                chkP75.Checked = assembler85.intrP75;
            } else
            {
                chkM55.Checked = false;
                chkM65.Checked = false;
                chkM75.Checked = false;
                chkIE.Checked = false;
                chkP55.Checked = false;
                chkP65.Checked = false;
                chkP75.Checked = false;
            }
        }

        /// <summary>
        /// Draw memory panel starting from address startAddress, show nextAddress in green
        /// </summary>
        /// <param name="startAddress"></param>
        /// <param name="nextAddress"></param>
        private void UpdateMemoryPanel(UInt16 startAddress, UInt16 nextAddress)
        {
            if (assembler85 != null)
            {
                // Boundary at address XXX0
                startAddress = (UInt16)(startAddress & 0xFFF0);

                // Check for overflow in displayig (startaddress + 0xFF larger then 0xFFFF)
                if (startAddress > 0xFF00) startAddress = 0xFF00;

                int i = startAddress;
                int j = 0;

                foreach (Label lbl in memoryAddressLabels)
                {
                    lbl.Text = i.ToString("X").PadLeft(4, '0');
                    i += 0x10;
                }

                i = 0;
                j = 0;

                // MemoryTableLabels, display the memory contents
                foreach (Label lbl in memoryTableLabels)
                {
                    int address = startAddress + (16 * i) + j;
                    lbl.Text = assembler85.RAM[address].ToString("X").PadLeft(2, '0');

                    if (address == nextAddress)
                    {
                        lbl.BackColor = Color.LightGreen;
                    } else
                    if (address == assembler85.registerSP)
                    {
                        lbl.BackColor = Color.LightPink;
                    } else
                    {
                        lbl.BackColor = SystemColors.Info;
                    }

                    j++;
                    if (j == 0x10)
                    {
                        j = 0;
                        i++;
                    }
                }
            } else
            {
                int i = 0;

                foreach (Label lbl in memoryAddressLabels)
                {
                    lbl.Text = i.ToString("X").PadLeft(4, '0');
                    i += 0x10;
                }

                // MemoryTableLabels, display 00
                foreach (Label lbl in memoryTableLabels)
                {
                    lbl.Text = "00";
                    lbl.BackColor = SystemColors.Info;
                }
            }
        }

        /// <summary>
        /// Port panel Update, we can view 64 Bytes at a time, it will be in form of 8 X 8
        /// </summary>
        private void UpdatePortPanel()
        {
            if (assembler85 != null)
            {
                int i = 0;
                int j = 0;

                foreach (Label lbl in portAddressLabels)
                {
                    lbl.Text = i.ToString("X").PadLeft(2, '0');
                    i += 0x10;
                }

                i = 0;
                j = 0;

                // PortTableLabels, display the port contents
                foreach (Label lbl in portTableLabels)
                {
                    lbl.Text = assembler85.PORT[(16 * i) + j].ToString("X").PadLeft(2, '0');

                    j++;
                    if (j == 0x10)
                    {
                        j = 0x00;
                        i++;
                    }
                }
            } else
            {
                // PortTableLabels, display 00
                foreach (Label lbl in portTableLabels)
                {
                    lbl.Text = "00";
                }
            }
        }

        /// <summary>
        /// Update 7 segment display of SDK-85 (if active)
        /// </summary>
        private void ClearDisplay()
        {
            if (formSDK_85 != null) 
            {
                formSDK_85.sevenSegmentData0.SegmentsValue = 0x00;
                formSDK_85.sevenSegmentData1.SegmentsValue = 0x00;
                formSDK_85.sevenSegmentAddress0.SegmentsValue = 0x00;
                formSDK_85.sevenSegmentAddress1.SegmentsValue = 0x00;
                formSDK_85.sevenSegmentAddress2.SegmentsValue = 0x00;
                formSDK_85.sevenSegmentAddress3.SegmentsValue = 0x00;
            }
        }

        /// <summary>
        /// Update 7 segment display of SDK-85 (if active)
        /// </summary>
        private void UpdateDisplay()
        {
            if ((formSDK_85 != null) && (assembler85.writeToDisplay))
            {
                // Data
                if (assembler85.RAM[0x1900] == 0x94)
                {
                    formSDK_85.sevenSegmentData1.SegmentsValue = formSDK_85.sevenSegmentData0.SegmentsValue;
                    formSDK_85.sevenSegmentData0.SegmentsValue = ~assembler85.RAM[0x1800];
                }

                // Address
                if (assembler85.RAM[0x1900] == 0x90)
                {
                    formSDK_85.sevenSegmentAddress3.SegmentsValue = formSDK_85.sevenSegmentAddress2.SegmentsValue;
                    formSDK_85.sevenSegmentAddress2.SegmentsValue = formSDK_85.sevenSegmentAddress1.SegmentsValue;
                    formSDK_85.sevenSegmentAddress1.SegmentsValue = formSDK_85.sevenSegmentAddress0.SegmentsValue;
                    formSDK_85.sevenSegmentAddress0.SegmentsValue = ~assembler85.RAM[0x1800];
                }

                assembler85.writeToDisplay = false;
            }
        }

        /// <summary>
        /// Update keyboard of SDK-85 (if active)
        /// </summary>
        private void UpdateKeyboard()
        {
            // Check for SDK-85 active
            if ((assembler85 != null) && (formSDK_85 != null))
            {
                // Check for keyboard interrupt (RST 5.5)
                if ((formSDK_85.keyC.Pressed) ||
                    (formSDK_85.keyD.Pressed) ||
                    (formSDK_85.keyE.Pressed) ||
                    (formSDK_85.keyF.Pressed) ||
                    (formSDK_85.keySingleStep.Pressed) ||
                    (formSDK_85.keyGo.Pressed) ||
                    (formSDK_85.key8.Pressed) ||
                    (formSDK_85.key9.Pressed) ||
                    (formSDK_85.keyA.Pressed) ||
                    (formSDK_85.keyB.Pressed) ||
                    (formSDK_85.keySubstMem.Pressed) ||
                    (formSDK_85.keyExamReg.Pressed) ||
                    (formSDK_85.key4.Pressed) ||
                    (formSDK_85.key5.Pressed) ||
                    (formSDK_85.key6.Pressed) ||
                    (formSDK_85.key7.Pressed) ||
                    (formSDK_85.keyNext.Pressed) ||
                    (formSDK_85.keyExec.Pressed) ||
                    (formSDK_85.key0.Pressed) ||
                    (formSDK_85.key1.Pressed) ||
                    (formSDK_85.key2.Pressed) ||
                    (formSDK_85.key3.Pressed))
                {
                    // Set interrupt indication flag
                    chkP55.Checked = true;

                    // Check for interrupt enable flag and interrupt mask 5.5 not set
                    if ((assembler85.intrIE) && (!assembler85.intrM55))
                    {
                        // Save current program counter to stack
                        assembler85.registerSP--;
                        assembler85.RAM[assembler85.registerSP] = (byte)((nextInstrAddress & 0xFF00) >> 8);
                        assembler85.registerSP--;
                        assembler85.RAM[assembler85.registerSP] = (byte)(nextInstrAddress & 0x00FF);

                        // Set interrupt vector to next addres to execute
                        nextInstrAddress = 0x002C;

                        // Reset interrupt indication flag
                        chkP55.Checked = false;

                        // Fill keycode provided by the 8279 Keyboard/Display Controller 
                        if (formSDK_85.keyC.Pressed) { assembler85.RAM[0x1800] = 0x0C; formSDK_85.keyC.Pressed = false; }
                        if (formSDK_85.keyD.Pressed) { assembler85.RAM[0x1800] = 0x0D; formSDK_85.keyD.Pressed = false; }
                        if (formSDK_85.keyE.Pressed) { assembler85.RAM[0x1800] = 0x0E; formSDK_85.keyE.Pressed = false; }
                        if (formSDK_85.keyF.Pressed) { assembler85.RAM[0x1800] = 0x0F; formSDK_85.keyF.Pressed = false; }
                        if (formSDK_85.keySingleStep.Pressed) { assembler85.RAM[0x1800] = 0x15; formSDK_85.keySingleStep.Pressed = false; }
                        if (formSDK_85.keyGo.Pressed) { assembler85.RAM[0x1800] = 0x12; formSDK_85.keyGo.Pressed = false; }
                        if (formSDK_85.key8.Pressed) { assembler85.RAM[0x1800] = 0x08; formSDK_85.key8.Pressed = false; }
                        if (formSDK_85.key9.Pressed) { assembler85.RAM[0x1800] = 0x09; formSDK_85.key9.Pressed = false; }
                        if (formSDK_85.keyA.Pressed) { assembler85.RAM[0x1800] = 0x0A; formSDK_85.keyA.Pressed = false; }
                        if (formSDK_85.keyB.Pressed) { assembler85.RAM[0x1800] = 0x0B; formSDK_85.keyB.Pressed = false; }
                        if (formSDK_85.keySubstMem.Pressed) { assembler85.RAM[0x1800] = 0x13; formSDK_85.keySubstMem.Pressed = false; }
                        if (formSDK_85.keyExamReg.Pressed) { assembler85.RAM[0x1800] = 0x14; formSDK_85.keyExamReg.Pressed = false; }
                        if (formSDK_85.key4.Pressed) { assembler85.RAM[0x1800] = 0x04; formSDK_85.key4.Pressed = false; }
                        if (formSDK_85.key5.Pressed) { assembler85.RAM[0x1800] = 0x05; formSDK_85.key5.Pressed = false; }
                        if (formSDK_85.key6.Pressed) { assembler85.RAM[0x1800] = 0x06; formSDK_85.key6.Pressed = false; }
                        if (formSDK_85.key7.Pressed) { assembler85.RAM[0x1800] = 0x07; formSDK_85.key7.Pressed = false; }
                        if (formSDK_85.keyNext.Pressed) { assembler85.RAM[0x1800] = 0x11; formSDK_85.keyNext.Pressed = false; }
                        if (formSDK_85.keyExec.Pressed) { assembler85.RAM[0x1800] = 0x10; formSDK_85.keyExec.Pressed = false; }
                        if (formSDK_85.key0.Pressed) { assembler85.RAM[0x1800] = 0x00; formSDK_85.key0.Pressed = false; }
                        if (formSDK_85.key1.Pressed) { assembler85.RAM[0x1800] = 0x01; formSDK_85.key1.Pressed = false; }
                        if (formSDK_85.key2.Pressed) { assembler85.RAM[0x1800] = 0x02; formSDK_85.key2.Pressed = false; }
                        if (formSDK_85.key3.Pressed) { assembler85.RAM[0x1800] = 0x03; formSDK_85.key3.Pressed = false; }
                    }
                }

                // Check for vector interrupt
                if (formSDK_85.keyVectIntr.Pressed) 
                {
                    // Set interrupt indication flag
                    chkP75.Checked = true;

                    // Check for interrupt enable flag and interrupt mask 7.5 not set
                    if ((assembler85.intrIE) && (!assembler85.intrM75))
                    {
                        // Save current program counter to stack
                        assembler85.registerSP--;
                        assembler85.RAM[assembler85.registerSP] = (byte)((nextInstrAddress & 0xFF00) >> 8);
                        assembler85.registerSP--;
                        assembler85.RAM[assembler85.registerSP] = (byte)(nextInstrAddress & 0x00FF);

                        // Set interrupt vector to next addres to execute
                        nextInstrAddress = 0x003C;

                        // Reset interrupt indication flag
                        chkP75.Checked = false;

                        // Reset key pressed in formSDK_85
                        formSDK_85.keyVectIntr.Pressed = false;
                    }
                }

                // Check for reset 
                if (formSDK_85.keyReset.Pressed)
                { 
                        nextInstrAddress = 0x0000; 
                        assembler85.intrIE = false; 
                        formSDK_85.keyReset.Pressed = false; 
                }
            }
        }

        /// <summary>
        /// get the memory start address from text box
        /// </summary>
        /// <returns></returns>
        private UInt16 GetTextBoxMemoryStartAddress()
        {
            string txtval = tbMemoryStartAddress.Text;
            UInt16 n = Convert.ToUInt16(txtval, 16);    // convert HEX to INT
            return n;
        }

        /// <summary>
        /// Change colors rich text box
        /// </summary>
        /// <param name="line_number"></param>
        /// <param name="error"></param>
        private void ChangeColorRTBLine(int line_number, bool error)
        {
            if ((line_number >= 0) && (richTextBoxProgram.Lines.Length > line_number))
            {
                // Disable event handler
                richTextBoxProgram.TextChanged -= richTextBoxProgram_TextChanged;
                richTextBoxProgram.SelectionChanged -= richTextBoxProgram_SelectionChanged;

                // Reset color
                richTextBoxProgram.SelectionStart = 0;
                richTextBoxProgram.SelectionLength = richTextBoxProgram.Text.Length;
                richTextBoxProgram.SelectionBackColor = System.Drawing.Color.White;

                // Get location in RTB
                int firstcharindex = richTextBoxProgram.GetFirstCharIndexFromLine(line_number);
                string currentlinetext = richTextBoxProgram.Lines[line_number];

                // Select line and color red/green
                richTextBoxProgram.SelectionStart = firstcharindex;
                richTextBoxProgram.SelectionLength = currentlinetext.Length;
                richTextBoxProgram.SelectionBackColor = System.Drawing.Color.LightGreen;
                if (error) richTextBoxProgram.SelectionBackColor = System.Drawing.Color.LightPink;

                // Reset selection
                richTextBoxProgram.SelectionStart = firstcharindex;
                richTextBoxProgram.SelectionLength = 0;

                // Scroll to line (show 1 line before selected line if available)
                if (line_number != 0)
                {
                    firstcharindex = richTextBoxProgram.GetFirstCharIndexFromLine(line_number - 1);
                    richTextBoxProgram.SelectionStart = firstcharindex;
                }

                richTextBoxProgram.ScrollToCaret();

                // Set cursor at selected line
                firstcharindex = richTextBoxProgram.GetFirstCharIndexFromLine(line_number);
                richTextBoxProgram.SelectionStart = firstcharindex;
                richTextBoxProgram.SelectionLength = 0;

                // Enable event handler
                richTextBoxProgram.TextChanged += new EventHandler(richTextBoxProgram_TextChanged);
                richTextBoxProgram.SelectionChanged += new EventHandler(richTextBoxProgram_SelectionChanged);
            }
        }

        /// <summary>
        /// show tooltip with string binaryval when we hover mouse over a (register) label 
        /// </summary>
        /// <param name="l"></param>
        private void RegisterHoverBinary(Label l)
        {
            string binaryval;
            binaryval = Convert.ToString(Convert.ToInt32(l.Text, 16), 2);

            // change the HEX string to BINARY string
            binaryval = binaryval.PadLeft(8, '0');
            toolTipRegisterBinary.SetToolTip(l, binaryval);
        }

        /// <summary>
        /// Update picturebox with breakpoint
        /// </summary>
        private void UpdateBreakPoint()
        {
            // Clear other breakpoint
            Graphics g = pbBreakPoint.CreateGraphics();
            g.Clear(Color.LightGray);

            if (lineBreakPoint >= 0)
            {
                int index = richTextBoxProgram.GetFirstCharIndexFromLine(lineBreakPoint);
                Point point = richTextBoxProgram.GetPositionFromCharIndex(index);

                g.FillEllipse(Brushes.Red, new Rectangle(1, richTextBoxProgram.Margin.Top + point.Y, 15, 15));
            }
        }

        /// <summary>
        /// Add info for each instruction button
        /// </summary>
        private void InitButtons()
        {
            // Init instruction buttons with texts
            foreach (Control control in groupBoxInstructions.Controls)
            {
                CommandDescription commandDescription;
                switch (control.Text)
                {
                    case "ACI":
                        commandDescription = new CommandDescription(control.Text, "00H", "Add 8-bit value + Carry to A");
                        break;
                    case "ADC":
                        commandDescription = new CommandDescription(control.Text, "R", "Add Register (or memory address M specified by H,L) + Carry to A");
                        break;
                    case "ADD":
                        commandDescription = new CommandDescription(control.Text, "R", "Add Register (or memory address M specified by H,L) to A");
                        break;
                    case "ADI":
                        commandDescription = new CommandDescription(control.Text, "00H", "Add 8-bit value to A");
                        break;
                    case "ANA":
                        commandDescription = new CommandDescription(control.Text, "R", "AND Register with A");
                        break;
                    case "ANI":
                        commandDescription = new CommandDescription(control.Text, "00H", "AND 8-bit value with A");
                        break;
                    case "CALL":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Call Unconditional");
                        break;
                    case "CC":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Call on Carry");
                        break;
                    case "CM":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Call on Minus");
                        break;
                    case "CMA":
                        commandDescription = new CommandDescription(control.Text, "", "Complement A");
                        break;
                    case "CMC":
                        commandDescription = new CommandDescription(control.Text, "", "Complement Carry");
                        break;
                    case "CMP":
                        commandDescription = new CommandDescription(control.Text, "R", "Compare Register (or memory address M specified by H,L) with A");
                        break;
                    case "CNC":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Call on No Carry");
                        break;
                    case "CNZ":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Call on No Zero");
                        break;
                    case "CP":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Call on Positive");
                        break;
                    case "CPE":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Call on Parity Even");
                        break;
                    case "CPI":
                        commandDescription = new CommandDescription(control.Text, "00H", "Compare 8-bit value with A ");
                        break;
                    case "CPO":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Call on Parity Odd");
                        break;
                    case "CZ":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Call on Zero");
                        break;
                    case "DAA":
                        commandDescription = new CommandDescription(control.Text, "", "Decimal Adjust accumulator");
                        break;
                    case "DAD":
                        commandDescription = new CommandDescription(control.Text, "Rp", "Add RegisterPair to HL");
                        break;
                    case "DCR":
                        commandDescription = new CommandDescription(control.Text, "R", "Register Decrement (or memory address M specified by H,L)");
                        break;
                    case "DCX":
                        commandDescription = new CommandDescription(control.Text, "Rp", "RegisterPair Decrement");
                        break;
                    case "DI":
                        commandDescription = new CommandDescription(control.Text, "", "Disable Interrupt");
                        break;
                    case "EI":
                        commandDescription = new CommandDescription(control.Text, "", "Enable Interrupt");
                        break;
                    case "HLT":
                        commandDescription = new CommandDescription(control.Text, "", "Halt");
                        break;
                    case "IN":
                        commandDescription = new CommandDescription(control.Text, "00H", "Input from 8-bit Port");
                        break;
                    case "INR":
                        commandDescription = new CommandDescription(control.Text, "R", "Register Increment (or memory address M specified by H,L)");
                        break;
                    case "INX":
                        commandDescription = new CommandDescription(control.Text, "R", "RegisterPair Increment");
                        break;
                    case "JC":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Jump on Carry");
                        break;
                    case "JM":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Jump on Minus");
                        break;
                    case "JMP":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Jump Unconditional");
                        break;
                    case "JNC":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Jump on No Carry");
                        break;
                    case "JNZ":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Jump on No Zero");
                        break;
                    case "JP":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Jump on Positive");
                        break;
                    case "JPE":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Jump on Even parity");
                        break;
                    case "JPO":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Jump on Odd parity");
                        break;
                    case "JZ":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Jump on Zero");
                        break;
                    case "LDA":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Load A direct");
                        break;
                    case "LDAX":
                        commandDescription = new CommandDescription(control.Text, "Rp", "Load A from memory address in BC / DE");
                        break;
                    case "LHLD ":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Load HL direct");
                        break;
                    case "LXI":
                        commandDescription = new CommandDescription(control.Text, "Rp, 0000H", "Load 16-bit in RegisterPair");
                        break;
                    case "MOV":
                        commandDescription = new CommandDescription(control.Text, "Rd, Rs", "Move Rs to Rd (or memory address M specified by H,L)");
                        break;
                    case "MVI":
                        commandDescription = new CommandDescription(control.Text, "R, 00H", "Load 8-bit in R (or memory address M specified by H,L)");
                        break;
                    case "NOP":
                        commandDescription = new CommandDescription(control.Text, "", "No Operation");
                        break;
                    case "ORA":
                        commandDescription = new CommandDescription(control.Text, "R", "OR A with R (or memory address M specified by H,L)");
                        break;
                    case "ORI":
                        commandDescription = new CommandDescription(control.Text, "00H", "OR A with 8-bit");
                        break;
                    case "OUT":
                        commandDescription = new CommandDescription(control.Text, "00H", "Output to 8-bit Port");
                        break;
                    case "PCHL":
                        commandDescription = new CommandDescription(control.Text, "", "Move HL to PC");
                        break;
                    case "POP":
                        commandDescription = new CommandDescription(control.Text, "Rp", "Pop RegisterPair");
                        break;
                    case "PUSH":
                        commandDescription = new CommandDescription(control.Text, "Rp", "Push RegisterPair");
                        break;
                    case "RAL ":
                        commandDescription = new CommandDescription(control.Text, "", "Rotate A Left through Carry (A7 -> C -> A0)");
                        break;
                    case "RAR":
                        commandDescription = new CommandDescription(control.Text, "", "Rotate A Right through Carry (A0 -> C -> A7)");
                        break;
                    case "RC ":
                        commandDescription = new CommandDescription(control.Text, "", "Return on Carry");
                        break;
                    case "RET ":
                        commandDescription = new CommandDescription(control.Text, "", "Return");
                        break;
                    case "RIM":
                        commandDescription = new CommandDescription(control.Text, "", "Read Interrupt Mask (to A)");
                        break;
                    case "RLC":
                        commandDescription = new CommandDescription(control.Text, "", "Rotate A Left (A7 -> C, A7 -> A0)");
                        break;
                    case "RM":
                        commandDescription = new CommandDescription(control.Text, "", "Return on Minus");
                        break;
                    case "RNC":
                        commandDescription = new CommandDescription(control.Text, "", "Return on No Carry");
                        break;
                    case "RNZ":
                        commandDescription = new CommandDescription(control.Text, "", "Return on No Zero");
                        break;
                    case "RP":
                        commandDescription = new CommandDescription(control.Text, "", "Return on Positive");
                        break;
                    case "RPE":
                        commandDescription = new CommandDescription(control.Text, "", "Return on Even Parity");
                        break;
                    case "RPO":
                        commandDescription = new CommandDescription(control.Text, "", "Return on Odd Parity");
                        break;
                    case "RRC":
                        commandDescription = new CommandDescription(control.Text, "", "Rotate A Right (A0 -> C, A0 -> A7)");
                        break;
                    case "RST":
                        commandDescription = new CommandDescription(control.Text, "N", "Restart (N = 0 to 7");
                        break;
                    case "RZ":
                        commandDescription = new CommandDescription(control.Text, "", "Return on Zero");
                        break;
                    case "SBB":
                        commandDescription = new CommandDescription(control.Text, "R", "Subtract R (or memory address M specified by H,L) from A with borrow");
                        break;
                    case "SBI":
                        commandDescription = new CommandDescription(control.Text, "00H", "Subtract 8-bit from A");
                        break;
                    case "SHLD":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Store HL direct");
                        break;
                    case "SIM":
                        commandDescription = new CommandDescription(control.Text, "", "Set interrupt Mask (from A)");
                        break;
                    case "SPHL":
                        commandDescription = new CommandDescription(control.Text, "", "Move HL to SP");
                        break;
                    case "STA":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Store A direct");
                        break;
                    case "STAX":
                        commandDescription = new CommandDescription(control.Text, "Rp", "Store A in memory address in BC / DE");
                        break;
                    case "STC":
                        commandDescription = new CommandDescription(control.Text, "", "Set Carry");
                        break;
                    case "SUB":
                        commandDescription = new CommandDescription(control.Text, "R", "Subtract R (or memory address M specified by H,L) from A");
                        break;
                    case "SUI":
                        commandDescription = new CommandDescription(control.Text, "00H", "Subtract 8-bit from A");
                        break;
                    case "XCHG":
                        commandDescription = new CommandDescription(control.Text, "", "Exchnage HL with DE");
                        break;
                    case "XRA":
                        commandDescription = new CommandDescription(control.Text, "R", "XOR A with R (or memory address M specified by H,L)");
                        break;
                    case "XRI":
                        commandDescription = new CommandDescription(control.Text, "00H", "XOR A with 8-bit");
                        break;
                    case "XTHL":
                        commandDescription = new CommandDescription(control.Text, "", "Exchange top of stack with HL");
                        break;
                    default:
                        commandDescription = new CommandDescription("-", "-", "-");
                        break;
                }

                control.Tag = commandDescription;
            }

            foreach (Control control in groupBoxUndocumentedInstructions.Controls)
            {
                CommandDescription commandDescription;
                switch (control.Text)
                {
                    case "ARHL":
                        commandDescription = new CommandDescription(control.Text, "", "Rotate HL Right (H7 -> H7, H0 -> L7, L0 -> C)");
                        break;
                    case "DSUB":
                        commandDescription = new CommandDescription(control.Text, "", "Subtract RegisterPair B,C from HL");
                        break;
                    case "JNK":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Jump on No K");
                        break;
                    case "JK":
                        commandDescription = new CommandDescription(control.Text, "0000H", "Jump on K");
                        break;
                    case "LDHI":
                        commandDescription = new CommandDescription(control.Text, "00H", "DE = HL + 8-bit");
                        break;
                    case "LDSI":
                        commandDescription = new CommandDescription(control.Text, "00H", "DE = SP + 8-bit");
                        break;
                    case "LHLX":
                        commandDescription = new CommandDescription(control.Text, "", "Load H and L indirect through D and E");
                        break;
                    case "RDEL":
                        commandDescription = new CommandDescription(control.Text, "", "Rotate DE Left through Carry (D7 -> C, E7 -> D0, C -> E0)");
                        break;
                    case "RSTV ":
                        commandDescription = new CommandDescription(control.Text, "", "Restart on overflow (RST 5");
                        break;
                    case "SHLX ":
                        commandDescription = new CommandDescription(control.Text, "", "Store H and L indirect through D and E");
                        break;
                    default:
                        commandDescription = new CommandDescription("-", "-", "-");
                        break;
                }

                control.Tag = commandDescription;
            }
        }

        #endregion
    }
}
