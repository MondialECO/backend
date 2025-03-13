using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApp.Models.DatabaseModels
{
    public class TestimonialModel
    {
        public string Id { get; set; }
        public string CompanyName { get; set; }
        public string Description { get; set; }
        public string Profession { get; set; }
        public int Point { get; set; }
        public string Author { get; set; }
        public string Image { get; set; }
    }
}
