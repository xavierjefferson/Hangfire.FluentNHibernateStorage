﻿using System;
using System.ComponentModel;
using System.Windows.Forms;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace Hangfire.FluentNHibernate.SampleApplication
{
    public class DataGridViewAppender : IAppender
    {
        private readonly object _lockObj = new object();
        private readonly BindingList<LoggingEvent> _loggingEvents = new BindingList<LoggingEvent>();
        private CheckBox _autoScrollCheckBox;
        private DataGridView _dataGrid;

        public DataGridViewAppender(DataGridView dataGrid, CheckBox autoScrollCheckBox)
        {
            _autoScrollCheckBox = autoScrollCheckBox;
            var frm = dataGrid.FindForm();
            if (frm == null)
                return;
            frm.FormClosing += delegate { Close(); };
            _dataGrid = dataGrid;
            dataGrid.AutoGenerateColumns = false;
            _dataGrid.DataSource = _loggingEvents;

            AddColumn(nameof(LoggingEvent.TimeStamp), "Timestamp",
                column => { column.DefaultCellStyle.Format = "yyyy-MM-dd hh:mm:ss"; });
            AddColumn(nameof(LoggingEvent.ThreadName), null,
                column => { });
            AddColumn(nameof(LoggingEvent.Level), null,
                column => { });
            AddColumn(nameof(LoggingEvent.RenderedMessage), "Message",
                column => { });
            AddColumn(nameof(LoggingEvent.LoggerName), null,
                column => { });

            Name = "DataGridViewAppender";
        }

        public string Name { get; set; }

        public void Close()
        {
            try
            {
                // This locking is required to avoid null reference exceptions
                // in situations where DoAppend() is writing to the TextBox while
                // Close() is nulling out the TextBox.
                lock (_lockObj)
                {
                    _dataGrid = null;
                    _autoScrollCheckBox = null;
                }

                var hierarchy = (Hierarchy) LogManager.GetRepository();
                hierarchy.Root.RemoveAppender(this);
            }
            catch
            {
                // There is not much that can be done here, and
                // swallowing the error is desired in my situation.
            }
        }

        public void DoAppend(LoggingEvent loggingEvent)
        {
            try
            {
                if (_dataGrid == null)
                    return;

                lock (_lockObj)
                {
                    _loggingEvents.Add(loggingEvent);
                    DoRefresh();
                }

                Application.DoEvents();
            }
            catch
            {
                // There is not much that can be done here, and
                // swallowing the error is desired in my situation.
            }
        }

        private void AddColumn(string name, string headerText = null, Action<DataGridViewColumn> action = null)
        {
            DataGridViewColumn column = new DataGridViewTextBoxColumn();
            column.DataPropertyName = name;
            column.Name = name;
            column.HeaderText = headerText ?? name;
            _dataGrid.Columns.Add(column);
            action?.Invoke(column);
        }

        public void DoRefresh()
        {
            if (_dataGrid.InvokeRequired)
            {
                _dataGrid.Invoke(new Action(DoRefresh));
            }
            else
            {
                _dataGrid.Refresh();
                if (_autoScrollCheckBox.Checked)
                {
                    _dataGrid.FirstDisplayedScrollingRowIndex = _dataGrid.RowCount - 1;
                    _dataGrid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                }
            }
        }

        public static void ConfigureTextBoxAppender(DataGridView textBox, CheckBox autoScrollCheckBox)
        {
            var hierarchy = (Hierarchy) LogManager.GetRepository();
            var appender = new DataGridViewAppender(textBox, autoScrollCheckBox);

            hierarchy.Root.AddAppender(appender);
            hierarchy.Root.Level = Level.Debug;
            hierarchy.Configured = true;
        }
    }
}