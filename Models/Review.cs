using System.ComponentModel.DataAnnotations;

namespace Autoparts.Models
{
    public class Review
    {
        public int Id { get; set; }
        [Required, Display(Name = "User")]
        public int UserId { get; set; }
        [Required, Display(Name = "Part")]
        public int PartId { get; set; }
        [Required]
        public required string Comment { get; set; }
        public DateTime CreateTime { get; set; }
        [Required]
        public int Rating { get; set; }

        public Part? Part { get; set; }
        public User? User { get; set; }
    }
}
