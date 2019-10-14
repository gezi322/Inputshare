using InputshareLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace InputshareWindows
{
    public partial class InitForm : Form
    {
        public static void Main()
        {
            ISLogger.EnableConsole = false;
            ISLogger.EnableLogFile = true;
            ISLogger.SetLogFileName("InputshareWindows.log");

            Application.Run(new InitForm());
        }

        public InitForm()
        {
            InitializeComponent();
            this.FormClosed += InitForm_FormClosed;
        }

        private void InitForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            ISLogger.Exit();
        }

        private void InitForm_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
        }

        private void serverButton_Click(object sender, EventArgs e)
        {
            ShowForm(new ServerForm());
        }

        private void ShowForm(Form form)
        {
            this.Hide();
            form.ShowDialog();
            form.Dispose();
            this.Show();
        }

        private void buttonClient_Click(object sender, EventArgs e)
        {
            ShowForm(new ClientForm());
        }

        private void buttonServiceClient_Click(object sender, EventArgs e)
        {
            ShowForm(new ServiceClientForm());
        }
    }
}
