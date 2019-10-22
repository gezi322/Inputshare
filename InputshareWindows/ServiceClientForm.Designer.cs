namespace InputshareWindows
{
    partial class ServiceClientForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.addressTextBox = new System.Windows.Forms.TextBox();
            this.portTextBox = new System.Windows.Forms.TextBox();
            this.connectButton = new System.Windows.Forms.Button();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.autoReconnectCheckBox = new System.Windows.Forms.CheckBox();
            // 
            // addressTextBox
            // 
            this.addressTextBox.Location = new System.Drawing.Point(8, 7);
            this.addressTextBox.Name = "addressTextBox";
            this.addressTextBox.Size = new System.Drawing.Size(100, 23);
            this.addressTextBox.TabIndex = 1;
            this.addressTextBox.Visible = false;
            // 
            // portTextBox
            // 
            this.portTextBox.Location = new System.Drawing.Point(114, 7);
            this.portTextBox.Name = "portTextBox";
            this.portTextBox.Size = new System.Drawing.Size(55, 23);
            this.portTextBox.TabIndex = 2;
            this.portTextBox.Text = "4441";
            this.portTextBox.Visible = false;
            // 
            // connectButton
            // 
            this.connectButton.Location = new System.Drawing.Point(8, 65);
            this.connectButton.Name = "connectButton";
            this.connectButton.Size = new System.Drawing.Size(75, 23);
            this.connectButton.TabIndex = 3;
            this.connectButton.Text = "Connect";
            this.connectButton.UseVisualStyleBackColor = true;
            this.connectButton.Visible = false;
            this.connectButton.Click += new System.EventHandler(this.Button2_Click);
            // 
            // nameTextBox
            // 
            this.nameTextBox.Location = new System.Drawing.Point(8, 36);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(100, 23);
            this.nameTextBox.TabIndex = 4;
            this.nameTextBox.Visible = false;
            // 
            // autoReconnectCheckBox
            // 
            this.autoReconnectCheckBox.AutoSize = true;
            this.autoReconnectCheckBox.Location = new System.Drawing.Point(89, 69);
            this.autoReconnectCheckBox.Name = "autoReconnectCheckBox";
            this.autoReconnectCheckBox.Size = new System.Drawing.Size(108, 19);
            this.autoReconnectCheckBox.TabIndex = 5;
            this.autoReconnectCheckBox.Text = "Auto reconnect";
            this.autoReconnectCheckBox.UseVisualStyleBackColor = true;
            this.autoReconnectCheckBox.Visible = false;
            this.autoReconnectCheckBox.CheckedChanged += new System.EventHandler(this.AutoReconnectCheckBox_CheckedChanged);
            // 
            // ServiceClientForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(198, 100);
            this.Controls.Add(this.autoReconnectCheckBox);
            this.Controls.Add(this.nameTextBox);
            this.Controls.Add(this.connectButton);
            this.Controls.Add(this.portTextBox);
            this.Controls.Add(this.addressTextBox);
            this.Name = "ServiceClientForm";
            this.Text = "ServiceClientForm";
            this.Load += new System.EventHandler(this.ServiceClientForm_Load);

        }

        #endregion
        private System.Windows.Forms.TextBox addressTextBox;
        private System.Windows.Forms.TextBox portTextBox;
        private System.Windows.Forms.Button connectButton;
        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.CheckBox autoReconnectCheckBox;
    }
}