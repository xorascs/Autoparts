using System.ComponentModel.DataAnnotations;

namespace Autoparts.Models
{
    public class UserFormViewModel
    {
        [Required]
        public required string Login { get; set; }
        [Required]
        public required string Password { get; set; }
    }
}
