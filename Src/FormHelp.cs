using System;
using System.Windows.Forms;

namespace _8085
{
    public partial class FormHelp : Form
    {
        #region Members

        System.Drawing.Font newFont1 = new System.Drawing.Font("Georgia", 14f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 178, false);
        System.Drawing.Font newFont2 = new System.Drawing.Font("Georgia", 12f, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, 178, false);

        #endregion

        #region Constructor

        public FormHelp()
        {
            InitializeComponent();
        }

        #endregion

        #region EventHandlers

        /// <summary>
        /// Exit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// First shown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormHelp_Shown(object sender, EventArgs e)
        {
            // Load help text
            rtbInfo.Text = Properties.Resources.manual;

            // Deselect
            rtbInfo.DeselectAll();

            // Make everything between < and > bold
            int start = 0, end = 0;
            while ((start >= 0) && (end >= 0) && (start < rtbInfo.Text.Length) && (rtbInfo.Text.IndexOf('<', start) >= 0))
            {
                start = rtbInfo.Text.IndexOf('<', start);
                end   = rtbInfo.Text.IndexOf('>', start);

                if (end > 0)
                {
                    rtbInfo.Select(start, end - start + 1);
                    rtbInfo.SelectionFont = newFont1;
                }

                start = end;
            }

            // Make everything between ' and ' in italics
            start = end = 0;
            while ((start >= 0) && (end >= 0) && (start < rtbInfo.Text.Length) && (rtbInfo.Text.IndexOf('`', start) >= 0))
            {
                start = rtbInfo.Text.IndexOf('`', start);
                end = rtbInfo.Text.IndexOf('`', start + 1);

                if (end > 0)
                {
                    rtbInfo.Select(start, end - start + 1);
                    rtbInfo.SelectionFont = newFont2;
                }

                start = end + 1;
            }
        }

        #endregion
    }
}
