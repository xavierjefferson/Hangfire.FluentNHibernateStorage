using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private PersistenceConfigurerEnum PersistenceConfigurerType
        {
            get => (PersistenceConfigurerEnum) DataProviderComboBox.SelectedItem;
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

        private Dictionary<PersistenceConfigurerEnum, string> GetSettings()
        {
            var a = Settings.Default.ConnectionStrings;
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<PersistenceConfigurerEnum, string>>(a) ??
                       new Dictionary<PersistenceConfigurerEnum, string>()
                       {
                           {
                               PersistenceConfigurerEnum.MsSql2012,
                               "Data Source=.\\sqlexpress;Database=northwind;Trusted_Connection=True;"
                           }
                       };
            }
            catch
            {
                return new Dictionary<PersistenceConfigurerEnum, string>();
            }
        }

        private string LoadConnectionString(PersistenceConfigurerEnum persistenceConfigurer)
        {
            var settings = GetSettings();
            return settings.ContainsKey(persistenceConfigurer) ? settings[persistenceConfigurer] : string.Empty;
        }

        private void Form1_Load(object sender, EventArgs e1)
        {
            var a = Enum.GetValues(typeof(PersistenceConfigurerEnum))
                .Cast<PersistenceConfigurerEnum>()
                .Where(i => i != PersistenceConfigurerEnum.None)
                .OrderBy(i => i.ToString())
                .ToList();
            DataProviderComboBox.DataSource = a;
            DataProviderComboBox.SelectedIndexChanged += DataProviderComboBox_SelectedIndexChanged;


            PersistenceConfigurerEnum b;
            PersistenceConfigurerType = Enum.TryParse(Settings.Default.DataSource, out b)
                ? b
                : PersistenceConfigurerEnum.MsSql2012;
           

            TextBoxAppender.ConfigureTextBoxAppender(LoggerTextBox);
        }

        private void DataProviderComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ConnectionStringTextBox.Text = LoadConnectionString((PersistenceConfigurerEnum) DataProviderComboBox.SelectedItem);
        }


        private void StartButton_Click(object sender, EventArgs e)
        {
            var connectionString = ConnectionStringTextBox.Text;
            SaveConnectionString(PersistenceConfigurerType, connectionString);

            //THIS LINE GETS THE STORAGE PROVIDER
            FluentNHibernateStorage.FluentNHibernateStorage storage = FluentNHibernateStorageFactory.For(PersistenceConfigurerType, connectionString);
            if (storage != null)
            {
                //THIS LINE CONFIGURES HANGFIRE WITH THE STORAGE PROVIDER
                GlobalConfiguration.Configuration.UseLog4NetLogProvider()
                    .UseStorage(storage);
                try
                {
                    

                    _timer = new Timer(60000);
                    _timer.Elapsed += (a, b) => { BackgroundJob.Enqueue(() => Display(Guid.NewGuid().ToString())); };

                    /*THIS LINE STARTS THE BACKGROUND SERVER*/
                    _backgroundJobServer = new BackgroundJobServer(new BackgroundJobServerOptions(), storage,
                        storage.GetBackgroundProcesses());

                    /*ADD DUMMY CRON JOBS FOR DEMONSTRATION PURPOSES*/
                    RecurringJob.AddOrUpdate(() => HelloWorld(), Cron.MinuteInterval(2));
                    RecurringJob.AddOrUpdate(() => HelloWorld5(), Cron.MinuteInterval(5));
                    loggerNew.Info("Background server started");
                    StartButton.Enabled = false;
                    StopButton.Enabled = true;
                }
                catch (Exception ex)
                {
                    loggerNew.Error("Server start failed", ex);
                    StopButton_Click(null, new EventArgs());
                }
            }
        }

        private void SaveConnectionString(PersistenceConfigurerEnum persistenceConfigurerType, string connectionString)
        {
            var dictionary = GetSettings();
            dictionary[persistenceConfigurerType] = connectionString;
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
                StartButton.Enabled = true;
                StopButton.Enabled = false;
            }
            catch (Exception ex)
            {
                loggerNew.Error("Error during stop", ex);
            }
        }
    }
}