using Newtonsoft.Json;
using System.Data;
using UsagePoller.ActivityChecker;

namespace UsagePoller.ServiceHelpers
{
    internal static class Helpers
    {
        public static string ParseSessionDetails(DataRow[] dataRows)
        {
            List<SessionActivity> sessions = dataRows.Select(row => new SessionActivity
                {
                    USERID = row["USERID"].ToString() ?? string.Empty,
                    USERNAME = row["USERNAME"].ToString() ?? string.Empty,
                    CMPNYNAM = row["CMPNYNAM"].ToString() ?? string.Empty,
                    LOGIN_DATE_TIME = row["LOGIN_DATE_TIME"].ToString() ?? string.Empty,
                    last_batch = row["last_batch"].ToString() ?? string.Empty,
                    Session_Length = Convert.ToInt32(row["Session_Length"]),
                    Idle_Time = Convert.ToInt32(row["Idle_Time"]),
                    hostname = row["hostname"].ToString() ?? string.Empty,
                    program_name = row["program_name"].ToString() ?? string.Empty,
                    Lock_Count = Convert.ToInt32(row["Lock_Count"]),
                    Batch_Count = Convert.ToInt32(row["Batch_Count"])
                })
                .ToList();

            return JsonConvert.SerializeObject(sessions, Formatting.Indented);
        }
    }
}
