using InputshareLib;
using InputshareLibWindows.IPC.NetIpc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputshareWindows.ServiceClient
{
    public partial class ServiceClientForm : Form
    {
        private NetIpcClient netClient;


        public ServiceClientForm()
        {
            InitializeComponent();
            Closed += ServiceClientForm_Closed;
        }

        private void ServiceClientForm_Closed(object sender, EventArgs e)
        {
            if(netClient != null)
            {
                netClient.Disconnected -= NetClient_Disconnected;
                netClient?.Dispose();
            }
           
        }

        private async void ServiceClientForm_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Text = "Inputshare service client";
            await ConnectIpc();
        }

        private async Task ConnectIpc()
        {
            try
            {
                netClient = new NetIpcClient("Service connection");
                netClient.Disconnected += NetClient_Disconnected;
                netClient.ServerConnected += NetClient_ServerConnected;
                netClient.ServerDisconnected += NetClient_ServerDisconnected;
                netClient.AutoReconnectChanged += NetClient_AutoReconnectChanged;
                if (!netClient.ConnectedEvent.WaitOne(3000))
                    throw new Exception("Timed out waiting for connection");


                if (await netClient.GetConnectedState())
                    OnConnected();
                else
                    OnDisconnected();

                nameTextBox.Text = await netClient.GetClientName();
                autoReconnectCheckBox.Checked = await netClient.GetAutoReconnectState();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to connect to service IPC. Check that the inputshare service is running.\n\n" + ex.Message);
                ISLogger.Write(ex.StackTrace);
                InvokeClose();
            }
        }

        private void NetClient_AutoReconnectChanged(object sender, bool e)
        {
            this.Invoke(new Action(() => { autoReconnectCheckBox.Checked = e; }));
        }

        private void NetClient_ServerDisconnected(object sender, EventArgs e)
        {
            OnDisconnected();
        }

        private void NetClient_ServerConnected(object sender, EventArgs e)
        {
            OnConnected();
        }

        private void NetClient_Disconnected(object sender, string reason)
        {
            MessageBox.Show("Connection to service lost. " + reason);
            InvokeClose();
        }

        private void OnConnected()
        {
            this.Invoke(new Action(() =>
            {
                nameTextBox.Hide();
                portTextBox.Hide();
                addressTextBox.Hide();
                connectButton.Text = "Disconnect";
                autoReconnectCheckBox.Show();
                connectButton.Show();
            }));
        }
        private void OnDisconnected()
        {
            this.Invoke(new Action(() =>
            {
                nameTextBox.Show();
                portTextBox.Show();
                addressTextBox.Show();
                connectButton.Text = "Connect";
                autoReconnectCheckBox.Show();
                connectButton.Show();
            }));
        }

        private void InvokeClose()
        {
            this.Invoke(new Action(() => { this.Close(); }));
        }


        private async void Button2_Click(object sender, EventArgs e)
        {
            if (await netClient.GetConnectedState())
            {
                netClient.Disconnect();
            }
            else
            {
                IPEndPoint ipe = ParseAddressInput();

                if (ipe == null)
                {
                    MessageBox.Show("Invalid address/port");
                    return;
                }

                if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                {
                    MessageBox.Show("Invalid client name");
                    return;
                }

                netClient.SetName(nameTextBox.Text);
                netClient.Connect(ipe);
            }
           
        }

        private IPEndPoint ParseAddressInput()
        {
            if (!IPAddress.TryParse(addressTextBox.Text, out IPAddress addr))
            {
                var addresses = Dns.GetHostAddresses(addressTextBox.Text);

                if (addresses.Length == 0)
                    return null;

                foreach(var address in addresses)
                {
                    if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        addr = address;
                }
            }

            if (!int.TryParse(portTextBox.Text, out int port))
                return null;

            return new IPEndPoint(addr, port);
        }

        private void AutoReconnectCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            netClient.SetAutoReconnectEnable(autoReconnectCheckBox.Checked);
        }
    }
}
