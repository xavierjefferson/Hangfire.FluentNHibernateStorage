namespace Hangfire.FluentNHibernate.SampleApplication
{
    partial class Form1
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
            this.label1 = new System.Windows.Forms.Label();
            this.ConnectionStringTextBox = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.PostgreSQLRadioButton = new System.Windows.Forms.RadioButton();
            this.MySqlButton = new System.Windows.Forms.RadioButton();
            this.SqlServerButton = new System.Windows.Forms.RadioButton();
            this.StartButton = new System.Windows.Forms.Button();
            this.StopButton = new System.Windows.Forms.Button();
            this.LoggerTextBox = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Connection String";
            // 
            // ConnectionStringTextBox
            // 
            this.ConnectionStringTextBox.Location = new System.Drawing.Point(135, 19);
            this.ConnectionStringTextBox.Multiline = true;
            this.ConnectionStringTextBox.Name = "ConnectionStringTextBox";
            this.ConnectionStringTextBox.Size = new System.Drawing.Size(342, 76);
            this.ConnectionStringTextBox.TabIndex = 1;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.PostgreSQLRadioButton);
            this.groupBox1.Controls.Add(this.MySqlButton);
            this.groupBox1.Controls.Add(this.SqlServerButton);
            this.groupBox1.Location = new System.Drawing.Point(26, 106);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(450, 71);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Data Source";
            // 
            // PostgreSQLRadioButton
            // 
            this.PostgreSQLRadioButton.AutoSize = true;
            this.PostgreSQLRadioButton.Location = new System.Drawing.Point(233, 19);
            this.PostgreSQLRadioButton.Name = "PostgreSQLRadioButton";
            this.PostgreSQLRadioButton.Size = new System.Drawing.Size(74, 17);
            this.PostgreSQLRadioButton.TabIndex = 2;
            this.PostgreSQLRadioButton.TabStop = true;
            this.PostgreSQLRadioButton.Text = "Postgresql";
            this.PostgreSQLRadioButton.UseVisualStyleBackColor = true;
            // 
            // MySqlButton
            // 
            this.MySqlButton.AutoSize = true;
            this.MySqlButton.Location = new System.Drawing.Point(131, 19);
            this.MySqlButton.Name = "MySqlButton";
            this.MySqlButton.Size = new System.Drawing.Size(54, 17);
            this.MySqlButton.TabIndex = 1;
            this.MySqlButton.TabStop = true;
            this.MySqlButton.Text = "MySql";
            this.MySqlButton.UseVisualStyleBackColor = true;
            // 
            // SqlServerButton
            // 
            this.SqlServerButton.AutoSize = true;
            this.SqlServerButton.Location = new System.Drawing.Point(6, 19);
            this.SqlServerButton.Name = "SqlServerButton";
            this.SqlServerButton.Size = new System.Drawing.Size(74, 17);
            this.SqlServerButton.TabIndex = 0;
            this.SqlServerButton.TabStop = true;
            this.SqlServerButton.Text = "Sql Server";
            this.SqlServerButton.UseVisualStyleBackColor = true;
            // 
            // StartButton
            // 
            this.StartButton.Location = new System.Drawing.Point(500, 20);
            this.StartButton.Name = "StartButton";
            this.StartButton.Size = new System.Drawing.Size(79, 30);
            this.StartButton.TabIndex = 3;
            this.StartButton.Text = "Start";
            this.StartButton.UseVisualStyleBackColor = true;
            this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // StopButton
            // 
            this.StopButton.Enabled = false;
            this.StopButton.Location = new System.Drawing.Point(500, 56);
            this.StopButton.Name = "StopButton";
            this.StopButton.Size = new System.Drawing.Size(79, 30);
            this.StopButton.TabIndex = 4;
            this.StopButton.Text = "Stop";
            this.StopButton.UseVisualStyleBackColor = true;
            this.StopButton.Click += new System.EventHandler(this.StopButton_Click);
            // 
            // LoggerTextBox
            // 
            this.LoggerTextBox.BackColor = System.Drawing.Color.Black;
            this.LoggerTextBox.ForeColor = System.Drawing.Color.Lime;
            this.LoggerTextBox.Location = new System.Drawing.Point(31, 195);
            this.LoggerTextBox.Multiline = true;
            this.LoggerTextBox.Name = "LoggerTextBox";
            this.LoggerTextBox.ReadOnly = true;
            this.LoggerTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.LoggerTextBox.Size = new System.Drawing.Size(677, 206);
            this.LoggerTextBox.TabIndex = 5;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(749, 425);
            this.Controls.Add(this.LoggerTextBox);
            this.Controls.Add(this.StopButton);
            this.Controls.Add(this.StartButton);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.ConnectionStringTextBox);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Hangfire Background Server";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox ConnectionStringTextBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton PostgreSQLRadioButton;
        private System.Windows.Forms.RadioButton MySqlButton;
        private System.Windows.Forms.RadioButton SqlServerButton;
        private System.Windows.Forms.Button StartButton;
        private System.Windows.Forms.Button StopButton;
        private System.Windows.Forms.TextBox LoggerTextBox;
    }
}

