using System.ComponentModel.DataAnnotations;

namespace Autoparts.Models
{
    public class Message
    {
        public int Id { get; set; }
        [Required, Display(Name = "User")]
        public int UserId { get; set; }
        [Required, Display(Name = "Issue")]
        public int IssueId { get; set; }
        [Required]
        public required string Comment { get; set; }
        public DateTime CreateTime { get; set; }

        public User? User { get; set; }
        public Issue? Issue { get; set; }
    }
}
