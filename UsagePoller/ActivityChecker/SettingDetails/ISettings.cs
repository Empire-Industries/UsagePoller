using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsagePoller.ActivityChecker.SettingDetails
{
    internal interface ISettings
    {
        public JArray GetPublicSettings();
    }
}
