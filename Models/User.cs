using System.ComponentModel.DataAnnotations;

namespace Autoparts.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        public Role Role { get; set; }
        [Required]
        public required string Login {  get; set; }
        [Required]
        public required string Password { get; set; }

        [Display(Name = "Related parts")]
        public ICollection<Part> RelatedParts { get; set; } = new List<Part>();
        public ICollection<Wish> Wishes { get; set; } = new List<Wish>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Issue> Issues { get; set; } = new List<Issue>();
    }

    public enum Role
    {
        User,
        Admin
    }
}
