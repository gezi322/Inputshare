using InputshareLib;
using InputshareLibWindows.IPC.NamedIpc;
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
    public partial class ServiceClientForm : Form
    {
        private NamedIpcClient serviceHost;
        private NamedIpcClient.ServiceConnectionState currentState;
        private System.Threading.Timer stateUpdateTimer;

        public ServiceClientForm()
        {
            InitializeComponent();
        }

        private void ServiceClientForm_Load(object sender, EventArgs e)
        {
            nameTextBox.Text = Environment.MachineName;

            Task.Run(() => {
                try
                {
                    serviceHost = new NamedIpcClient();
                    serviceHost.StateReceived += ServiceHost_StateReceived;
                    currentState = serviceHost.GetState();

                    this.Invoke(new Action(() => { nameTextBox.Text = currentState.ClientName; }));
                    OnStateUpdate();
                }catch(Exception ex)
                {
                    MessageBox.Show("Failed to connect to service IPC. Check that the inputshare service is running.\n\n" + ex.Message);
                    this.Close();
                }
                
            });

            //stateUpdateTimer = new System.Threading.Timer(StateUpdateTimerCallback, null, 0, 1000);
        }

        private void StateUpdateTimerCallback(object sync)
        {
            if (serviceHost != null && serviceHost.Connected)
            {
                currentState = serviceHost.GetState();
                OnStateUpdate();
            }
               
        }

        private void ServiceHost_StateReceived(object sender, NamedIpcClient.ServiceConnectionState newState)
        {
            currentState = newState;
            OnStateUpdate();
        }

        private void OnStateUpdate()
        {
            ISLogger.Write("Got state update... connected = " + currentState.Connected);

            if (currentState.Connected)
                OnConnected();
            else
                OnDisconnected();
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
                autoReconnectCheckBox.Checked = currentState.AutoReconnect;
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
                autoReconnectCheckBox.Checked = currentState.AutoReconnect;
            }));
        }



        private void button2_Click(object sender, EventArgs e)
        {
            if (!currentState.Connected)
                serviceHost.Connect(new System.Net.IPEndPoint(IPAddress.Parse(addressTextBox.Text), int.Parse(portTextBox.Text)), nameTextBox.Text);
            else
                serviceHost.Disconnect();
        }

        private void autoReconnectCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (serviceHost.Connected)
                serviceHost.SetAutoReconnect(autoReconnectCheckBox.Checked);
        }
    }
}
