using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlServerCe;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Hangfire.FluentNHibernate.SampleApplication.Properties;
using Hangfire.FluentNHibernateStorage;
using log4net;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace Hangfire.FluentNHibernate.SampleApplication
{
    public partial class Form1 : Form
    {
        private static readonly ILog loggerNew = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private BackgroundJobServer _backgroundJobServer;

        private Timer _timer;


        public Form1()
        {
            InitializeComponent();
        }

        public enum StateEnum
        {
            Stopped=0,Starting,Started
        }

        private StateEnum _currentState;

        private StateEnum State
        {
            get { return _currentState; }
            set
            {
                _currentState = value;
                StartButton.Enabled = value == StateEnum.Stopped;
                ConnectionStringTextBox.Enabled = value == StateEnum.Stopped;
                StopButton.Enabled = value == StateEnum.Started;
                HQLButton.Enabled = value == StateEnum.Started;
            }
        }
    
        private ProviderTypeEnum ProviderType
        {
            get => (ProviderTypeEnum) DataProviderComboBox.SelectedItem;
            set
            {
                ConnectionStringTextBox.Text = LoadConnectionString(value);
                DataProviderComboBox.SelectedItem = value;
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

        private Dictionary<ProviderTypeEnum, string> GetSettings()
        {
            var a = Settings.Default.ConnectionStrings;
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<ProviderTypeEnum, string>>(a) ??
                       new Dictionary<ProviderTypeEnum, string>
                       {
                           {
                               ProviderTypeEnum.MsSql2012,
                               "Data Source=.\\sqlexpress;Database=northwind;Trusted_Connection=True;"
                           }
                       };
            }
            catch
            {
                return new Dictionary<ProviderTypeEnum, string>();
            }
        }

        private string LoadConnectionString(ProviderTypeEnum persistenceConfigurer)
        {
            var settings = GetSettings();
            return settings.ContainsKey(persistenceConfigurer) ? settings[persistenceConfigurer] : string.Empty;
        }

        private void Form1_Load(object sender, EventArgs e1)
        {
            var persistenceConfigurerEnums = Enum.GetValues(typeof(ProviderTypeEnum))
                .Cast<ProviderTypeEnum>()
                .Where(i => i != ProviderTypeEnum.None)
                .OrderBy(i => i.ToString())
                .ToList();
            DataProviderComboBox.DataSource = persistenceConfigurerEnums;
            DataProviderComboBox.SelectedIndexChanged += DataProviderComboBox_SelectedIndexChanged;


            ProviderTypeEnum type;
            ProviderType = Enum.TryParse(Settings.Default.DataSource, out type)
                ? type
                : ProviderTypeEnum.MsSql2012;


            TextBoxAppender.ConfigureTextBoxAppender(LoggerTextBox);
            State = StateEnum.Stopped;
        }

        private void DataProviderComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ConnectionStringTextBox.Text =
                LoadConnectionString((ProviderTypeEnum) DataProviderComboBox.SelectedItem);
        }

        private FluentNHibernateJobStorage storage = null;

        private void StartButton_Click(object sender, EventArgs e)
        {
            
            var connectionString = ConnectionStringTextBox.Text;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                MessageBox.Show(this, "connection string cannot be blank");
                return;
            }
                SaveConnectionString(ProviderType, connectionString);

            Settings.Default.DataSource = ProviderType.ToString();
            Settings.Default.Save();

           
            //THIS LINE GETS THE STORAGE PROVIDER
            storage = FluentNHibernateStorageFactory.For(ProviderType, connectionString);
            if (storage != null)
            {
                State = StateEnum.Starting;
                //THIS LINE CONFIGURES HANGFIRE WITH THE STORAGE PROVIDER
                GlobalConfiguration.Configuration.UseLog4NetLogProvider()
                    .UseStorage(storage);
                try
                {
                    _timer = new Timer(60000);
                    _timer.Elapsed += (a, b) => { BackgroundJob.Enqueue(() => Display(Guid.NewGuid().ToString())); };
                    _timer.Start();
                    /*THIS LINE STARTS THE BACKGROUND SERVER*/
                    _backgroundJobServer = new BackgroundJobServer(new BackgroundJobServerOptions(), storage,
                        storage.GetBackgroundProcesses());

                    /*ADD DUMMY CRON JOBS FOR DEMONSTRATION PURPOSES*/
                    RecurringJob.AddOrUpdate(() => HelloWorld(), Cron.MinuteInterval(2));
                    RecurringJob.AddOrUpdate(() => HelloWorld5(), Cron.MinuteInterval(5));
                    loggerNew.Info("Background server started");
                    State = StateEnum.Started;

                }
                catch (Exception ex)
                {
                    loggerNew.Error("Server start failed", ex);
                    StopButton_Click(null, new EventArgs());
                    State = StateEnum.Stopped;
                    storage?.Dispose();
                    storage = null;
                }
            }
        }

        private void SaveConnectionString(ProviderTypeEnum providerType, string connectionString)
        {
            var dictionary = GetSettings();
            dictionary[providerType] = connectionString;
            Settings.Default.ConnectionStrings = JsonConvert.SerializeObject(dictionary);
            Settings.Default.Save();
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_timer != null)
                {
                    _timer.Stop();
                    _timer = null;
                }
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
                loggerNew.Error("Error during stop", ex);
            }
        }

        private void HQLButton_Click(object sender, EventArgs e)
        {
            var f = new HQLForm(this.storage);
            f.ShowDialog(this);
        }
    }
}