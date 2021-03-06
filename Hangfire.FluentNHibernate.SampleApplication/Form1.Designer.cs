﻿namespace Hangfire.FluentNHibernate.SampleApplication
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.label1 = new System.Windows.Forms.Label();
            this.ConnectionStringTextBox = new System.Windows.Forms.TextBox();
            this.StartButton = new System.Windows.Forms.Button();
            this.StopButton = new System.Windows.Forms.Button();
            this.DataProviderComboBox = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.HQLButton = new System.Windows.Forms.Button();
            this.TableNamePrefixTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.LoggerDataGridView = new System.Windows.Forms.DataGridView();
            this.AutoScrollCheckBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.LoggerDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 43);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Connection String";
            // 
            // ConnectionStringTextBox
            // 
            this.ConnectionStringTextBox.Location = new System.Drawing.Point(134, 40);
            this.ConnectionStringTextBox.Multiline = true;
            this.ConnectionStringTextBox.Name = "ConnectionStringTextBox";
            this.ConnectionStringTextBox.Size = new System.Drawing.Size(342, 76);
            this.ConnectionStringTextBox.TabIndex = 1;
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
            // DataProviderComboBox
            // 
            this.DataProviderComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DataProviderComboBox.FormattingEnabled = true;
            this.DataProviderComboBox.Location = new System.Drawing.Point(134, 13);
            this.DataProviderComboBox.Name = "DataProviderComboBox";
            this.DataProviderComboBox.Size = new System.Drawing.Size(342, 21);
            this.DataProviderComboBox.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 13);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Data Provider";
            // 
            // HQLButton
            // 
            this.HQLButton.Location = new System.Drawing.Point(500, 92);
            this.HQLButton.Name = "HQLButton";
            this.HQLButton.Size = new System.Drawing.Size(79, 30);
            this.HQLButton.TabIndex = 8;
            this.HQLButton.Text = "HQL...";
            this.HQLButton.UseVisualStyleBackColor = true;
            this.HQLButton.Click += new System.EventHandler(this.HQLButton_Click);
            // 
            // TableNamePrefixTextBox
            // 
            this.TableNamePrefixTextBox.Location = new System.Drawing.Point(134, 122);
            this.TableNamePrefixTextBox.Multiline = true;
            this.TableNamePrefixTextBox.Name = "TableNamePrefixTextBox";
            this.TableNamePrefixTextBox.Size = new System.Drawing.Size(342, 22);
            this.TableNamePrefixTextBox.TabIndex = 10;
            this.TableNamePrefixTextBox.Text = "Hangfire_";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 125);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(94, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Table Name Prefix";
            // 
            // LoggerDataGridView
            // 
            this.LoggerDataGridView.AllowUserToAddRows = false;
            this.LoggerDataGridView.AllowUserToDeleteRows = false;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.LoggerDataGridView.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle2;
            this.LoggerDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.LoggerDataGridView.Location = new System.Drawing.Point(31, 195);
            this.LoggerDataGridView.Name = "LoggerDataGridView";
            this.LoggerDataGridView.ReadOnly = true;
            this.LoggerDataGridView.Size = new System.Drawing.Size(677, 206);
            this.LoggerDataGridView.TabIndex = 5;
            // 
            // AutoScrollCheckBox
            // 
            this.AutoScrollCheckBox.AutoSize = true;
            this.AutoScrollCheckBox.Checked = true;
            this.AutoScrollCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.AutoScrollCheckBox.Location = new System.Drawing.Point(631, 153);
            this.AutoScrollCheckBox.Name = "AutoScrollCheckBox";
            this.AutoScrollCheckBox.Size = new System.Drawing.Size(77, 17);
            this.AutoScrollCheckBox.TabIndex = 11;
            this.AutoScrollCheckBox.Text = "Auto Scroll";
            this.AutoScrollCheckBox.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(749, 425);
            this.Controls.Add(this.AutoScrollCheckBox);
            this.Controls.Add(this.TableNamePrefixTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.HQLButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.DataProviderComboBox);
            this.Controls.Add(this.LoggerDataGridView);
            this.Controls.Add(this.StopButton);
            this.Controls.Add(this.StartButton);
            this.Controls.Add(this.ConnectionStringTextBox);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Hangfire Background Server";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.LoggerDataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox ConnectionStringTextBox;
        private System.Windows.Forms.Button StartButton;
        private System.Windows.Forms.Button StopButton;
        private System.Windows.Forms.DataGridView LoggerDataGridView;
        private System.Windows.Forms.ComboBox DataProviderComboBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button HQLButton;
        private System.Windows.Forms.TextBox TableNamePrefixTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox AutoScrollCheckBox;
    }
}

