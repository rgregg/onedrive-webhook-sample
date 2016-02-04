using PhotoOrganizerShared;
using PhotoOrganizerShared.Models;
using System;
using System.IO;

namespace PhotoOrganizerWebJob
{
    public class WebJobLogger
    {
        private readonly TextWriter writer;
        public Account Account { get; set; }

        public WebJobLogger(TextWriter writer)
            : this(null, writer)
        { }

        public WebJobLogger(Account account, TextWriter writer)
        {
            this.Account = account;
            this.writer = writer;
        }

        public void WriteLog(ActivityEventCode? code, string format, params object[] values)
        {
#if DEBUG
            Console.WriteLine(string.Format(format, values));
#endif
            if (null != this.writer)
            {
                this.writer.WriteFormattedLine(format, values);
            }

            if (null != this.Account && code.HasValue)
            {
                // Log to azure asynchronously
                var t = AzureStorage.InsertActivityAsync(
                    new Activity
                    {
                        UserId = this.Account.Id,
                        Type = code.Value,
                        Message = string.Format(format, values)
                    });
                t.Wait();
            }
        }

        public void WriteLog(string message)
        {
            WriteLog(ActivityEventCode.MessageLogged, "{0}", message);
        }

        public void WriteLog(string format, object value)
        {
            WriteLog(ActivityEventCode.MessageLogged, format, value);
        }

        public void WriteLog(string format, params object[] values)
        {
            WriteLog(ActivityEventCode.MessageLogged, format, values);
        }


    }
}
