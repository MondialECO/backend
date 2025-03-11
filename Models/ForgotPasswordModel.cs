using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class ForgotPasswordModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
