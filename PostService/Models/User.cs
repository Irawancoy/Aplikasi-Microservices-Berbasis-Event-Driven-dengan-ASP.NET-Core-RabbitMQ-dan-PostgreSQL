using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PostService.Models
{
    public partial class User
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
