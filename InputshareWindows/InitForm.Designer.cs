namespace InputshareWindows
{
    partial class InitForm
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
            this.serverButton = new System.Windows.Forms.Button();
            this.buttonClient = new System.Windows.Forms.Button();
            this.buttonServiceClient = new System.Windows.Forms.Button();
            // 
            // serverButton
            // 
            this.serverButton.Location = new System.Drawing.Point(4, 3);
            this.serverButton.Name = "serverButton";
            this.serverButton.Size = new System.Drawing.Size(148, 23);
            this.serverButton.TabIndex = 0;
            this.serverButton.Text = "Server";
            this.serverButton.UseVisualStyleBackColor = true;
            this.serverButton.Click += new System.EventHandler(this.serverButton_Click);
            // 
            // buttonClient
            // 
            this.buttonClient.Location = new System.Drawing.Point(4, 32);
            this.buttonClient.Name = "buttonClient";
            this.buttonClient.Size = new System.Drawing.Size(148, 23);
            this.buttonClient.TabIndex = 0;
            this.buttonClient.Text = "Client";
            this.buttonClient.UseVisualStyleBackColor = true;
            this.buttonClient.Click += new System.EventHandler(this.buttonClient_Click);
            // 
            // buttonServiceClient
            // 
            this.buttonServiceClient.Location = new System.Drawing.Point(4, 61);
            this.buttonServiceClient.Name = "buttonServiceClient";
            this.buttonServiceClient.Size = new System.Drawing.Size(148, 23);
            this.buttonServiceClient.TabIndex = 0;
            this.buttonServiceClient.Text = "Client (Service)";
            this.buttonServiceClient.UseVisualStyleBackColor = true;
            this.buttonServiceClient.Click += new System.EventHandler(this.buttonServiceClient_Click);
            // 
            // InitForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(156, 91);
            this.Controls.Add(this.serverButton);
            this.Controls.Add(this.buttonClient);
            this.Controls.Add(this.buttonServiceClient);
            this.Name = "InitForm";
            this.Text = "Inputshare";
            this.Load += new System.EventHandler(this.InitForm_Load);

        }

        #endregion

        private System.Windows.Forms.Button serverButton;
        private System.Windows.Forms.Button buttonClient;
        private System.Windows.Forms.Button buttonServiceClient;
    }
}