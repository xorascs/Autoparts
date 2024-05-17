using System.ComponentModel.DataAnnotations;

namespace Autoparts.Models
{
    public class Part
    {
        public int Id { get; set; }
        [Required, Display(Name = "Brand")]
        public int BrandId { get; set; }
        [Required, Display(Name = "Category")]
        public int CategoryId { get; set; }
        [Display(Name = "Total Rating")]
        public double? TotalRating { get; set; }
        [Required]
        public required string Name { get; set; }
        [Required]
        public required string Description { get; set; }

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public List<string> Photos { get; set; } = new List<string>();

        public Category? Category { get; set; }
        public Brand? Brand { get; set; }
    }
}
