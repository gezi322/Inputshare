using InputshareLib;
using InputshareLib.Client;
using InputshareLibWindows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputshareWindows
{
    

    public partial class ClientForm : Form
    {
        private ISClient client;

        public ClientForm()
        {
            InitializeComponent();
            client = new ISClient(WindowsDependencies.GetClientDependencies());
            client.Connected += Client_Connected;
            client.ConnectionError += Client_ConnectionError;
            client.ConnectionFailed += Client_ConnectionFailed;
            client.Disconnected += Client_Disconnected;
            client.SasRequested += Client_SasRequested;
            client.ActiveClientChanged += Client_ActiveClientChanged;

            ISLogger.LogMessageOut += ISLogger_LogMessageOut;
            this.FormClosed += ClientForm_FormClosed;
            this.FormBorderStyle = FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            clientNameTextBox.Text = Environment.MachineName;
            portTextBox.Text = "4441";
        }

        private void ClientForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            client.Stop();
            ISLogger.LogMessageOut -= ISLogger_LogMessageOut;
        }

        private void ISLogger_LogMessageOut(object sender, string message)
        {
            this.Invoke(new Action(() => {
                consoleRichTextBox.AppendText(message + "\n");
                consoleRichTextBox.ScrollToCaret();
            }));
        }

        private void Client_ActiveClientChanged(object sender, bool active)
        {

        }

        private void Client_SasRequested(object sender, EventArgs e)
        {
            ShowMessage("Alt+Ctrl+Delete is not supported on standalone client");
        }

        private void Client_Disconnected(object sender, EventArgs e)
        {
            OnDisconnect();
        }

        private void Client_ConnectionFailed(object sender, string e)
        {
            OnDisconnect();

            if (!client.AutoReconnect)
                ShowMessage("Connection failed: " + e);
        }

        private void Client_ConnectionError(object sender, string e)
        {
            OnDisconnect();

            if(!client.AutoReconnect)
                ShowMessage("Connection error: " + e);
        }

        private void Client_Connected(object sender, System.Net.IPEndPoint e)
        {
            OnConnect(e);
        }

        private void OnConnect(IPEndPoint address)
        {
            this.Invoke(new Action(() => {
                this.Text = "Inputshare client (" + address + ")";
                label1.Hide();
                label2.Hide();
                label3.Hide();
                clientNameTextBox.Hide();
                portTextBox.Hide();
                addressTextBox.Hide();
                connectButton.Text = "Disconnect";
            }));
        }

        private void OnDisconnect()
        {
            this.Invoke(new Action(() =>
            {
                this.Text = "Inputshare client (disconnected)";
                label1.Show();
                label2.Show();
                label3.Show();
                clientNameTextBox.Show();
                portTextBox.Show();
                addressTextBox.Show();
                connectButton.Text = "Connect";
            }));
        }

        private void ClientForm_Load(object sender, EventArgs e)
        {
            consoleRichTextBox.ReadOnly = true;
            consoleRichTextBox.BackColor = Color.White;
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            if (!client.IsConnected)
            {
                try
                {
                    client.SetClientName(clientNameTextBox.Text);
                    client.Connect(addressTextBox.Text, int.Parse(portTextBox.Text));
                }
                catch (FormatException)
                {
                    ShowMessage("Invalid input");
                }catch(Exception ex)
                {
                    ShowMessage(ex.Message);
                }
            }

            else
            {
                client.Disconnect();
                OnDisconnect();
            }
               
        }

        private void ShowMessage(string message)
        {
            Task.Run(() =>
            {
                MessageBox.Show(message);
            });
        }

        private void autoReconnectTickbox_CheckedChanged(object sender, EventArgs e)
        {
            client.AutoReconnect = autoReconnectTickbox.Checked;
        }
    }
}
