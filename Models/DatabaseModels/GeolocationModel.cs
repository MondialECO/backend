namespace WebApp.Models.DatabaseModels
{
    public class GeolocationModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string IpAddress { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
        public string Postal { get; set; }
        public string Loc { get; set; }
        public string Org { get; set; }
        public string TimeZone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
