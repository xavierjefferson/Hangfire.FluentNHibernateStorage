namespace Hangfire.FluentNHibernate.SampleApplication
{
    partial class HQLForm
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
            this.HQLQueryTextBox = new System.Windows.Forms.TextBox();
            this.ExecuteButton = new System.Windows.Forms.Button();
            this.QueryResponseTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Enter HQL Query";
            // 
            // HQLQueryTextBox
            // 
            this.HQLQueryTextBox.Location = new System.Drawing.Point(14, 35);
            this.HQLQueryTextBox.Multiline = true;
            this.HQLQueryTextBox.Name = "HQLQueryTextBox";
            this.HQLQueryTextBox.Size = new System.Drawing.Size(452, 105);
            this.HQLQueryTextBox.TabIndex = 1;
            // 
            // ExecuteButton
            // 
            this.ExecuteButton.Location = new System.Drawing.Point(359, 146);
            this.ExecuteButton.Name = "ExecuteButton";
            this.ExecuteButton.Size = new System.Drawing.Size(107, 34);
            this.ExecuteButton.TabIndex = 2;
            this.ExecuteButton.Text = "Execute";
            this.ExecuteButton.UseVisualStyleBackColor = true;
            this.ExecuteButton.Click += new System.EventHandler(this.ExecuteButton_Click);
            // 
            // QueryResponseTextBox
            // 
            this.QueryResponseTextBox.Location = new System.Drawing.Point(15, 232);
            this.QueryResponseTextBox.Multiline = true;
            this.QueryResponseTextBox.Name = "QueryResponseTextBox";
            this.QueryResponseTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.QueryResponseTextBox.Size = new System.Drawing.Size(452, 105);
            this.QueryResponseTextBox.TabIndex = 3;
            // 
            // HQLForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(490, 394);
            this.Controls.Add(this.QueryResponseTextBox);
            this.Controls.Add(this.ExecuteButton);
            this.Controls.Add(this.HQLQueryTextBox);
            this.Controls.Add(this.label1);
            this.Name = "HQLForm";
            this.Text = "HQLForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox HQLQueryTextBox;
        private System.Windows.Forms.Button ExecuteButton;
        private System.Windows.Forms.TextBox QueryResponseTextBox;
    }
}