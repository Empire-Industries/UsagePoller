using Newtonsoft.Json.Linq;

namespace UsagePoller.ActivityChecker.SettingDetails
{
    internal struct ProgramSettings : ISettings
    {
        public string Exclusions { get; set; }

        public List<string> ListOfUsersToExclude => string.IsNullOrEmpty(Exclusions) ? new List<string>() : Exclusions.Split(',').ToList();

        public JArray GetPublicSettings()
        {
            return new JArray(new JObject { { nameof(Exclusions), Exclusions } });
        }
    }
}
