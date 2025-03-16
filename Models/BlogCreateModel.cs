using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class BlogCreateModel
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        public string BlogCategory { get; set; }

        public IFormFile? ImageFile { get; set; }

        public string CreatorId { get; set; }
    }
}
