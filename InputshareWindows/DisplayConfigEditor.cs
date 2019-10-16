using InputshareLib;
using InputshareLib.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace InputshareWindows
{
    public partial class DisplayConfigEditor : Form
    {
        public List<ClientInfo> Clients { get; }
        private ClientInfo selectedClient;
        private ClientInfo draggingClient = null;

        public DisplayConfigEditor(List<ClientInfo> clients)
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Clients = clients;
            PopulateListBoxes();

            otherClientsListBox.MouseDown += OtherClientsListBox_MouseDown;
            selectedClientListBox.SelectedIndexChanged += SelectedClientListBox_SelectedIndexChanged;

            leftLabel.DragOver += LeftLabel_DragOver;
            leftLabel.DragDrop += LeftLabel_DragDrop;

            rightLabel.DragOver += RightLabel_DragOver;
            rightLabel.DragDrop += RightLabel_DragDrop;

            topLabel.DragOver += TopLabel_DragOver;
            topLabel.DragDrop += TopLabel_DragDrop;

            bottomLabel.DragOver += BottomLabel_DragOver;
            bottomLabel.DragDrop += BottomLabel_DragDrop;

            selectedClient = clients[0];
        }

        private void BottomLabel_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void BottomLabel_DragDrop(object sender, DragEventArgs e)
        {
            if (selectedClient == null)
            {
                MessageBox.Show("No client selected");
                return;
            }

            SetEdge(draggingClient, Edge.Bottom, selectedClient);
        }

        private void TopLabel_DragDrop(object sender, DragEventArgs e)
        {
            if (selectedClient == null)
            {
                MessageBox.Show("No client selected");
                return;
            }

            SetEdge(draggingClient, Edge.Top, selectedClient);
        }

        private void TopLabel_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }
        

        private void RightLabel_DragDrop(object sender, DragEventArgs e)
        {
            if (selectedClient == null)
            {
                MessageBox.Show("No client selected");
                return;
            }

            SetEdge(draggingClient, Edge.Right, selectedClient);
        }

        private void RightLabel_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void LeftLabel_DragDrop(object sender, DragEventArgs e)
        {
            if (selectedClient == null)
            {
                MessageBox.Show("No client selected");
                return;
            }

            SetEdge(draggingClient, Edge.Left, selectedClient);
        }

        private void SetEdge(ClientInfo clientA, Edge edgeOf, ClientInfo clientB)
        {
            if(clientA.Name == "None")
            {
                RemoveEdge(clientB, edgeOf);
                PopulateListBoxes();
                return;
            }

            switch (edgeOf)
            {
                case Edge.Bottom:
                    clientB.BottomClient = clientA;
                    clientA.TopClient = clientB;
                    break;
                case Edge.Left:
                    clientB.LeftClient = clientA;
                    clientA.RightClient = clientB;
                    break;
                case Edge.Right:
                    clientB.RightClient = clientA;
                    clientA.LeftClient = clientB;
                    break;
                case Edge.Top:
                    clientB.TopClient = clientA;
                    clientA.BottomClient = clientB;
                    break;
            }
            PopulateListBoxes();
        }

        private ClientInfo GetFullClient(string name)
        {
            foreach(var client in Clients)
            {
                if (client.Name == name)
                    return client;
            }

            return null;
        }

        private ClientInfo GetClientAtEdge(ClientInfo client, Edge edge)
        {
            switch (edge)
            {
                case Edge.Bottom:
                    return client.BottomClient;
                case Edge.Right:
                    return client.RightClient;
                case Edge.Left:
                    return client.LeftClient;
                case Edge.Top:
                    return client.TopClient;
                default:
                    return null;
            }
        }

        private void RemoveEdge(ClientInfo client, Edge edge)
        {
            switch (edge)
            {
                case Edge.Bottom:
                    if (client.BottomClient != null)
                        GetFullClient(client.BottomClient.Name).TopClient = null;

                    client.BottomClient = null;
                    break;
                case Edge.Left:
                    if (client.LeftClient != null)
                        GetFullClient(client.LeftClient.Name).RightClient = null;

                    client.LeftClient = null;
                    break;
                case Edge.Top:
                    if (client.TopClient != null)
                        GetFullClient(client.TopClient.Name).BottomClient = null;
                    client.TopClient = null;
                    break;
                case Edge.Right:
                    if (client.RightClient != null)
                        GetFullClient(client.RightClient.Name).LeftClient = null;
                    client.RightClient = null;
                    break;
            }

            PopulateListBoxes();
        }

        private void LeftLabel_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void OtherClientsListBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (otherClientsListBox.SelectedIndex == -1)
                return;

            ClientInfo item = otherClientsListBox.Items[otherClientsListBox.SelectedIndex] as ClientInfo;

            if (item == null)
                return;

            draggingClient = item;
            DragDropEffects effect = DoDragDrop(item.Name.ToString(), DragDropEffects.All);
        }

        private void SelectedClientListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (selectedClientListBox.SelectedItem == null)
                return;

            ClientInfo client = (ClientInfo)selectedClientListBox.SelectedItem;

            selectedClient = client;
            PopulateListBoxes();
        }

        private void DisplayConfigEditor_Load(object sender, EventArgs e)
        {

        }

        private void PopulateListBoxes()
        {
            selectedClientListBox.Items.Clear();
            otherClientsListBox.Items.Clear();

            foreach(var client in Clients)
            {
                selectedClientListBox.Items.Add(client);
                
                if ( selectedClient != null && selectedClient != client)
                    otherClientsListBox.Items.Add(client);
            }

            otherClientsListBox.Items.Add(ClientInfo.None);

            if (selectedClient == null)
            {
                clientHeaderLabel.Text = "Select a client";
                return;
            }
            else
            {
                clientHeaderLabel.Text = "Edges of " + selectedClient.Name;
            }

            if (selectedClient.LeftClient == null)
                leftLabel.Text = "Left: None";
            else
                leftLabel.Text = "Left: " + selectedClient.LeftClient.Name;

            if (selectedClient.RightClient == null)
                rightLabel.Text = "Right: None";
            else
                rightLabel.Text = "Right: " + selectedClient.RightClient.Name;

            if (selectedClient.TopClient == null)
                topLabel.Text = "Top: None";
            else
                topLabel.Text = "Top: " + selectedClient.TopClient.Name;

            if (selectedClient.BottomClient == null)
                bottomLabel.Text = "Bottom: None";
            else
                bottomLabel.Text = "Bottom: " + selectedClient.BottomClient.Name;
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
