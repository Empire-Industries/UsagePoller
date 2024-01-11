using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UsagePoller.ActivityChecker.SettingDetails
{
    internal class SQLServer : ISettings
    {
        public string? ConnectionString
        {
            get
            {
                if (!(string.IsNullOrEmpty(DatabaseName) || string.IsNullOrEmpty(UserId) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(ServerName)))
                {
                    return $"Data Source={ServerName};Initial Catalog={DatabaseName};User ID={UserId};Password={password}";
                }
                else
                {
                    return null;
                }
            }
        }

        public string? ServerName { get; set; }

        public string? UserId { get; set; }

        public string? Password { get => "*****";
            set => password = value;
        }
        public string? DatabaseName { get; set; }

        private string? password;

        public virtual JArray GetPublicSettings()
        {
            JArray publicSettings = new JArray(new JObject { { nameof(ServerName), ServerName }, { nameof(UserId), UserId }, { nameof(Password), "*****" }, { nameof(DatabaseName), DatabaseName } });
            return publicSettings;

        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
