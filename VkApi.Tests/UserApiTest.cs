using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace VkApiTests;

public class TestDatabaseFixture
{
    private const string ConnectionString = @"host=localhost;port=5431;database=vktest;username=vktest;password=vktest";

    private static readonly object _lock = new();
    public readonly Dictionary<GroupCode, UserGroup> Groups = new();
    public readonly Dictionary<StateCode, UserState> States = new();
    public readonly List<User> Users = new();
    private static bool _databaseInitialized;

    public TestDatabaseFixture()
    {
        lock (_lock)
        {
            if (_databaseInitialized) return;
            using var context = CreateContext();

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            Groups[GroupCode.Admin] = new UserGroup() { Code = GroupCode.Admin, Description = "This is admin user group" };
            Groups[GroupCode.User] = new UserGroup() { Code = GroupCode.User, Description = "" };
            States[StateCode.Active] = new UserState() { Code = StateCode.Active, Description = "Active user" };
            States[StateCode.Blocked] = new UserState() { Code = StateCode.Blocked, Description = "Deleted user" };
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
            context.AddRange(
                Groups[GroupCode.Admin],
                Groups[GroupCode.User],
                States[StateCode.Active],
                States[StateCode.Blocked]
            );
            context.AddRange(Users);
            context.SaveChanges();

            _databaseInitialized = true;
        }
    }

    private UserContext CreateContext()
        => new (new DbContextOptionsBuilder<UserContext>()
                .UseNpgsql(ConnectionString)
                .Options);
}

public class ErrorMsg
{
    public string Message { get; set; }
}

public class UnitTest1 : IClassFixture<TestDatabaseFixture>
{
    private TestDatabaseFixture _dbFixture;
    
    public UnitTest1(TestDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    [Fact]
    public async Task TestCreateEndpoint()
    {
        // Arrange
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var user = new User { Login = "new user", Password = "secret1" };
        
        // Act
        var response = await client.PostAsJsonAsync("/user", user);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdUser = await response.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(createdUser);
        Assert.Equal(_dbFixture.States[StateCode.Active], createdUser.State);
        Assert.Equal(_dbFixture.Groups[GroupCode.User], createdUser.Group);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), createdUser.CreatedDate);
        Assert.Equal(user.Login, createdUser.Login);
    }
    
    [Fact]
    public async Task TestGetEndpoint()
    {
        // Arrange
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var userInDb = _dbFixture.Users[0];
        
        // Act
        var response = await client.GetAsync($"/user/${userInDb.Login}");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<User>();
        Assert.Equal(userInDb, user);
    }

    [Fact]
    public async Task TestCreateEndpoint_AlreadyExists()
    {
        // Arrange
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var user = new User { Login =_dbFixture.Users[0].Login, Password = "123" };
        
        // Act
        var response = await client.PostAsJsonAsync("/user", user);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorMsg = await response.Content.ReadFromJsonAsync<ErrorMsg>();
        Assert.NotNull(errorMsg);
        Assert.Equal("Login has already taken", errorMsg.Message);
    }

    [Fact]
    public async Task TestCreateEndpoint_SingleAdmin()
    {
        // Arrange
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var user = new User { Login = "Second admin", Password = "123", Group = _dbFixture.Groups[GroupCode.Admin]};
        
        // Act
        var response = await client.PostAsJsonAsync("/user", user);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorMsg = await response.Content.ReadFromJsonAsync<ErrorMsg>();
        Assert.NotNull(errorMsg);
        Assert.Equal("Admin has already created and must be single", errorMsg.Message);
    }
    
    
    [Fact]
    public async Task TestListEndpoint()
    {
        // Arrange
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        
        // Act
        var response = await client.GetAsync("/user");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var users = await response.Content.ReadFromJsonAsync<IList<User>>();
        Assert.NotNull(users);
        _dbFixture.Users.Where(user => user.State.Code == StateCode.Active).AssertListEqual(users, user => user.Id);
    }
    
        
    [Fact]
    public async Task TestDeleteEndpoint()
    {
        // Arrange
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var userToDelete = _dbFixture.Users.First(user => user.State.Code != StateCode.Blocked);
        
        // Act
        var response = await client.DeleteAsync($"/user${userToDelete.Login}");
        var getResponse = await client.GetAsync($"/user/${userToDelete.Login}");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}

public static class AssertExt
{
    public static void AssertListEqual<T>(this IEnumerable<T> expected, IEnumerable<T> actual, Func<T, int> keySelector)
    {
        Assert.Equal(expected.OrderBy(keySelector), actual.OrderBy(keySelector));
    }
}