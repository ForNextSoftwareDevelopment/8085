using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace _8085
{
    public partial class FormSerial : Form
    {
        #region Members

        // Initial location of window
        private int x, y;

        // Images
        Bitmap bmSOD;
        Bitmap bmSID;

        // SID/SOD values
        private List<bool> valuesSID;
        private List<bool> valuesSOD;

        // SID/SOD timecodes
        private Dictionary<int, UInt64> timecodesSID;
        private Dictionary<int, UInt64> timecodesSOD;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public FormSerial(int x, int y)
        {
            InitializeComponent();

            this.x = x;
            this.y = y;

            // New list of values
            valuesSID = new List<bool>();
            valuesSOD = new List<bool>();

            // New dictinary of timecodes
            timecodesSID = new Dictionary<int, UInt64>();
            timecodesSOD = new Dictionary<int, UInt64>();
        }

        #endregion

        #region EventHandlers

        /// <summary>
        /// Form loaded, clear screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormSerial_Load(object sender, EventArgs e)
        {
            // Set location of window
            this.Location = new Point(x, y);

            // Initialize graphics
            bmSOD = new Bitmap(pbSOD.Size.Width, pbSOD.Size.Height);
            bmSID = new Bitmap(pbSID.Size.Width, pbSID.Size.Height);

            pbSOD.Image = bmSOD;
            pbSID.Image = bmSID;

            Graphics gSOD = Graphics.FromImage(pbSOD.Image);
            Graphics gSID = Graphics.FromImage(pbSID.Image);

            gSOD.Clear(Color.FloralWhite);
            gSID.Clear(Color.FloralWhite);

            gSOD.Dispose();
            gSID.Dispose();
        }

        /// <summary>
        /// Redraw if value changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void hScrollBar_ValueChanged(object sender, EventArgs e)
        {
            DrawSID();
            DrawSOD();
        }

        /// <summary>
        /// Disable auto keepup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void hScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            chkKeepUp.Checked = false;
        }

        /// <summary>
        /// Resize images according to new size picturebox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormSerial_Resize(object sender, EventArgs e)
        {
            bmSOD = new Bitmap(pbSOD.Size.Width, pbSOD.Size.Height);
            bmSID = new Bitmap(pbSID.Size.Width, pbSID.Size.Height);
            pbSID.Image = bmSID;
            pbSOD.Image = bmSOD;
            DrawSOD();
            DrawSID();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Add new value to SID list
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cycles"></param>
        public void AddToSID(bool value, UInt64 cycles)
        {
            if ((valuesSID.Count > 0) && (valuesSID[valuesSID.Count -1] != value))
            {
                timecodesSID.Add(valuesSID.Count, cycles);
            }
            valuesSID.Add(value);
        }

        /// <summary>
        /// Add new value to SOD list
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cycles"></param>
        public void AddToSOD(bool value, UInt64 cycles)
        {
            if ((valuesSOD.Count > 0) && (valuesSOD[valuesSOD.Count - 1] != value))
            {
                timecodesSOD.Add(valuesSOD.Count, cycles);
            }
            valuesSOD.Add(value);
        }

        /// <summary>
        /// Draw a graphic representation of the SID on screen
        /// </summary>
        public void DrawSID()
        {
            int X_SID = 0;
            int Y_SID = pbSID.Height - 14;

            int X_SID_new = 0;
            int Y_SID_new = pbSID.Height - 14;
            if ((valuesSID.Count > 0) && valuesSID[0]) Y_SID_new = 10;

            hScrollBar.Maximum = valuesSID.Count;
            if (chkKeepUp.Checked)
            {
                int temp = valuesSID.Count - pbSID.Width;
                if (temp < 0) temp = 0;
                hScrollBar.Value = temp;
            }

            int index = hScrollBar.Value;
            if ((valuesSID.Count > 0) && valuesSID[index]) Y_SID = 10;

            // Set SID picturebox and clear
            Graphics gSID = Graphics.FromImage(pbSID.Image);
            gSID.Clear(Color.FloralWhite);

            do
            {
                // Set new level
                X_SID_new += 1;
                Y_SID_new = pbSID.Height - 14;

                if (index < valuesSID.Count)
                {
                    if (valuesSID[index]) Y_SID_new = 10;

                    gSID.DrawLine(new Pen(Color.Red), X_SID, Y_SID, X_SID, Y_SID_new);
                    gSID.DrawLine(new Pen(Color.Red), X_SID, Y_SID_new, X_SID_new, Y_SID_new);

                    if (timecodesSID.ContainsKey(index))
                    {
                        if ( valuesSOD[index]) gSID.DrawString((timecodesSID[index] / 3.072).ToString("F0"), new Font(FontFamily.GenericMonospace, 6.5F), new SolidBrush(Color.Black), X_SID, 0);
                        if (!valuesSOD[index]) gSID.DrawString((timecodesSID[index] / 3.072).ToString("F0"), new Font(FontFamily.GenericMonospace, 6.5F), new SolidBrush(Color.Black), X_SID, pbSID.Height - 14);
                    }

                    X_SID = X_SID_new;
                    Y_SID = Y_SID_new;
                }

                index++;
            } while (X_SID_new < pbSID.Width);

            // Refresh images
            pbSID.Invalidate();
            pbSID.Update();
        }

        /// <summary>
        /// Draw a graphic representation of the SOD on screen
        /// </summary>
        public void DrawSOD()
        {
            int X_SOD = 0;
            int Y_SOD = pbSOD.Height - 14;

            int X_SOD_new = 0;
            int Y_SOD_new = pbSOD.Height - 14;

            if (valuesSOD.Count == 0) hScrollBar.Maximum = 0; else hScrollBar.Maximum = valuesSOD.Count - 1;

            if (chkKeepUp.Checked)
            {
                int temp = valuesSOD.Count - pbSOD.Width;
                if (temp < 0) temp = 0;
                hScrollBar.Value = temp;
            }

            int index = hScrollBar.Value;
            if ((valuesSOD.Count > 0) && valuesSOD[index]) Y_SOD = 10;

            // Set SOD picturebox and clear
            Graphics gSOD = Graphics.FromImage(pbSOD.Image);
            gSOD.Clear(Color.FloralWhite);

            do
            {
                // Set new level
                X_SOD_new += 1;
                Y_SOD_new = pbSOD.Height - 14;

                if (index < valuesSOD.Count)
                {
                    if (valuesSOD[index]) Y_SOD_new = 10;

                    gSOD.DrawLine(new Pen(Color.Red), X_SOD, Y_SOD, X_SOD, Y_SOD_new);
                    gSOD.DrawLine(new Pen(Color.Red), X_SOD, Y_SOD_new, X_SOD_new, Y_SOD_new);

                    if (timecodesSOD.ContainsKey(index))
                    {
                        if ( valuesSOD[index]) gSOD.DrawString((timecodesSOD[index] / 3.072).ToString("F0"), new Font(FontFamily.GenericMonospace, 6.5F), new SolidBrush(Color.Black), X_SOD, 0);
                        if (!valuesSOD[index]) gSOD.DrawString((timecodesSOD[index] / 3.072).ToString("F0"), new Font(FontFamily.GenericMonospace, 6.5F), new SolidBrush(Color.Black), X_SOD, pbSOD.Height - 14);
                    }

                    X_SOD = X_SOD_new;
                    Y_SOD = Y_SOD_new;
                }

                index++;
            } while (X_SOD_new < pbSOD.Width);

            // Refresh images
            pbSOD.Invalidate();
            pbSOD.Update();
        }

        /// <summary>
        /// Clear all SID/SOD values in buffer
        /// </summary>
        public void ClearValues()
        {
            valuesSID.Clear();
            valuesSOD.Clear();
            timecodesSID.Clear();
            timecodesSOD.Clear();
            DrawSID();
            DrawSOD();
        }

        #endregion
    }
}
