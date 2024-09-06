using Microsoft.EntityFrameworkCore;
using PostService.Data;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Hosting;
using PostService.Models;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Menambahkan layanan ke dalam kontainer.
builder.Services.AddControllers();

// Mengonfigurasi Swagger/OpenAPI untuk dokumentasi API.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Koneksi ke database PostgreSQL dengan string koneksi yang diambil dari konfigurasi.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PostDbContext>(options =>
    options.UseNpgsql(connectionString)
    );

var app = builder.Build();

// Mengonfigurasi pipeline permintaan HTTP.
if (app.Environment.IsDevelopment())
{
    // Jika dalam mode pengembangan, pastikan database sudah dibuat.
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<PostDbContext>();
        context.Database.EnsureCreated();
    }
    // Mengaktifkan Swagger UI untuk dokumentasi API.
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Memulai proses untuk mendengarkan event dari RabbitMQ.
ListenForIntegrationEvents(app);

app.Run();

// Fungsi untuk mendengarkan event dari RabbitMQ dan memprosesnya.
static void ListenForIntegrationEvents(IHost host)
{
    var factory = new ConnectionFactory()
    {
        HostName = "localhost",
        UserName = "guest",
        Password = "guest",
        VirtualHost = "/"
    };
    var connection = factory.CreateConnection();
    var channel = connection.CreateModel();

    // Mendeklarasikan exchange 'User' dengan tipe direct.
    channel.ExchangeDeclare(exchange: "User", type: "direct", durable: true, autoDelete: false);

    // Mendeklarasikan queue 'user.postservice'.
    channel.QueueDeclare(queue: "user.postservice", durable: true, exclusive: false, autoDelete: false, arguments: null);

    // Menghubungkan queue ke exchange dengan routing key 'user.add' dan 'user.update'.
    channel.QueueBind(queue: "user.postservice", exchange: "User", routingKey: "user.add");
    channel.QueueBind(queue: "user.postservice", exchange: "User", routingKey: "user.update");

    var consumer = new EventingBasicConsumer(channel);

    // Menangani pesan yang diterima dari RabbitMQ.
    consumer.Received += async (model, ea) =>
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine($"[x] Received {message}");

        try
        {
            var data = JObject.Parse(message);
            var type = ea.RoutingKey;
            Console.WriteLine($"[x] Routing Key: {type}");

            using var localScope = host.Services.CreateScope();
            var context = localScope.ServiceProvider.GetRequiredService<PostDbContext>();

            if (type == "user.add")
            {
                var userId = data["id"]?.Value<int>();
                var userName = data["name"]?.Value<string>();

                if (userId != null && userName != null)
                {
                    var user = new User
                    {
                        Id = userId.Value,
                        Name = userName
                    };

                    if (!context.Users.Any(u => u.Id == user.Id))
                    {
                        context.Users.Add(user);
                        await context.SaveChangesAsync();
                        Console.WriteLine($"User {userName} berhasil disimpan.");
                    }
                }
            }
            else if (type == "user.update")
            {
                var userId = data["id"]?.Value<int>();
                var userName = data["name"]?.Value<string>();

                if (userId != null && userName != null)
                {
                    var user = context.Users.Find(userId);
                    if (user != null)
                    {
                        user.Name = userName;
                        await context.SaveChangesAsync();
                        Console.WriteLine($"User {userName} berhasil diupdate.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    };

    // Mulai konsumsi pesan dari queue dengan auto acknowledgment.
    channel.BasicConsume(queue: "user.postservice", autoAck: true, consumer: consumer);
}
