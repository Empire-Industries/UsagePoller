namespace UsagePoller.ActivityChecker
{
    internal struct Queries
    {
        public const string InsertActivityLog = "INSERT INTO activityTrackingLog(userID, companyName, hostname, applicationName, loginTime, sessionLength, idleLength, lockCount, batchCount, updateTime, sessionHash) VALUES(@userID, @companyName, @hostname, @applicationName, @loginTime, @sessionLength, @idleLength, @lockCount, @batchCount, @updateTime, @sessionHash)";
        public const string GetAllActivityDetails = "SELECT * FROM _nb_GetActivity_Function()";
        public const string DeleteUserFromActivityTable = "DELETE from dynamics..ACTIVITY where userid = @userID";
        public const string DeleteUserTransactionLocks = "delete from DYNAMICS..SY00801 where USERID = @userID";
        public const string DeleteUserDexSessions = "delete from tempdb..DEX_SESSION where session_id in (select SQLSESID from DYNAMICS..ACTIVITY where USERID = @userID)";
        public const string DeleteUserDexLocks = "delete from tempdb..DEX_LOCK where session_id in (select SQLSESID from DYNAMICS..ACTIVITY where USERID = @userID)";
    }
}
