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
    public partial class FormTerminal : Form
    {
        #region Members

        // Initial location of window
        private int x, y;

        // Keyboard buffer
        public string keyBuffer;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public FormTerminal(int x, int y)
        {
            InitializeComponent();

            this.x = x;
            this.y = y;

            keyBuffer = "";
        }

        #endregion

        #region EventHandlers

        /// <summary>
        /// Form loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormTerminal_Load(object sender, EventArgs e)
        {
            // Set location of window
            this.Location = new Point(x, y);

            tbTerminal.Font = new Font(FontFamily.GenericMonospace, 10.25F);

            cbBaudRate.SelectedIndex = 7;
        }

        /// <summary>
        /// Key pressed, send to terminal
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbTerminal_KeyPress(object sender, KeyPressEventArgs e)
        {
            keyBuffer += e.KeyChar;
            e.Handled = true;
        }

        #endregion

        #region Methods

        public void Clear()
        {
            tbTerminal.Text = "";
            keyBuffer = "";
            tbKeyBuffer.Text = keyBuffer;
        }

        public void UpdateBufferText()
        {
            tbKeyBuffer.Text = keyBuffer;
        }

        /// <summary>
        /// bitCycles = 3.072 * 10^6 / Br
        /// </summary>
        /// <returns></returns>
        public UInt64 GetBitCycles()
        {
            switch (cbBaudRate.SelectedItem.ToString().Trim())
            {
                case "110 Bd":
                    return 27927;
                case "150 Bd":
                    return 20480;
                case "300 Bd":
                    return 10240;
                case "600 Bd":
                    return 5120;
                case "1200 Bd":
                    return 2560;
                case "2400 Bd":
                    return 1280;
                case "4800 Bd":
                    return 640;
                case "9600 Bd":
                    return 320;
                case "19200 Bd":
                    return 160;
                default:
                    return 0;
            }
        }

        #endregion
    }
}
