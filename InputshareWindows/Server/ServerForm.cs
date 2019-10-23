using InputshareLib;
using InputshareLib.Input.Hotkeys;
using InputshareLib.Server;
using InputshareLib.Server.API;
using InputshareLibWindows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputshareWindows.Server
{
    public partial class ServerForm : Form
    {
        private int startArg = 0;
        private ISServer server;
        private NotifyIcon trayIcon;

        public ServerForm(int startPort = 0)
        {
            startArg = startPort;
            InitializeComponent();
            this.FormClosed += MainForm_FormClosed;
            this.Resize += ServerForm_Resize;
            realtimeMouseRadioButton.Checked = true;
        }

        private void ServerForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                trayIcon.Visible = true;
            }
            else if (this.WindowState == FormWindowState.Normal)
            {
                trayIcon.Visible = false;
            }
        }

        private void CreateNotifyIcon()
        {
            trayIcon.Visible = false;
            trayIcon.Icon = Properties.Resources.RedSquare;
            trayIcon.Click += TrayIcon_Click;
            trayIcon.Text = "Inputshare server";
        }

        private void TrayIcon_Click(object sender, EventArgs e)
        {
            this.Show();
            this.BringToFront();
            this.TopMost = true;
            WindowState = FormWindowState.Normal;
        }

        private void Server_Stopped(object sender, EventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                trayIcon.Icon = Properties.Resources.RedSquare;

                trayIcon.ShowBalloonTip(2500, "Inputshare server", "Server stopped", ToolTipIcon.Warning);

                startServerButton.Text = "Start server";
                hotkeyListBox.Items.Clear();
                hotkeyListBox.Enabled = false;
                clientListBox.Enabled = false;
                clientListBox.Items.Clear();
                portTextBox.Show();
                portLabel.Show();
                displayConfigEditButton.Hide();
            }));
        }

        private void Server_Started(object sender, EventArgs e)
        {
            this.Invoke(new Action(() => {
                trayIcon.Icon = Properties.Resources.GreenSquare;
                startServerButton.Text = "Stop server";
                clientListBox.Enabled = true;
                hotkeyListBox.Enabled = true;
                UpdateClientList();
                UpdateHotkeyFunctionList();
                portTextBox.Hide();
                portLabel.Hide();
                displayConfigEditButton.Show();
            }));
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (server.Running)
                server.Stop();

            ISLogger.LogMessageOut -= ISLogger_LogMessageOut;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            trayIcon = new NotifyIcon();
            CreateNotifyIcon();

            server = new ISServer(WindowsDependencies.GetServerDependencies());
            server.Started += Server_Started;
            server.Stopped += Server_Stopped;
            server.GlobalClipboardContentChanged += Server_GlobalClipboardContentChanged;
            ISLogger.LogMessageOut += ISLogger_LogMessageOut;
            consoleRichTextBox.BackColor = Color.White;

            server.InputClientSwitched += Server_InputClientSwitched;
            server.ClientConnected += Server_ClientConnected;
            server.ClientDisconnected += Server_ClientDisconnected;
            hotkeyListBox.DoubleClick += HotkeyListBox_DoubleClick;
            clientListBox.DoubleClick += ClientListBox_DoubleClick;
            this.FormBorderStyle = FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;

            if (startArg != 0)
                server.Start(startArg);
        }

        private void Server_InputClientSwitched(object sender, ClientInfo e)
        {
            if (e.Name.ToLower() == "localhost")
                trayIcon.Icon = Properties.Resources.GreenSquare;
            else
                trayIcon.Icon = Properties.Resources.OrangeSquare;
        }

        private void Server_GlobalClipboardContentChanged(object sender, CurrentClipboardData e)
        {
            this.Invoke(new Action(() => {
                trayIcon.ShowBalloonTip(3000, "Inputshare server", "Clipboard set to type " + e.Type + "(" + e.Host.Name + ")", ToolTipIcon.Info);

                clipboardDataTypeLabel.Text = "Data type: " + e.Type.ToString();
                clipboardHostLabel.Text = "Host: " + e.Host.Name;
            }));
        }

        private void Server_ClientDisconnected(object sender, ClientInfo e)
        {
            if (trayIcon.Visible)
                trayIcon.ShowBalloonTip(3000, "Inputshare server", e.Name + " disconnected", ToolTipIcon.Info);

            UpdateClientList();
        }

        private void Server_ClientConnected(object sender, ClientInfo e)
        {
            if (trayIcon.Visible)
                trayIcon.ShowBalloonTip(3000, "Inputshare server", e.Name + " connected (" + e.ClientAddress.ToString() +")", ToolTipIcon.Info);

            UpdateClientList();
        }

        private void ISLogger_LogMessageOut(object sender, string message)
        {
            this.Invoke(new Action(() => { consoleRichTextBox.AppendText(message +"\n");
                consoleRichTextBox.ScrollToCaret();
            }));
        }

        private void StartServerButton_Click(object sender, EventArgs e)
        {
            if (server.Running)
            {
                try
                {
                    server.Stop();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to stop server: " + ex.Message);
                }
                
            }
            else
            {
                int.TryParse(portTextBox.Text, out int port);
                if(port == 0)
                {
                    Task.Run(() => {
                        MessageBox.Show("Invalid port");
                    });
                    return;
                }

                try
                {
                    server.Start(port);
                }catch(Exception ex)
                {
                    MessageBox.Show("Failed to start server: " + ex.Message);
                }
                
            }
        }

        private void UpdateClientList()
        {
            if (!server.Running)
                return;

            List<ClientInfo> clients = new List<ClientInfo>(server.GetAllClients());

            this.Invoke(new Action(() => { clientListBox.Items.Clear(); }));

            foreach(var client in clients)
            {
                this.Invoke(new Action(() => { clientListBox.Items.Add(new ClientListItem(client)); }));
            }
        }

        private void HotkeyListBox_DoubleClick(object sender, EventArgs e)
        {
            if (hotkeyListBox.SelectedIndex == -1)
                return;

            HotkeyListItem item = hotkeyListBox.Items[hotkeyListBox.SelectedIndex] as HotkeyListItem;
            if (item == null)
                return;

            AssignHotkeyWindow wnd = new AssignHotkeyWindow("Assign a hotkey for " + item.Hotkey.Function);
            wnd.ShowDialog();

            if(wnd.AssignedKey.Key == 0)
            {
                MessageBox.Show("Invalid hotkey input");
                return;
            }

            try
            {
                server.SetHotkeyForFunction(wnd.AssignedKey, item.Hotkey.Function);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Failed to set function hotkey: " + ex.Message);
            }
            
            wnd.Dispose();
            UpdateHotkeyFunctionList();
        }

        private void ClientListBox_DoubleClick(object sender, EventArgs e)
        {
            if (clientListBox.SelectedIndex == -1)
                return;

            ClientListItem item = clientListBox.Items[clientListBox.SelectedIndex] as ClientListItem;

            if (item == null)
                return;

            AssignHotkeyWindow wnd = new AssignHotkeyWindow("Assign a hotkey for client " + item.Client.Name);
            wnd.ShowDialog();

            if (wnd.AssignedKey.Key == 0)
            {
                MessageBox.Show("Invalid hotkey input");
                return;
            }

            try
            {
                server.SetHotkeyForClient(item.Client, wnd.AssignedKey);
            }catch(Exception ex)
            {
                MessageBox.Show("Failed to set client hotkey: " + ex.Message);
            }
            
            wnd.Dispose();
            UpdateClientList();
        }


        private void UpdateHotkeyFunctionList()
        {
            if (!server.Running)
                return;

            List<Hotkey> keys = new List<Hotkey>();
            this.Invoke(new Action(() => { hotkeyListBox.Items.Clear(); }));
            foreach (var func in (Hotkeyfunction[])Enum.GetValues(typeof(Hotkeyfunction)))
            {
                FunctionHotkey hk = server.GetHotkeyForFunction(func);
                if (hk != null)
                    this.Invoke(new Action(() => { hotkeyListBox.Items.Add(new HotkeyListItem(hk)); }));
            }
        }

        class HotkeyListItem
        {
            public HotkeyListItem(FunctionHotkey hotkey)
            {
                Hotkey = hotkey;
            }

            public override string ToString()
            {
                return Hotkey.Function + ": " + Hotkey.ToString();
            }

            public FunctionHotkey Hotkey { get; }
        }

        class ClientListItem
        {
            public ClientListItem(ClientInfo client)
            {
                Client = client;
            }

            public override string ToString()
            {
                if (Client.ClientHotkey != null)
                    return Client.Name + " (" + Client.ClientHotkey + ")";
                else
                    return Client.Name;
            }

            public ClientInfo Client { get; }
        }

        private void hotkeyListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void displayConfigEditButton_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(() => {
                DisplayConfigEditor editor = new DisplayConfigEditor(new List<ClientInfo>(server.GetAllClients()));
                editor.ShowDialog();

                List<ClientInfo> clients = editor.Clients;

                try
                {
                    foreach (var client in clients)
                    {
                        if (client.LeftClient != null)
                            server.SetClientEdge(client.LeftClient, Edge.Left, client);
                        else
                            server.RemoveClientEdge(client, Edge.Left);


                        if (client.RightClient != null)
                            server.SetClientEdge(client.RightClient, Edge.Right, client);
                        else
                            server.RemoveClientEdge(client, Edge.Right);

                        if (client.TopClient != null)
                            server.SetClientEdge(client.TopClient, Edge.Top, client);
                        else
                            server.RemoveClientEdge(client, Edge.Top);

                        if (client.BottomClient != null)
                            server.SetClientEdge(client.BottomClient, Edge.Bottom, client);
                        else
                            server.RemoveClientEdge(client, Edge.Bottom);

                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Failed to set client edges: " + ex.Message);
                }
                


            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        private void realtimeMouseRadioButton_CheckedChanged(object sender, EventArgs e)
        {
          
        }

        private void bufferedMouseRadioButton_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void realtimeMouseRadioButton_MouseClick(object sender, MouseEventArgs e)
        {
            realtimeMouseRadioButton.Checked = true;
            bufferedMouseRadioButton.Checked = false;

            if (server.Running)
                server.SetMouseInputMode(InputshareLib.Input.MouseInputMode.Realtime);
        }

        private void bufferedMouseRadioButton_MouseClick(object sender, MouseEventArgs e)
        {
            if (!int.TryParse(mouseUpdateRateTextBox.Text, out int newRate) || newRate < 1 || newRate > 1000)
            {
                MessageBox.Show("Invalid update rate. Must be between 1 and 1000");
                return;
            }

            realtimeMouseRadioButton.Checked = false;
            bufferedMouseRadioButton.Checked = true;

            if (server.Running)
                server.SetMouseInputMode(InputshareLib.Input.MouseInputMode.Buffered, newRate);
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }
    }
}
