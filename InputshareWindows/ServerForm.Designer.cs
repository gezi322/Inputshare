namespace InputshareWindows
{
    partial class ServerForm
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
            this.startServerButton = new System.Windows.Forms.Button();
            this.consoleRichTextBox = new System.Windows.Forms.RichTextBox();
            this.clientListLabel = new System.Windows.Forms.Label();
            this.hotkeyListBox = new System.Windows.Forms.ListBox();
            this.clientListBox = new System.Windows.Forms.ListBox();
            this.hotkeyLabel = new System.Windows.Forms.Label();
            this.portTextBox = new System.Windows.Forms.TextBox();
            this.portLabel = new System.Windows.Forms.Label();
            this.displayConfigEditButton = new System.Windows.Forms.Button();
            // 
            // startServerButton
            // 
            this.startServerButton.Location = new System.Drawing.Point(12, 66);
            this.startServerButton.Name = "startServerButton";
            this.startServerButton.Size = new System.Drawing.Size(143, 23);
            this.startServerButton.TabIndex = 0;
            this.startServerButton.Text = "Start server";
            this.startServerButton.UseVisualStyleBackColor = true;
            this.startServerButton.Click += new System.EventHandler(this.StartServerButton_Click);
            // 
            // consoleRichTextBox
            // 
            this.consoleRichTextBox.Location = new System.Drawing.Point(12, 173);
            this.consoleRichTextBox.Name = "consoleRichTextBox";
            this.consoleRichTextBox.ReadOnly = true;
            this.consoleRichTextBox.Size = new System.Drawing.Size(537, 155);
            this.consoleRichTextBox.TabIndex = 1;
            this.consoleRichTextBox.Text = "";
            // 
            // clientListLabel
            // 
            this.clientListLabel.AutoSize = true;
            this.clientListLabel.Location = new System.Drawing.Point(390, 15);
            this.clientListLabel.Name = "clientListLabel";
            this.clientListLabel.Size = new System.Drawing.Size(105, 15);
            this.clientListLabel.TabIndex = 3;
            this.clientListLabel.Text = "Connected clients:";
            this.clientListLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // hotkeyListBox
            // 
            this.hotkeyListBox.FormattingEnabled = true;
            this.hotkeyListBox.ItemHeight = 15;
            this.hotkeyListBox.Location = new System.Drawing.Point(161, 37);
            this.hotkeyListBox.Name = "hotkeyListBox";
            this.hotkeyListBox.Size = new System.Drawing.Size(191, 124);
            this.hotkeyListBox.TabIndex = 4;
            this.hotkeyListBox.SelectedIndexChanged += new System.EventHandler(this.hotkeyListBox_SelectedIndexChanged);
            // 
            // clientListBox
            // 
            this.clientListBox.FormattingEnabled = true;
            this.clientListBox.ItemHeight = 15;
            this.clientListBox.Location = new System.Drawing.Point(358, 37);
            this.clientListBox.Name = "clientListBox";
            this.clientListBox.Size = new System.Drawing.Size(191, 124);
            this.clientListBox.TabIndex = 5;
            // 
            // hotkeyLabel
            // 
            this.hotkeyLabel.AutoSize = true;
            this.hotkeyLabel.Location = new System.Drawing.Point(221, 15);
            this.hotkeyLabel.Name = "hotkeyLabel";
            this.hotkeyLabel.Size = new System.Drawing.Size(53, 15);
            this.hotkeyLabel.TabIndex = 6;
            this.hotkeyLabel.Text = "Hotkeys:";
            // 
            // portTextBox
            // 
            this.portTextBox.Location = new System.Drawing.Point(74, 37);
            this.portTextBox.Name = "portTextBox";
            this.portTextBox.Size = new System.Drawing.Size(81, 23);
            this.portTextBox.TabIndex = 7;
            this.portTextBox.Text = "4441";
            // 
            // portLabel
            // 
            this.portLabel.AutoSize = true;
            this.portLabel.Location = new System.Drawing.Point(36, 40);
            this.portLabel.Name = "portLabel";
            this.portLabel.Size = new System.Drawing.Size(32, 15);
            this.portLabel.TabIndex = 8;
            this.portLabel.Text = "Port:";
            // 
            // displayConfigEditButton
            // 
            this.displayConfigEditButton.Location = new System.Drawing.Point(12, 138);
            this.displayConfigEditButton.Name = "displayConfigEditButton";
            this.displayConfigEditButton.Size = new System.Drawing.Size(143, 23);
            this.displayConfigEditButton.TabIndex = 9;
            this.displayConfigEditButton.Text = "Edit display config";
            this.displayConfigEditButton.UseVisualStyleBackColor = true;
            this.displayConfigEditButton.Visible = false;
            this.displayConfigEditButton.Click += new System.EventHandler(this.displayConfigEditButton_Click);
            // 
            // ServerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(563, 338);
            this.Controls.Add(this.displayConfigEditButton);
            this.Controls.Add(this.portLabel);
            this.Controls.Add(this.portTextBox);
            this.Controls.Add(this.hotkeyLabel);
            this.Controls.Add(this.clientListBox);
            this.Controls.Add(this.hotkeyListBox);
            this.Controls.Add(this.clientListLabel);
            this.Controls.Add(this.consoleRichTextBox);
            this.Controls.Add(this.startServerButton);
            this.Name = "ServerForm";
            this.Text = "Inputshare Server";
            this.Load += new System.EventHandler(this.MainForm_Load);

        }

        #endregion

        private System.Windows.Forms.Button startServerButton;
        private System.Windows.Forms.RichTextBox consoleRichTextBox;
        private System.Windows.Forms.Label clientListLabel;
        private System.Windows.Forms.ListBox hotkeyListBox;
        private System.Windows.Forms.ListBox clientListBox;
        private System.Windows.Forms.Label hotkeyLabel;
        private System.Windows.Forms.TextBox portTextBox;
        private System.Windows.Forms.Label portLabel;
        private System.Windows.Forms.Button displayConfigEditButton;
    }
}