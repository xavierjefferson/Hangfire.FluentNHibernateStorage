using System;
using System.ComponentModel;
using System.Windows.Forms;
using Hangfire.FluentNHibernate.SampleStuff;
using Hangfire.FluentNHibernateStorage;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace Hangfire.FluentNHibernate.WinformsApplication
{
    public partial class Form1 : Form
    {
        public enum StateEnum
        {
            Stopped = 0,
            Starting,
            Started
        }

        private readonly object _lockObj = new object();

        private readonly ILogEventEmitterService _logEventEmitterService;

        private readonly ILogger<Form1> _logger;
        private readonly ISqliteTempFileService _sqliteTempFileService;
        private readonly IServiceProvider serviceProvider;


        private BackgroundJobServer _backgroundJobServer;

        private StateEnum _currentState;

        private FluentNHibernateJobStorage _storage;

        public Form1(ILogger<Form1> logger, ILogEventEmitterService logEventEmitterService,
            ISqliteTempFileService sqliteTempFileService, IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            _sqliteTempFileService = sqliteTempFileService;
            _logger = logger;
            _logEventEmitterService = logEventEmitterService;
            _logEventEmitterService.OnEmit += LogEventEmitterServiceOnEmit;
            InitializeComponent();
            SetupDataGridView();
        }

        private StateEnum State
        {
            get => _currentState;
            set
            {
                _currentState = value;
                StartButton.Enabled = value == StateEnum.Stopped;

                StopButton.Enabled = value == StateEnum.Started;
            }
        }

        public BindingList<LogEventWrapper> LoggingEvents { get; } = new BindingList<LogEventWrapper>();

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _logEventEmitterService.OnEmit -= LogEventEmitterServiceOnEmit;

            base.OnFormClosing(e);
        }


        private void SetupDataGridView()
        {
            LoggerDataGridView.AutoGenerateColumns = false;
            AddColumn(nameof(LogEventWrapper.TimeStamp), "Timestamp",
                column => { column.DefaultCellStyle.Format = "yyyy-MM-dd hh:mm:ss"; });
            AddColumn(nameof(LogEventWrapper.ThreadName), null,
                column => { });
            AddColumn(nameof(LogEventWrapper.Level), null,
                column => { });
            AddColumn(nameof(LogEventWrapper.Message), "Message",
                column => { });
            AddColumn(nameof(LogEventWrapper.LoggerName), null,
                column => { });
            AddColumn(nameof(LogEventWrapper.Exception), null,
                column => { });
            LoggerDataGridView.DataSource = LoggingEvents;
        }

        public void DoAppend(LogEvent loggingEvent)
        {
            try
            {
                lock (_lockObj)
                {
                    LoggingEvents.Add(new LogEventWrapper(loggingEvent, null));
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
            LoggerDataGridView.Columns.Add(column);
            action?.Invoke(column);
        }

        public void DoRefresh()
        {
            if (LoggerDataGridView.InvokeRequired)
            {
                LoggerDataGridView.Invoke(new Action(DoRefresh));
            }
            else
            {
                LoggerDataGridView.Refresh();
                if (AutoScrollCheckBox.Checked)
                {
                    LoggerDataGridView.FirstDisplayedScrollingRowIndex = LoggerDataGridView.RowCount - 1;
                    LoggerDataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                }
            }
        }

        private void LogEventEmitterServiceOnEmit(LogEvent l)
        {
            DoAppend(l);
            DoRefresh();
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            _backgroundJobServer?.SendStop();
            base.OnClosing(e);
        }


        private void Form1_Load(object sender, EventArgs e1)
        {
            _logger.LogError("This is an intentional exception", new Exception("Intentional exception object"));
            State = StateEnum.Stopped;
        }


        private void StartButton_Click(object sender, EventArgs e)
        {
            try
            {
                var connectionString = _sqliteTempFileService.GetConnectionString();


                //THIS LINE GETS THE STORAGE PROVIDER

                State = StateEnum.Starting;
                //THIS LINE CONFIGURES HANGFIRE WITH THE STORAGE PROVIDER
                GlobalConfiguration.Configuration.SetupJobStorage(_sqliteTempFileService)
                    .SetupActivator(serviceProvider);
                /*THIS LINE STARTS THE BACKGROUND SERVER*/
                _storage = JobStorage.Current as FluentNHibernateJobStorage;
                _backgroundJobServer = new BackgroundJobServer(new BackgroundJobServerOptions(), _storage,
                    _storage.GetBackgroundProcesses());


                JobMethods.CreateRecurringJobs(_logger);
                _logger.LogInformation("Background server started");
                State = StateEnum.Started;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server start failed");
                StopButton_Click(null, new EventArgs());
                State = StateEnum.Stopped;
                _storage?.Dispose();
                _storage = null;
            }
        }


        private void StopButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_backgroundJobServer != null)
                {
                    _backgroundJobServer.SendStop();
                    _backgroundJobServer.Dispose();
                }

                _backgroundJobServer = null;
                State = StateEnum.Stopped;
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Error during stop", ex);
            }
        }
    }
}