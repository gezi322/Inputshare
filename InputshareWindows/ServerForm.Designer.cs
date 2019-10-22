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
            this.label1 = new System.Windows.Forms.Label();
            this.clipboardDataTypeLabel = new System.Windows.Forms.Label();
            this.clipboardHostLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.realtimeMouseRadioButton = new System.Windows.Forms.RadioButton();
            this.bufferedMouseRadioButton = new System.Windows.Forms.RadioButton();
            this.mouseUpdateRateTextBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
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
            this.consoleRichTextBox.Location = new System.Drawing.Point(12, 170);
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
            this.hotkeyListBox.Size = new System.Drawing.Size(191, 109);
            this.hotkeyListBox.TabIndex = 4;
            this.hotkeyListBox.SelectedIndexChanged += new System.EventHandler(this.hotkeyListBox_SelectedIndexChanged);
            // 
            // clientListBox
            // 
            this.clientListBox.FormattingEnabled = true;
            this.clientListBox.ItemHeight = 15;
            this.clientListBox.Location = new System.Drawing.Point(358, 37);
            this.clientListBox.Name = "clientListBox";
            this.clientListBox.Size = new System.Drawing.Size(191, 109);
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
            this.displayConfigEditButton.Location = new System.Drawing.Point(12, 141);
            this.displayConfigEditButton.Name = "displayConfigEditButton";
            this.displayConfigEditButton.Size = new System.Drawing.Size(143, 23);
            this.displayConfigEditButton.TabIndex = 9;
            this.displayConfigEditButton.Text = "Edit display config";
            this.displayConfigEditButton.UseVisualStyleBackColor = true;
            this.displayConfigEditButton.Visible = false;
            this.displayConfigEditButton.Click += new System.EventHandler(this.displayConfigEditButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(584, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(123, 15);
            this.label1.TabIndex = 10;
            this.label1.Text = "Global clipboard data:";
            // 
            // clipboardDataTypeLabel
            // 
            this.clipboardDataTypeLabel.AutoSize = true;
            this.clipboardDataTypeLabel.Location = new System.Drawing.Point(555, 45);
            this.clipboardDataTypeLabel.Name = "clipboardDataTypeLabel";
            this.clipboardDataTypeLabel.Size = new System.Drawing.Size(63, 15);
            this.clipboardDataTypeLabel.TabIndex = 11;
            this.clipboardDataTypeLabel.Text = "Data type: ";
            // 
            // clipboardHostLabel
            // 
            this.clipboardHostLabel.AutoSize = true;
            this.clipboardHostLabel.Location = new System.Drawing.Point(555, 70);
            this.clipboardHostLabel.Name = "clipboardHostLabel";
            this.clipboardHostLabel.Size = new System.Drawing.Size(32, 15);
            this.clipboardHostLabel.TabIndex = 12;
            this.clipboardHostLabel.Text = "Host";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(261, 149);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(166, 15);
            this.label2.TabIndex = 13;
            this.label2.Text = "Double click to assign hotkeys";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(556, 103);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(111, 15);
            this.label3.TabIndex = 14;
            this.label3.Text = "Mouse input mode:";
            // 
            // realtimeMouseRadioButton
            // 
            this.realtimeMouseRadioButton.AutoSize = true;
            this.realtimeMouseRadioButton.Location = new System.Drawing.Point(556, 127);
            this.realtimeMouseRadioButton.Name = "realtimeMouseRadioButton";
            this.realtimeMouseRadioButton.Size = new System.Drawing.Size(71, 19);
            this.realtimeMouseRadioButton.TabIndex = 15;
            this.realtimeMouseRadioButton.TabStop = true;
            this.realtimeMouseRadioButton.Text = "Realtime";
            this.realtimeMouseRadioButton.UseVisualStyleBackColor = true;
            this.realtimeMouseRadioButton.CheckedChanged += new System.EventHandler(this.realtimeMouseRadioButton_CheckedChanged);
            this.realtimeMouseRadioButton.MouseClick += new System.Windows.Forms.MouseEventHandler(this.realtimeMouseRadioButton_MouseClick);
            // 
            // bufferedMouseRadioButton
            // 
            this.bufferedMouseRadioButton.AutoSize = true;
            this.bufferedMouseRadioButton.Location = new System.Drawing.Point(556, 152);
            this.bufferedMouseRadioButton.Name = "bufferedMouseRadioButton";
            this.bufferedMouseRadioButton.Size = new System.Drawing.Size(70, 19);
            this.bufferedMouseRadioButton.TabIndex = 15;
            this.bufferedMouseRadioButton.TabStop = true;
            this.bufferedMouseRadioButton.Text = "Buffered";
            this.bufferedMouseRadioButton.UseVisualStyleBackColor = true;
            this.bufferedMouseRadioButton.CheckedChanged += new System.EventHandler(this.bufferedMouseRadioButton_CheckedChanged);
            this.bufferedMouseRadioButton.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bufferedMouseRadioButton_MouseClick);
            // 
            // mouseUpdateRateTextBox
            // 
            this.mouseUpdateRateTextBox.Location = new System.Drawing.Point(556, 196);
            this.mouseUpdateRateTextBox.Name = "mouseUpdateRateTextBox";
            this.mouseUpdateRateTextBox.Size = new System.Drawing.Size(31, 23);
            this.mouseUpdateRateTextBox.TabIndex = 16;
            this.mouseUpdateRateTextBox.Text = "60";
            this.mouseUpdateRateTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBox1_KeyPress);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(556, 178);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(137, 15);
            this.label4.TabIndex = 17;
            this.label4.Text = "Update rate (per second)";
            // 
            // ServerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(747, 338);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.mouseUpdateRateTextBox);
            this.Controls.Add(this.realtimeMouseRadioButton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.clipboardHostLabel);
            this.Controls.Add(this.clipboardDataTypeLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.displayConfigEditButton);
            this.Controls.Add(this.portLabel);
            this.Controls.Add(this.portTextBox);
            this.Controls.Add(this.hotkeyLabel);
            this.Controls.Add(this.clientListBox);
            this.Controls.Add(this.hotkeyListBox);
            this.Controls.Add(this.clientListLabel);
            this.Controls.Add(this.consoleRichTextBox);
            this.Controls.Add(this.startServerButton);
            this.Controls.Add(this.bufferedMouseRadioButton);
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
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label clipboardDataTypeLabel;
        private System.Windows.Forms.Label clipboardHostLabel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RadioButton realtimeMouseRadioButton;
        private System.Windows.Forms.RadioButton bufferedMouseRadioButton;
        private System.Windows.Forms.TextBox mouseUpdateRateTextBox;
        private System.Windows.Forms.Label label4;
    }
}