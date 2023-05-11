using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using VkApi;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("VkApiDb");
builder.Services.AddNpgsql<UserContext>(connectionString);
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "VkTest API", Description = "Manage users", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PizzaStore API V1");
});
app.MapUserApi();

app.Run();

public partial class Program { }
