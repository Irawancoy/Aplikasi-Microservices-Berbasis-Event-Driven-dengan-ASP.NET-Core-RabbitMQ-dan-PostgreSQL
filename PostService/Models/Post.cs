using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PostService.Models
{
    public partial class Post
    {
        public int PostId { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;

  
        public int UserId { get; set; }

        public virtual User? User { get; set; }
    }
}
