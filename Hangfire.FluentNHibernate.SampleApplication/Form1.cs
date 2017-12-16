using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Hangfire.FluentNHibernate.SampleApplication.Properties;
using Hangfire.FluentNHibernateStorage;
using log4net;
using Timer = System.Timers.Timer;

namespace Hangfire.FluentNHibernate.SampleApplication
{
    public partial class Form1 : Form
    {
        private static readonly ILog loggerNew = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static FluentNHibernateStorage.FluentNHibernateStorage storage;
        private BackgroundJobServer _backgroundJobServer;

        private RadioButton[] radioButtons;
        private Timer t;


        public Form1()
        {
            InitializeComponent();
        }

        private DataSourceEnum DataSource
        {
            get { return (DataSourceEnum) radioButtons.FirstOrDefault(i => i.Checked).Tag; }
            set
            {
                foreach (var radioButton in radioButtons)
                {
                    radioButton.Checked = value == (DataSourceEnum) radioButton.Tag;
                }
            }
        }

        public static void HelloWorld()
        {
            loggerNew.Info("Hello world at 2 min intervals");
        }

        public static void HelloWorld5()
        {
            loggerNew.Info("Hello world at 5 min intervals");
        }

        public static void Display(string x)
        {
            loggerNew.InfoFormat("Display {0}", x);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _backgroundJobServer?.SendStop();
            base.OnClosing(e);
        }

        private void Form1_Load(object sender, EventArgs e1)
        {
            SqlServerButton.Tag = DataSourceEnum.SqlServer;
            MySqlButton.Tag = DataSourceEnum.Mysql;
            PostgreSQLRadioButton.Tag = DataSourceEnum.Postgresl;
            radioButtons = new[] {SqlServerButton, MySqlButton, PostgreSQLRadioButton};
            SqlServerButton.Click += SqlServerButton_Click;
            MySqlButton.Click += SqlServerButton_Click;
            PostgreSQLRadioButton.Click += SqlServerButton_Click;

            ConnectionStringTextBox.Text = Settings.Default.ConnectionString;
            DataSourceEnum e;
            if (Enum.TryParse(Settings.Default.DataSource, out e))
            {
                DataSource = e;
            }
            else
            {
                DataSource = DataSourceEnum.SqlServer;
            }
            TextBoxAppender.ConfigureTextBoxAppender(LoggerTextBox);
        }

        private void SqlServerButton_Click(object sender, EventArgs e)
        {
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            var connectionString = ConnectionStringTextBox.Text;
            
            switch (DataSource)
            {
                case DataSourceEnum.SqlServer:
                    storage = FluentNHibernateStorageFactory.ForMsSql2012(connectionString);
                    break;
                case DataSourceEnum.Mysql:
                    storage = FluentNHibernateStorageFactory.ForMySQL(connectionString);
                    break;
                case DataSourceEnum.Postgresl:
                    storage = FluentNHibernateStorageFactory.ForPostgreSQL(connectionString);
                    break;
            }
            if (storage != null)
            {
                GlobalConfiguration.Configuration.UseLog4NetLogProvider()
                    .UseStorage(storage);
                try
                {   Settings.Default.DataSource = DataSource.ToString();
                    Settings.Default.ConnectionString = ConnectionStringTextBox.Text;
                    Settings.Default.Save();

                    t = new Timer(60000);
                    t.Elapsed += (a, b) => { BackgroundJob.Enqueue(() => Display(Guid.NewGuid().ToString())); };

                    _backgroundJobServer = new BackgroundJobServer(new BackgroundJobServerOptions(), storage,
                        storage.GetBackgroundProcesses());

                    RecurringJob.AddOrUpdate(() => HelloWorld(), Cron.MinuteInterval(2));
                    RecurringJob.AddOrUpdate(() => HelloWorld5(), Cron.MinuteInterval(5));
                    loggerNew.Info("Background server started");
                    StartButton.Enabled = false;
                    StopButton.Enabled = true;
                 
                }
                catch (Exception ex)
                {
                    loggerNew.Error("Server start failed",ex);
                   StopButton_Click(null,new EventArgs());
                }
            }
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (t != null)
                {
                    t.Stop();
                    t = null;
                }
                if (_backgroundJobServer != null)
                {
                    _backgroundJobServer.SendStop();
                    _backgroundJobServer.Dispose();
                }
                _backgroundJobServer = null;
                StartButton.Enabled = true;
                StopButton.Enabled = false;
            }
            catch (Exception ex)
            {
                loggerNew.Error("Error during stop",ex);
            }
        }

        private enum DataSourceEnum
        {
            SqlServer = 0,
            Mysql = 1,
            Postgresl = 2
        }
    }
}