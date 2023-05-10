var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("VkApiDb");
builder.Services.AddNpgsql<UserContext>(connectionString);


var app = builder.Build();

app.MapGet("/", () => "Hello World!!!!");

app.Run();

public partial class Program { }
