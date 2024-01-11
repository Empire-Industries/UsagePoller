using Newtonsoft.Json.Linq;
using System.Net.Mail;

namespace UsagePoller.ActivityChecker.SettingDetails
{
    internal struct ReportingDetails : ISettings
    {
        public int SleepTime { get; set; }

        public int MaxIdleSessionLength { get; set; }

        public string ReportSenderEmailString { get; set; }

        public string ReportSenderNameString { get; set; }

        public MailAddress ReportSenderAddress => new(ReportSenderEmailString, ReportSenderNameString);

        public string LongSessionEmailString
        {
            get => LongSessionEmailAddress.Address;
            set
            {
                MailAddress address = new MailAddress(value);
                LongSessionEmailAddress = address;
            }
        }

        public MailAddress LongSessionEmailAddress { get; set; }

        public JArray GetPublicSettings()
        {
            return new JArray(new JObject { { nameof(SleepTime), SleepTime }, { nameof(MaxIdleSessionLength), MaxIdleSessionLength }, { nameof(ReportSenderEmailString), ReportSenderEmailString }, { nameof(ReportSenderNameString), ReportSenderNameString }, { nameof(LongSessionEmailString), LongSessionEmailString } });
        }
    }
}
