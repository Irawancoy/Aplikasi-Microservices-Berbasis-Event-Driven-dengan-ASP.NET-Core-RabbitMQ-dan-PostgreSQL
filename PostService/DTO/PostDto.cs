using PostService.DTO;

namespace PostService.DTO
{
    public class PostDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public UserDto? User { get; set; }

    }
}
