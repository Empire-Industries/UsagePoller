using Newtonsoft.Json.Linq;

namespace UsagePoller.ActivityChecker.SettingDetails
{
    internal struct EmailSettings : ISettings
    {
        public string SmtpUser { get; set; }

        public string SmtpKey { get; set; }

        public string SmtpServer { get; set; }

        public int SmtpPort { get; set; }

        public bool SmtpEnableSsl { get; set; }

        public JArray GetPublicSettings()
        {
            return new JArray(new JObject { { nameof(SmtpUser), SmtpUser }, { nameof(SmtpKey), "*****" }, { nameof(SmtpServer), SmtpServer }, { nameof(SmtpPort), SmtpPort }, { nameof(SmtpEnableSsl), SmtpEnableSsl.ToString() } });
        }
    }
}