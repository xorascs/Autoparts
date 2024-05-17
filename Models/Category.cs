using System.ComponentModel.DataAnnotations;

namespace Autoparts.Models
{
    public class Category
    {
        public int Id { get; set; }
        [Required]
        public required string Name { get; set; }
    }
}
