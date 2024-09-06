using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostService.Data;
using PostService.Models;
using PostService.DTO;

namespace PostService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly PostDbContext _context;

        // Constructor untuk menginisialisasi DbContext
        public PostController(PostDbContext context)
        {
            _context = context;
        }

        // Mengambil semua post dan mengembalikannya sebagai DTO
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PostDto>>> GetPosts()
        {
            var posts = await _context.Posts.Include(x => x.User).ToListAsync(); // Ambil semua post dari database termasuk data user terkait
            var postDtos = posts.Select(post => new PostDto
            {
                Id = post.PostId,
                Title = post.Title,
                Content = post.Content,
                User = post.User != null ? new UserDto
                {
                    Id = post.User.Id,
                    Name = post.User.Name
                } : new UserDto()
            }).ToList(); // Konversi data post ke dalam format DTO

            return Ok(postDtos); // Kembalikan hasil sebagai response HTTP 200 OK
        }

        // Mengambil post berdasarkan ID dan mengembalikannya sebagai DTO
        [HttpGet("{id}")]
        public async Task<ActionResult<PostDto>> GetPost(int id)
        {
            var post = await _context.Posts.Include(x => x.User).FirstOrDefaultAsync(p => p.PostId == id); // Ambil post berdasarkan ID dari database

            if (post == null)
            {
                return NotFound(); // Jika post tidak ditemukan, kembalikan HTTP 404 Not Found
            }

            var postDto = new PostDto
            {
                Id = post.PostId,
                Title = post.Title,
                Content = post.Content,
                User = post.User != null ? new UserDto
                {
                    Id = post.User.Id,
                    Name = post.User.Name
                } : new UserDto()
            };

            return Ok(postDto); // Kembalikan post sebagai DTO dengan HTTP 200 OK
        }

        // Menambahkan post baru ke database dan mengembalikan hasilnya sebagai DTO
        [HttpPost]
        public async Task<ActionResult<PostDto>> PostPost(Post post)
        {
            // Pastikan user yang dikaitkan ada di database
            var user = await _context.Users.FindAsync(post.UserId); // Cari user berdasarkan ID dari database
            if (user == null)
            {
                return BadRequest("User tidak ditemukan."); // Jika user tidak ada, kembalikan HTTP 400 Bad Request
            }

            post.User = user; // Kaitkan user dengan post
            _context.Posts.Add(post); // Tambahkan post ke dalam DbContext
            await _context.SaveChangesAsync(); // Simpan perubahan ke database

            // Buat DTO untuk response
            var postDto = new PostDto
            {
                Id = post.PostId,
                Title = post.Title,
                Content = post.Content,
                User = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name
                }
            };

            return CreatedAtAction(nameof(GetPost), new { id = post.PostId }, postDto); // Kembalikan response HTTP 201 Created dengan lokasi resource yang baru dibuat
        }
    }

}

