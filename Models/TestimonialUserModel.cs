using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class TestimonialUserModel
    {

        [Required]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Description can't exceed 500 characters.")]
        public string Description { get; set; }

        [Required]
        public string Profession { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Point { get; set; }

        [Required]
        public string Author { get; set; }

        public IFormFile imageFile { get; set; }
    }
}
