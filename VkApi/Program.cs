using VkApi;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("VkApiDb");
builder.Services.AddNpgsql<UserContext>(connectionString);
builder.Services.AddSingleton<IPasswordService, PasswordService>();

var app = builder.Build();

app.MapGet("/", (IPasswordService ps) => ps.Encrypt("Hello, World!"));

app.Run();

public partial class Program { }
