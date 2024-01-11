#region Using statements
using System.Data;
using System.Text;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UsagePoller.ActivityChecker.SettingDetails;
#endregion

namespace UsagePoller.ActivityChecker
{
    public sealed class Checker
    {
        private List<string> NotifiedSessionHashes = new List<string>();
        private Settings settings;


        public MailAddress GetLongReportEmailAddress()
        {
            return settings.ReportingDetails.LongSessionEmailAddress;
        }

        public int GetMaxIdleSessionLength()
        {
            return settings.ReportingDetails.MaxIdleSessionLength;
        }

        public int GetSleepTime()
        {
            return settings.ReportingDetails.SleepTime;
        }

        public void LoadSettings(string fileName)
        {
            this.settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(fileName));
        }

        public DataTable GetActivityDetails()
        {

            DataTable dataTable = GetResults();
            return dataTable;
        }

        private DataTable GetResults()
        {
            DataTable dataTable = new DataTable();

            using (SqlConnection sqlConnection = new SqlConnection(this.settings.GPServer.ConnectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    StringBuilder query = new StringBuilder(Queries.GetAllActivityDetails);

                    switch (this.settings.ProgramSettings.ListOfUsersToExclude.Count())
                    {
                        case 0:
                            break;
                        case 1:
                            query.Append(" WHERE USERID <> @userToExclude");
                            sqlCommand.Parameters.AddWithValue("@userToExclude", settings.ProgramSettings.ListOfUsersToExclude[0]);
                            break;
                        default:
                            query.Append(" WHERE USERID NOT IN (");

                            for (int index = 0; index < this.settings.ProgramSettings.ListOfUsersToExclude.Count; index++)
                            {
                                string paramName = "@userToExclude" + index.ToString();
                                if (index > 0)
                                {
                                    query.Append(", ");
                                }

                                query.Append(paramName);
                                sqlCommand.Parameters.AddWithValue(paramName, settings.ProgramSettings.ListOfUsersToExclude[index]);
                            }
                            query.Append(")");
                            break;
                    }

                    sqlCommand.CommandText = query.ToString();
                    sqlCommand.Connection = sqlConnection;

                    sqlConnection.Open();
                    SqlDataReader reader = sqlCommand.ExecuteReader();
                    dataTable.Load(reader);
                }
            }

            return dataTable;
        }

        internal void UpdateActivityLog(DataTable activityDataTable)
        {
            using (SqlConnection sqlConnection = new SqlConnection(settings.LogServer.ConnectionString))
            {
                var updateDateTime = DateTime.Now;
                string updateTime = updateDateTime.ToString(Thread.CurrentThread.CurrentCulture);
                
                sqlConnection.Open();
                SqlCommand sqlCommand = new SqlCommand(Queries.InsertActivityLog, sqlConnection);

                foreach (DataRow row in activityDataTable.Rows)
                {
                    string userId = row["USERID"].ToString() ?? "";
                    string companyName = row["CMPNYNAM"].ToString() ?? "";
                    string hostname = row["hostname"].ToString() ?? "";
                    string applicationName = row["program_name"].ToString() ?? "";
                    string loginTime = row["LOGIN_DATE_TIME"].ToString() ?? "";
                    string sessionHash = GetSessionHashValue(row);
                    int.TryParse(row["Session_Length"].ToString(), out int sessionLength);
                    int.TryParse(row["Idle_Time"].ToString(), out int idleLength);
                    if (!int.TryParse(row["Lock_Count"].ToString(), out int lockCount))
                        lockCount = 0;
                    if (!int.TryParse(row["Batch_Count"].ToString(), out int batchCount))
                        batchCount = 0;

                    sqlCommand.Parameters.Clear();
                    sqlCommand.Parameters.AddWithValue("@userID", userId);
                    sqlCommand.Parameters.AddWithValue("@companyName", companyName);
                    sqlCommand.Parameters.AddWithValue("@hostname", hostname);
                    sqlCommand.Parameters.AddWithValue("@applicationName", applicationName);
                    sqlCommand.Parameters.AddWithValue("@loginTime", loginTime);
                    sqlCommand.Parameters.AddWithValue("@sessionLength", sessionLength);
                    sqlCommand.Parameters.AddWithValue("@idleLength", idleLength);
                    sqlCommand.Parameters.AddWithValue("@updateTime", updateTime);
                    sqlCommand.Parameters.AddWithValue("@lockCount", lockCount);
                    sqlCommand.Parameters.AddWithValue("@batchCount", batchCount);
                    sqlCommand.Parameters.AddWithValue("@sessionHash", sessionHash);
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        public void EmailAllLongSessions(DataRow[] dataTable, MailAddress sendToEmail)
        {
            if (dataTable.Length <= 0) return;
            MailMessage mailMessage = new MailMessage()
            {
                From = settings.ReportingDetails.ReportSenderAddress,
                Subject = $"Activity Checker Results - {DateTime.Now.ToString(Thread.CurrentThread.CurrentCulture)}",
                IsBodyHtml = true,
                To = { sendToEmail }
            };

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("<p>Here are long idle sessions</p><table><caption>Long Idle Sessions</caption><tr><th>Login</th><th>Login Time</th><th>Hostname</th><th>Program</th><th>Session Length</th><th>Idle Time</th><th>Locks</th><th>Open Batches</th></tr>");

            foreach (DataRow dataRow in dataTable)
            {
                stringBuilder.Append($"<tr><td>{dataRow["USERNAME"]}</td><td>{dataRow["LOGIN_DATE_TIME"]}</td><td>{dataRow["hostname"]}</td><td>{dataRow["program_name"]}</td><td>{dataRow["Session_Length"]}</td><td>{dataRow["Idle_Time"]}</td><td>{dataRow["Lock_Count"]}</td><td>{dataRow["Batch_Count"]}</td></tr>");
            }
            stringBuilder.AppendLine("</table>");

            string messageBody = stringBuilder.ToString();
            mailMessage.Body = messageBody;

            SendEmail(mailMessage);
        }

        public void EmailLongUsers(DataRow[] dataRow)
        {
            if (dataRow.Length <= 0) return;
            foreach (DataRow session in dataRow)
            {
                string emailAddress = Environment.GetEnvironmentVariable("USER_EMAIL") ??
                                      $"{session["USERID"]}@empireindustries.com";

                string messageBody = BuildMessageBody(session);

                MailMessage message = new MailMessage()
                {
                    From = settings.ReportingDetails.ReportSenderAddress,
                    Subject = $"Your session has been idle for too long - {DateTime.Now.ToString(Thread.CurrentThread.CurrentCulture)}",
                    IsBodyHtml = true,
                    Body = messageBody
                };

                message.To.Add(emailAddress);

                SendEmail(message);
            }
        }

        private static string BuildMessageBody(DataRow session)
        {
            #region Build the message body

            StringBuilder messageBody = new StringBuilder();

            messageBody.AppendLine(
                $"{session["USERNAME"]} - <br>Your GP session has been idle for {session["Idle_Time"]} minutes, which is too long.  Your GP session has been logged out, and any in-process work may have been lost.");
            if (!int.TryParse(session["Lock_Count"].ToString(), out int lockCount))
                lockCount = 0;
            if (!int.TryParse(session["Batch_Count"].ToString(), out int batchCount))
                batchCount = 0;
            if (lockCount > 0)
                messageBody.AppendLine(
                    $"<p><b>You had {lockCount} transactions locked.  These locks have been removed, and the transactions closed.  Please verify them before proceeding.</b></p>");
            if (batchCount > 0)
                messageBody.AppendLine(
                    $"<p>You have {batchCount} batches created.  These should be fine, but please verify all transactions in them prior to posting.");

            #endregion

            return messageBody.ToString();
        }

        public DataRow[] GetLongSessions(DataTable dataTable)
        {
            // Filter the table to only include sessions that have been idle for more than specified number of minutes, then compare against the list of previously flagged sessions to only return new sessions
            var filteredLongTable = dataTable.AsEnumerable().Where(row => row.Field<int>("Idle_Time") > this.settings.ReportingDetails.MaxIdleSessionLength);
            var filteredUniqueTable = filteredLongTable.AsEnumerable().Where(r => !NotifiedSessionHashes.Contains(GetSessionHashValue(r)));
            DataRow[] dataRows = filteredUniqueTable.ToArray();

            // Add the new sessions to the list of previously flagged sessions
            foreach (DataRow dataRow in dataRows)
            {
                string sessionHash = GetSessionHashValue(dataRow);
                NotifiedSessionHashes.Add(sessionHash);
            }

            return dataRows;
        }

        private static string GetSessionHashValue(DataRow dataRow)
        {
            StringBuilder hashBuilder = new StringBuilder();
            hashBuilder.Append(dataRow["USERID"]);
            hashBuilder.Append(dataRow["CMPNYNAM"]);
            hashBuilder.Append(dataRow["LOGIN_DATE_TIME"]);
            hashBuilder.Append(dataRow["hostname"]);
            hashBuilder.Append(dataRow["program_name"]);

            string sessionIndex = hashBuilder.ToString();
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sessionIndex));
            
            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte t in bytes)
            {
                stringBuilder.Append(t.ToString("x2"));
            }
            return stringBuilder.ToString();
        }

        public void DeleteLongIdleSessions(DataRow[] longSessions, ILogger logger)
        {
            using SqlConnection sqlConnection = new SqlConnection(settings.GPServer.ConnectionString);
            {
                sqlConnection.Open();
                SqlCommand deleteDexLocks = new SqlCommand(Queries.DeleteUserDexLocks, sqlConnection);
                SqlCommand deleteDexSessions = new SqlCommand(Queries.DeleteUserDexSessions, sqlConnection);
                SqlCommand deleteTransactionLocks = new SqlCommand(Queries.DeleteUserTransactionLocks, sqlConnection);
                SqlCommand deleteUserActivity = new SqlCommand(Queries.DeleteUserFromActivityTable, sqlConnection);

                foreach (DataRow session in longSessions)
                {
                    logger.LogWarning("Deleting session for {User}", session["USERID"]);

                    deleteDexLocks.Parameters.AddWithValue("@userID", session["USERID"].ToString());
                    deleteDexLocks.ExecuteNonQuery();
                    deleteDexLocks.Parameters.Clear();

                    deleteDexSessions.Parameters.AddWithValue("@userID", session["USERID"].ToString());
                    deleteDexSessions.ExecuteNonQuery();
                    deleteDexSessions.Parameters.Clear();

                    deleteTransactionLocks.Parameters.AddWithValue("@userID", session["USERID"].ToString());
                    deleteTransactionLocks.ExecuteNonQuery();
                    deleteTransactionLocks.Parameters.Clear();

                    deleteUserActivity.Parameters.AddWithValue("@userID", session["USERID"].ToString());
                    deleteUserActivity.ExecuteNonQuery();
                    deleteUserActivity.Parameters.Clear();
                }
                sqlConnection.Close();
            }
        }

        public string GetPublicSettings()
        {
            return settings.GetPublicSettings();
        }
        
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
        
        internal void PrintResultsToConsole(DataTable dataTable)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{ConsoleReportSettings.PrintActivityDetailsHeader}\t{DateTime.Now}");

            foreach (DataRow row in dataTable.Rows)
            {

                if (!int.TryParse(row["Idle_Time"].ToString(), out int idleTime))
                {
                    idleTime = 0;
                }

                switch (idleTime)
                {
                    case int n when n > 90:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case int n when n > 60:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                }

                Console.WriteLine(ConsoleReportSettings.PrintActivityDetails, row["USERID"].ToString().Substring(0, int.Min(7, row["USERID"].ToString().Length)).PadRight(7), row["USERNAME"].ToString().Substring(0, int.Min(25, row["USERNAME"].ToString().Length)).PadRight(20), row["CMPNYNAM"].ToString().Substring(0, int.Min(20, row["CMPNYNAM"].ToString().Length)).PadRight(20), row["hostname"], row["program_name"].ToString().Substring(0, int.Min(20, row["program_name"].ToString().Length)).PadRight(20), row["LOGIN_DATE_TIME"].ToString().Substring(9, row["LOGIN_DATE_TIME"].ToString().Length - 9), row["Session_Length"], row["Idle_Time"]);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private void SendEmail(MailMessage mailMessage)
        {
            using (SmtpClient smtpClient = new SmtpClient(settings.EmailSettings.SmtpServer)
            {
                Port = settings.EmailSettings.SmtpPort,
                Credentials = new System.Net.NetworkCredential(settings.EmailSettings.SmtpUser, settings.EmailSettings.SmtpKey),
                EnableSsl = settings.EmailSettings.SmtpEnableSsl
            })
            {
                smtpClient.Send(mailMessage);
            }
        }
    }
}
