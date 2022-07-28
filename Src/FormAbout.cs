using System;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace _8085
{
    public partial class FormAbout : Form
    {
        public FormAbout()
        {
            InitializeComponent();

            // Get version info to display
            Assembly thisAssem = typeof(MainForm).Assembly;
            AssemblyName thisAssemName = thisAssem.GetName();
            Version ver = thisAssemName.Version;
            tbAbout.Text += "\r\n\r\nversion: " + ver.Major + "." + ver.Minor;

            tbAbout.DeselectAll();
        }

        private void button_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void FormAbout_Shown(object sender, EventArgs e)
        {
            this.tbAbout.DeselectAll();
            this.btnOK.Focus();
        }
    }
}
