using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UsagePoller.ActivityChecker.SettingDetails;

namespace UsagePoller.ActivityChecker
{
    internal struct Settings
    {
        public EmailSettings EmailSettings { get; init; }

        public SQLServer GPServer { get; set; }

        public SQLServer LogServer { get; set; }

        public ConsoleReportSettings ConsoleReportSettings { get; set; }

        public ReportingDetails ReportingDetails { get; init; }

        public ProgramSettings ProgramSettings { get; init; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public string GetPublicSettings()
        {
            JArray emailSettings = EmailSettings.GetPublicSettings();
            JArray gpSettings = GPServer.GetPublicSettings();
            JArray logSettings = LogServer.GetPublicSettings();
            JArray reportingDetails = ReportingDetails.GetPublicSettings();
            JArray programSettingDetails = ProgramSettings.GetPublicSettings();

            JObject publicSettings = new JObject
            {
                [nameof(ReportingDetails)] = reportingDetails,
                [nameof(EmailSettings)] = emailSettings,
                [nameof(GPServer)] = gpSettings,
                [nameof(LogServer)] = logSettings,
                [nameof(ProgramSettings)] = programSettingDetails
            };
            
            return publicSettings.ToString();
        }
    }
}