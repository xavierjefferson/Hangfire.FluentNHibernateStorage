using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Hangfire.FluentNHibernate.SampleApplication.Properties;
using Hangfire.FluentNHibernateStorage;
using log4net;
using Newtonsoft.Json;
using Snork.FluentNHibernateTools;
using Timer = System.Timers.Timer;

namespace Hangfire.FluentNHibernate.SampleApplication
{
    public partial class Form1 : Form
    {
        public enum StateEnum
        {
            Stopped = 0,
            Starting,
            Started
        }

        private static readonly ILog loggerNew = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private BackgroundJobServer _backgroundJobServer;

        private StateEnum _currentState;

        private Timer _timer;

        private FluentNHibernateJobStorage storage;


        public Form1()
        {
            InitializeComponent();
        }

        private StateEnum State
        {
            get => _currentState;
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
                ConnectionStringTextBox.Text = LoadProviderSetting(value).ConnectionString;
                DataProviderComboBox.SelectedItem = value;
            }
        }

        public static void HelloWorld(DateTime whenQueued, int interval)
        {
            loggerNew.InfoFormat("Hello world at {2} min intervals.  Enqueued={0}, Now={1}", whenQueued, DateTime.Now,
                interval);
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

        private Dictionary<ProviderTypeEnum, ProviderSetting> GetSettings()
        {
            var a = Settings.Default.ProviderSettings;
            try
            {
                var tmp = JsonConvert.DeserializeObject<Dictionary<ProviderTypeEnum, ProviderSetting>>(a);
                if (tmp != null) return tmp;
            }
            catch (Exception ex)
            {
                loggerNew.Error("Error reading settings", ex);
            }

            return new Dictionary<ProviderTypeEnum, ProviderSetting>
            {
                {
                    ProviderTypeEnum.MsSql2012,
                    new ProviderSetting
                        {ConnectionString = "Data Source=.\\sqlexpress;Database=northwind;Trusted_Connection=True;"}
                }
            };
        }

        private ProviderSetting LoadProviderSetting(ProviderTypeEnum persistenceConfigurer)
        {
            var settings = GetSettings();
            return settings.ContainsKey(persistenceConfigurer)
                ? settings[persistenceConfigurer]
                : new ProviderSetting();
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
            TableNamePrefixTextBox.Text = Settings.Default.TableNamePrefix;

            TextBoxAppender.ConfigureTextBoxAppender(LoggerTextBox);
            State = StateEnum.Stopped;
        }

        private void DataProviderComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ConnectionStringTextBox.Text =
                LoadProviderSetting((ProviderTypeEnum) DataProviderComboBox.SelectedItem).ConnectionString;
        }

        public static void Show(int c)
        {
            Console.WriteLine(c);
            Thread.Sleep(1000 * c);
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            try
            {
                var connectionString = ConnectionStringTextBox.Text;
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    MessageBox.Show(this, "connection string cannot be blank");
                    return;
                }

                var tableNamePrefix = TableNamePrefixTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(tableNamePrefix))
                {
                    MessageBox.Show(this, "Table name prefix cannot be blank");
                    return;
                }

                SaveConnectionString(ProviderType, connectionString);
                Settings.Default.TableNamePrefix = tableNamePrefix;
                Settings.Default.DataSource = ProviderType.ToString();
                Settings.Default.Save();


                //THIS LINE GETS THE STORAGE PROVIDER
                storage = FluentNHibernateStorageFactory.For(ProviderType, connectionString,
                    new FluentNHibernateStorageOptions {TablePrefix = tableNamePrefix});
                if (storage != null)
                {
                    State = StateEnum.Starting;
                    //THIS LINE CONFIGURES HANGFIRE WITH THE STORAGE PROVIDER
                    GlobalConfiguration.Configuration.UseLog4NetLogProvider()
                        .UseStorage(storage);

                    _timer = new Timer(60000);
                    _timer.Elapsed += (a, b) => { BackgroundJob.Enqueue(() => Display(Guid.NewGuid().ToString())); };
                    _timer.Start();
                    /*THIS LINE STARTS THE BACKGROUND SERVER*/
                    _backgroundJobServer = new BackgroundJobServer(new BackgroundJobServerOptions(), storage,
                        storage.GetBackgroundProcesses());
                    string parent = null;
                    for (var i = 0; i < 100; i++)
                    {
                        var k = i;
                        parent = i == 0
                            ? BackgroundJob.Enqueue(() => Show(k))
                            : BackgroundJob.ContinueJobWith(parent, () => Show(k));
                    }

                    /*ADD DUMMY CRON JOBS FOR DEMONSTRATION PURPOSES*/
                    RecurringJob.AddOrUpdate("h2", () => HelloWorld(DateTime.Now, 2), "*/2 * * * *");
                    RecurringJob.AddOrUpdate("h5", () => HelloWorld(DateTime.Now, 5), "*/5 * * * *");
                    RecurringJob.AddOrUpdate("h1", () => HelloWorld(DateTime.Now, 1), "* * * * *");
                    RecurringJob.AddOrUpdate("h7", () => HelloWorld(DateTime.Now, 7), "*/7 * * * *");
                    loggerNew.Info("Background server started");
                    State = StateEnum.Started;
                }
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

        private void SaveConnectionString(ProviderTypeEnum providerType, string connectionString)
        {
            var dictionary = GetSettings();
            dictionary[providerType] = new ProviderSetting {ConnectionString = connectionString};
            Settings.Default.ProviderSettings = JsonConvert.SerializeObject(dictionary);
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
            var f = new HQLForm(storage);
            f.ShowDialog(this);
        }

        public class ProviderSetting
        {
            public string ConnectionString { get; set; }
        }
    }
}