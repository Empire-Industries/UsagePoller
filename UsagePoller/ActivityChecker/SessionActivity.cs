namespace UsagePoller.ActivityChecker
{
    internal struct SessionActivity
    {
        public string USERID;
        public string USERNAME;
        public string CMPNYNAM;
        public string LOGIN_DATE_TIME;
        public string last_batch;
        public int Session_Length;
        public int Idle_Time;
        public string hostname;
        public string program_name;
        public int Lock_Count;
        public int Batch_Count;
    }
}
