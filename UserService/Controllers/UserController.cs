using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Models;
using RabbitMQ.Client;
using System.Text;
using Newtonsoft.Json;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserDbContext _context;

        // Konstruktor untuk menginisialisasi konteks database
        public UserController(UserDbContext context)
        {
            _context = context;
        }

        // GET: api/User
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            // Mengambil daftar semua pengguna dari database
            return await _context.Users.ToListAsync();
        }

        // Fungsi untuk mengirimkan event ke RabbitMQ
        private void PublishToMessageQueue(string integrationEvent, string eventData)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost", // Nama host RabbitMQ
                UserName = "guest",     // Nama pengguna RabbitMQ
                Password = "guest",     // Kata sandi RabbitMQ
                VirtualHost = "/"       // Virtual host RabbitMQ
            };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            var body = Encoding.UTF8.GetBytes(eventData); // Mengubah data event menjadi byte array
            channel.BasicPublish(exchange: "User",       // Nama exchange RabbitMQ
                                 routingKey: integrationEvent, // Routing key untuk event
                                 basicProperties: null,
                                 body: body); // Mengirimkan pesan ke RabbitMQ
        }

        // GET: api/User/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            // Mencari pengguna berdasarkan ID dari database
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                // Mengembalikan NotFound jika pengguna tidak ditemukan
                return NotFound();
            }

            return user;
        }

        // PUT: api/User/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.Id)
            {
                // Mengembalikan BadRequest jika ID tidak cocok
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified; // Menandai entitas sebagai diperbarui

            try
            {
                await _context.SaveChangesAsync(); // Menyimpan perubahan ke database
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    // Mengembalikan NotFound jika pengguna tidak ditemukan
                    return NotFound();
                }
                else
                {
                    throw; // Melemparkan pengecualian lainnya jika ada
                }
            }

            // Setelah update, kirimkan event ke RabbitMQ
            var integrationEventData = JsonConvert.SerializeObject(new
            {
                id = user.Id,
                name = user.Name
            });

            PublishToMessageQueue("user.update", integrationEventData);

            return NoContent();
        }

        // POST: api/User
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            // Cek apakah pengguna dengan nama yang sama sudah ada
            if (_context.Users.Any(u => u.Name == user.Name))
            {
                return Conflict("User with the same name already exists.");
            }

            _context.Users.Add(user); // Menambahkan pengguna baru ke database
            await _context.SaveChangesAsync(); // Menyimpan perubahan ke database

            // Setelah pengguna dibuat, kirimkan event ke RabbitMQ
            var integrationEventData = JsonConvert.SerializeObject(new
            {
                id = user.Id,
                name = user.Name
            });

            PublishToMessageQueue("user.add", integrationEventData);

            // Mengarahkan ke GetUser untuk pengguna yang baru dibuat
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        // Fungsi untuk mengecek apakah pengguna dengan ID tertentu ada
        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
