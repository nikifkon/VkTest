using Microsoft.EntityFrameworkCore;

public enum GroupCode
{
    Admin,
    User
}

public enum StateCode
{
    Active,
    Blocked
}

public class User
{
    public int Id { get; set; }
    public string? Login { get; set; }
    public string? Password { get; set; }
    public DateOnly CreatedDate { get; set; }
    
    public UserGroup Group { get; set; }
    public UserState State { get; set; }
}

public class UserGroup
{
    public int Id { get; set; }
    public GroupCode Code { get; set; }
    public string? Description { get; set; }
}

public class UserState
{
    public int Id { get; set; }
    public StateCode Code { get; set; }
    public string? Description { get; set; }
}

public class UserContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<UserGroup> UserGroups { get; set; }
    public DbSet<UserState> UserStates { get; set; }

    public UserContext(DbContextOptions<UserContext> options) : base(options)
    {
        Database.EnsureCreated();
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserGroup>()
            .HasData(new UserGroup() { Id = 1, Code = GroupCode.Admin, Description = "This is admin user group" },
                new UserGroup() { Id = 2, Code = GroupCode.User, Description = "" });
        
        modelBuilder.Entity<UserState>()
            .HasData(new UserState() { Id = 1, Code = StateCode.Active, Description = "Active user" },
                new UserState() { Id = 2, Code = StateCode.Blocked, Description = "Deleted user" });

        modelBuilder.Entity<User>()
            .HasIndex(user => user.Login)
            .IsUnique();
    }
}