using System;
using System.Windows.Forms;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace Hangfire.FluentNHibernate.SampleApplication
{
    public class TextBoxAppender : IAppender
    {
        private readonly object _lockObj = new object();
        private TextBox _textBox;

        public TextBoxAppender(TextBox textBox)
        {
            var frm = textBox.FindForm();
            if (frm == null)
                return;
            frm.FormClosing += delegate { Close(); };
            _textBox = textBox;
            Name = "TextBoxAppender";
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
                    _textBox = null;
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
                if (_textBox == null)
                    return;
    
       
                var msg = string.Format("{0:O} [{3}] {1} {2}\r\n", loggingEvent.TimeStamp, loggingEvent.LoggerName,
                    loggingEvent.RenderedMessage, loggingEvent.ThreadName);
                lock (_lockObj)
                {
                    // This check is required a second time because this class 
                    // is executing on multiple threads.
                    if (_textBox == null)
                        return;
                    // Because the logging is running on a different thread than
                    // the GUI, the control's "BeginInvoke" method has to be
                    // leveraged in order to append the message. Otherwise, a 
                    // threading exception will be thrown. 
                    var del = new Action<string>(s => _textBox.AppendText(s));
                    _textBox.BeginInvoke(del, msg);
                }
                Application.DoEvents();
            }
            catch
            {
                // There is not much that can be done here, and
                // swallowing the error is desired in my situation.
            }
        }

        public static void ConfigureTextBoxAppender(TextBox textBox)
        {
            var hierarchy = (Hierarchy) LogManager.GetRepository();
            var appender = new TextBoxAppender(textBox);
            hierarchy.Root.AddAppender(appender);
        }
    }
}