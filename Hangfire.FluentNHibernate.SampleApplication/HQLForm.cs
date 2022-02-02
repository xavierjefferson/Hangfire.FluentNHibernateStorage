using System;
using System.Windows.Forms;
using Hangfire.FluentNHibernateStorage;
using Newtonsoft.Json;

namespace Hangfire.FluentNHibernate.SampleApplication
{
    public partial class HQLForm : Form
    {
        private readonly FluentNHibernateJobStorage Storage;

        public HQLForm(FluentNHibernateJobStorage Storage)
        {
            InitializeComponent();
            this.Storage = Storage;
        }

        private void ExecuteButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(HQLQueryTextBox.Text))
            {
                MessageBox.Show(this, "Enter query text.");
            }
            else
            {
                try
                {
                    QueryResponseTextBox.Text =
                        JsonConvert.SerializeObject(new FluentNHibernateJobStorageTestWrapper(Storage).ExecuteHqlQuery(HQLQueryTextBox.Text));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }
    }
}