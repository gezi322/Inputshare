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
            // 
            // selectedClientListBox
            // 
            this.selectedClientListBox.FormattingEnabled = true;
            this.selectedClientListBox.ItemHeight = 15;
            this.selectedClientListBox.Location = new System.Drawing.Point(9, 7);
            this.selectedClientListBox.Name = "selectedClientListBox";
            this.selectedClientListBox.Size = new System.Drawing.Size(128, 109);
            this.selectedClientListBox.TabIndex = 0;
            // 
            // leftLabel
            // 
            this.leftLabel.AllowDrop = true;
            this.leftLabel.AutoSize = true;
            this.leftLabel.Location = new System.Drawing.Point(143, 7);
            this.leftLabel.Name = "leftLabel";
            this.leftLabel.Size = new System.Drawing.Size(62, 15);
            this.leftLabel.TabIndex = 1;
            this.leftLabel.Text = "Left: None";
            // 
            // topLabel
            // 
            this.topLabel.AllowDrop = true;
            this.topLabel.AutoSize = true;
            this.topLabel.Location = new System.Drawing.Point(143, 67);
            this.topLabel.Name = "topLabel";
            this.topLabel.Size = new System.Drawing.Size(61, 15);
            this.topLabel.TabIndex = 1;
            this.topLabel.Text = "Top: None";
            // 
            // rightLabel
            // 
            this.rightLabel.AllowDrop = true;
            this.rightLabel.AutoSize = true;
            this.rightLabel.Location = new System.Drawing.Point(143, 37);
            this.rightLabel.Name = "rightLabel";
            this.rightLabel.Size = new System.Drawing.Size(70, 15);
            this.rightLabel.TabIndex = 1;
            this.rightLabel.Text = "Right: None";
            // 
            // bottomLabel
            // 
            this.bottomLabel.AllowDrop = true;
            this.bottomLabel.AutoSize = true;
            this.bottomLabel.Location = new System.Drawing.Point(143, 101);
            this.bottomLabel.Name = "bottomLabel";
            this.bottomLabel.Size = new System.Drawing.Size(82, 15);
            this.bottomLabel.TabIndex = 1;
            this.bottomLabel.Text = "Bottom: None";
            // 
            // otherClientsListBox
            // 
            this.otherClientsListBox.FormattingEnabled = true;
            this.otherClientsListBox.ItemHeight = 15;
            this.otherClientsListBox.Location = new System.Drawing.Point(273, 7);
            this.otherClientsListBox.Name = "otherClientsListBox";
            this.otherClientsListBox.Size = new System.Drawing.Size(128, 109);
            this.otherClientsListBox.TabIndex = 0;
            // 
            // applyButton
            // 
            this.applyButton.Location = new System.Drawing.Point(9, 122);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(392, 23);
            this.applyButton.TabIndex = 2;
            this.applyButton.Text = "Apply";
            this.applyButton.UseVisualStyleBackColor = true;
            this.applyButton.Click += new System.EventHandler(this.applyButton_Click);
            // 
            // DisplayConfigEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(406, 153);
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
    }
}