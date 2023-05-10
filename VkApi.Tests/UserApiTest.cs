using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace VkApiTests;

public class CustomWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                     typeof(DbContextOptions<UserContext>));

            services.Remove(dbContextDescriptor);

            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                     typeof(DbConnection));

            services.Remove(dbConnectionDescriptor);

            // Create open SqliteConnection so EF won't automatically close it.
            services.AddSingleton<DbConnection>(container =>
            {
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();
                
                return connection;
            });

            services.AddDbContext<UserContext>((container, options) =>
            {
                var connection = container.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });
        });
        
        builder.UseEnvironment("Development");
    }
}

public class TestDatabaseFixture
{
    public readonly Dictionary<GroupCode, UserGroup> Groups = new();
    public readonly Dictionary<StateCode, UserState> States = new();
    public readonly List<User> Users = new();

    public TestDatabaseFixture(UserContext context)
    {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        Groups[GroupCode.Admin] = context.UserGroups.First(group => group.Code == GroupCode.Admin);
        Groups[GroupCode.User] = context.UserGroups.First(group => group.Code == GroupCode.User);
        States[StateCode.Active] = context.UserStates.First(state => state.Code == StateCode.Active);
        States[StateCode.Blocked] = context.UserStates.First(state => state.Code == StateCode.Blocked);
        Users = new()
        {
            new User
            {
                Id = 1,
                Login = "First",
                Password = "secret1",
                CreatedDate = new DateOnly(2023, 5, 9),
                Group = Groups[GroupCode.Admin],
                State = States[StateCode.Active]
            },
            new User
            {
                Id = 2,
                Login = "Second",
                Password = "secret2",
                CreatedDate = new DateOnly(2023, 5, 10),
                Group = Groups[GroupCode.User],
                State = States[StateCode.Active]
            },
            new User
            {
                Id = 3,
                Login = "Bloced",
                Password = "secret3",
                CreatedDate = new DateOnly(2023, 5, 8),
                Group = Groups[GroupCode.User],
                State = States[StateCode.Blocked]
            }
        };
        context.AddRange(Users);
        context.SaveChanges();

    }
}

public class UnitTest1
{
    async private Task<(HttpClient client, TestDatabaseFixture dbFixture)> SetupClient()
    {
        var application = new CustomWebApplicationFactory<Program>();
        var scopeFactory = application.Services.GetService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetService<UserContext>();
        var _dbFixture = new TestDatabaseFixture(context);
        var client = application.CreateClient();
        return (client, _dbFixture);
    }
    
    [Fact]
    public async Task TestCreateEndpoint()
    {
        // Arrange
        var (client, dbFixture) = await SetupClient();
        var user = new User { Login = "new user", Password = "secret1" };
        
        // Act
        var response = await client.PostAsJsonAsync("/user", user);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    
        var createdUser = await response.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(createdUser);
        Assert.Equivalent(dbFixture.States[StateCode.Active], createdUser.State);
        Assert.Equivalent(dbFixture.Groups[GroupCode.User], createdUser.Group);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), createdUser.CreatedDate);
        Assert.Equal(user.Login, createdUser.Login);
    }
    
    [Fact]
    public async Task TestGetEndpoint()
    {
        // Arrange
        var (client, dbFixture) = await SetupClient();
        var userInDb = dbFixture.Users[0];
        
        // Act
        var response = await client.GetAsync($"/user/{userInDb.Login}");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    
        var user = await response.Content.ReadFromJsonAsync<User>();
        Assert.Equivalent(userInDb, user);
    }
    
    [Fact]
    public async Task TestCreateEndpoint_AlreadyExists()
    {
        // Arrange
        var (client, dbFixture) = await SetupClient();
        var user = new User { Login = dbFixture.Users[0].Login, Password = "123" };
        
        // Act
        var response = await client.PostAsJsonAsync("/user", user);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errorMsg = await response.Content.ReadAsStringAsync();
        Assert.Equal("\"Login has already taken\"", errorMsg);
    }
    
    [Fact]
    public async Task TestCreateEndpoint_SingleAdmin()
    {
        // Arrange
        var (client, dbFixture) = await SetupClient();
        var user = new User { Login = "Second admin", Password = "123", Group = dbFixture.Groups[GroupCode.Admin]};
        
        // Act
        var response = await client.PostAsJsonAsync("/user", user);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    
        var errorMsg = await response.Content.ReadAsStringAsync();
        Assert.Equal("\"Admin has already created and must be single\"", errorMsg);
    }
    
    
    [Fact]
    public async Task TestListEndpoint()
    {
        // Arrange
        var (client, dbFixture) = await SetupClient();
        
        // Act
        var response = await client.GetAsync("/user");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    
        var users = await response.Content.ReadFromJsonAsync<IList<User>>();
        Assert.NotNull(users);
        dbFixture.Users.Where(user => user.State.Code == StateCode.Active).AssertListEqual(users, user => user.Id);
    }
    
        
    [Fact]
    public async Task TestDeleteEndpoint()
    {
        // Arrange
        var (client, dbFixture) = await SetupClient();
        var userToDelete = dbFixture.Users.First(user => user.State.Code != StateCode.Blocked);
        
        // Act
        var response = await client.DeleteAsync($"/user/{userToDelete.Login}");
        var getResponse = await client.GetAsync($"/user/{userToDelete.Login}");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}

public static class AssertExt
{
    public static void AssertListEqual<T>(this IEnumerable<T> expected, IEnumerable<T> actual, Func<T, int> keySelector)
    {
        Assert.Equivalent(expected.OrderBy(keySelector), actual.OrderBy(keySelector));
    }
}