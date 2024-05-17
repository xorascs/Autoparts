using System.ComponentModel.DataAnnotations;

namespace Autoparts.Models
{
    public class Wish
    {
        public int Id { get; set; }
        [Required, Display(Name = "Part")]
        public int PartId { get; set; }
        [Required, Display(Name = "Created at")]
        public DateTime CreateTime { get; set; }
        public string? Comment { get; set; }

        public Part? Part { get; set; }
    }
}
