using System.ComponentModel.DataAnnotations;

namespace Autoparts.Models
{
    public class Issue
    {
        public int Id { get; set; }
        [Required, Display(Name = "User")]
        public int UserId { get; set; }
        [Required]
        public required string Title { get; set; }
        [Required]
        public required string Problem { get; set; }
        public bool? Solved { get; set; }

        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<User> Users { get; set; } = new List<User>();

        public User? User { get; set; }
    }
}
