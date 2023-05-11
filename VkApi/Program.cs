using VkApi;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("VkApiDb");
builder.Services.AddNpgsql<UserContext>(connectionString);
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

app.MapUserApi();

app.Run();

public partial class Program { }
