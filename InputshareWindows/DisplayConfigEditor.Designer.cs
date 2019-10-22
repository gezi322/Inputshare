namespace InputshareWindows
{
    partial class DisplayConfigEditor
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
            this.selectedClientListBox = new System.Windows.Forms.ListBox();
            this.leftLabel = new System.Windows.Forms.Label();
            this.topLabel = new System.Windows.Forms.Label();
            this.rightLabel = new System.Windows.Forms.Label();
            this.bottomLabel = new System.Windows.Forms.Label();
            this.otherClientsListBox = new System.Windows.Forms.ListBox();
            this.applyButton = new System.Windows.Forms.Button();
            this.clientHeaderLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            // 
            // selectedClientListBox
            // 
            this.selectedClientListBox.FormattingEnabled = true;
            this.selectedClientListBox.ItemHeight = 15;
            this.selectedClientListBox.Location = new System.Drawing.Point(9, 7);
            this.selectedClientListBox.Name = "selectedClientListBox";
            this.selectedClientListBox.Size = new System.Drawing.Size(128, 139);
            this.selectedClientListBox.TabIndex = 0;
            // 
            // leftLabel
            // 
            this.leftLabel.AllowDrop = true;
            this.leftLabel.AutoSize = true;
            this.leftLabel.Location = new System.Drawing.Point(144, 50);
            this.leftLabel.Name = "leftLabel";
            this.leftLabel.Size = new System.Drawing.Size(62, 15);
            this.leftLabel.TabIndex = 1;
            this.leftLabel.Text = "Left: None";
            // 
            // topLabel
            // 
            this.topLabel.AllowDrop = true;
            this.topLabel.AutoSize = true;
            this.topLabel.Location = new System.Drawing.Point(145, 106);
            this.topLabel.Name = "topLabel";
            this.topLabel.Size = new System.Drawing.Size(61, 15);
            this.topLabel.TabIndex = 1;
            this.topLabel.Text = "Top: None";
            // 
            // rightLabel
            // 
            this.rightLabel.AllowDrop = true;
            this.rightLabel.AutoSize = true;
            this.rightLabel.Location = new System.Drawing.Point(144, 77);
            this.rightLabel.Name = "rightLabel";
            this.rightLabel.Size = new System.Drawing.Size(70, 15);
            this.rightLabel.TabIndex = 1;
            this.rightLabel.Text = "Right: None";
            // 
            // bottomLabel
            // 
            this.bottomLabel.AllowDrop = true;
            this.bottomLabel.AutoSize = true;
            this.bottomLabel.Location = new System.Drawing.Point(143, 131);
            this.bottomLabel.Name = "bottomLabel";
            this.bottomLabel.Size = new System.Drawing.Size(82, 15);
            this.bottomLabel.TabIndex = 1;
            this.bottomLabel.Text = "Bottom: None";
            // 
            // otherClientsListBox
            // 
            this.otherClientsListBox.FormattingEnabled = true;
            this.otherClientsListBox.ItemHeight = 15;
            this.otherClientsListBox.Location = new System.Drawing.Point(277, 7);
            this.otherClientsListBox.Name = "otherClientsListBox";
            this.otherClientsListBox.Size = new System.Drawing.Size(128, 139);
            this.otherClientsListBox.TabIndex = 0;
            // 
            // applyButton
            // 
            this.applyButton.Location = new System.Drawing.Point(9, 185);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(396, 38);
            this.applyButton.TabIndex = 2;
            this.applyButton.Text = "Apply";
            this.applyButton.UseVisualStyleBackColor = true;
            this.applyButton.Click += new System.EventHandler(this.applyButton_Click);
            // 
            // clientHeaderLabel
            // 
            this.clientHeaderLabel.AutoSize = true;
            this.clientHeaderLabel.Location = new System.Drawing.Point(143, 7);
            this.clientHeaderLabel.Name = "clientHeaderLabel";
            this.clientHeaderLabel.Size = new System.Drawing.Size(79, 15);
            this.clientHeaderLabel.TabIndex = 3;
            this.clientHeaderLabel.Text = "Select a client";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 167);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(396, 15);
            this.label1.TabIndex = 4;
            this.label1.Text = "Select a client from the left and drag clients from the right to assign edges";
            // 
            // DisplayConfigEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(413, 225);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.clientHeaderLabel);
            this.Controls.Add(this.applyButton);
            this.Controls.Add(this.leftLabel);
            this.Controls.Add(this.selectedClientListBox);
            this.Controls.Add(this.topLabel);
            this.Controls.Add(this.rightLabel);
            this.Controls.Add(this.bottomLabel);
            this.Controls.Add(this.otherClientsListBox);
            this.Name = "DisplayConfigEditor";
            this.Text = "DisplayConfigEditor";
            this.Load += new System.EventHandler(this.DisplayConfigEditor_Load);

        }

        #endregion

        private System.Windows.Forms.ListBox selectedClientListBox;
        private System.Windows.Forms.Label leftLabel;
        private System.Windows.Forms.Label topLabel;
        private System.Windows.Forms.Label rightLabel;
        private System.Windows.Forms.Label bottomLabel;
        private System.Windows.Forms.ListBox otherClientsListBox;
        private System.Windows.Forms.Button applyButton;
        private System.Windows.Forms.Label clientHeaderLabel;
        private System.Windows.Forms.Label label1;
    }
}