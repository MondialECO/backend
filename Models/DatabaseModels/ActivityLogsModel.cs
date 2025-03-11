namespace WebApp.Models.DatabaseModels
{
    public class ActivityLogsModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string IpAddress { get; set; }
        public string LocalIpAddress { get; set; }
        public int Port { get; set; }
        public string GeoLocation { get; set; }
        public string ReferrerUrl { get; set; }
        public string url { get; set; }
        public string data { get; set; }
        public string RequestMethod { get; set; }
        public int? ResponseStatusCode { get; set; }
        public string UserAgent { get; set; }
        public string BrowserLanguage { get; set; }
        public string DeviceType { get; set; }
        public string OperatingSystem { get; set; }
        public string BrowserName { get; set; }
        public bool IsSecureConnection { get; set; }
        public DateTime ActivityDate { get; set; }
    }
}
