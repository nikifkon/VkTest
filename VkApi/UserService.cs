using Microsoft.EntityFrameworkCore;

namespace VkApi;

public class UserService : IUserService
{
    private readonly UserContext _dbContext;
    private readonly IPasswordService _passwordService;
    
    public UserService(UserContext dbContext, IPasswordService passwordService)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
    }
    
    public async Task<IList<User>> GetAll()
    {
        var users = _dbContext.Users.Where(user => user.State.Code != StateCode.Blocked).Include(user => user.State).Include(user => user.Group);
        return await users.ToListAsync();
    }

    public async Task<User?> Get(string login)
    {
        var user = _dbContext.Users.Where(user => user.Login == login && user.State.Code == StateCode.Active).Include(user => user.State).Include(user => user.Group);
        return user.Any() ? await user.FirstAsync() : null;
    }

    public async Task<(User? newUser, string errorMsg)> Create(User user)
    {
        if (user.Group?.Code == GroupCode.Admin)
        {
            if (_dbContext.Users.Any(u => u.Group.Code == GroupCode.Admin))
            {
                return (null, "Admin has already created and must be single");
            }
        }

        user.Password = _passwordService.Encrypt(user.Password!);
        var defaultUserGroup = await _dbContext.UserGroups.Where(group => group.Code == GroupCode.User).FirstAsync();
        var defaultUserState = await _dbContext.UserStates.Where(state => state.Code == StateCode.Active).FirstAsync();
        user.State = defaultUserState;
        user.Group = defaultUserGroup;
        user.CreatedDate = DateOnly.FromDateTime(DateTime.Today);
        _dbContext.Add(user);
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (null, "Login has already taken");
        }
        return (user, "");
    }

    public async Task<bool> Delete(string login)
    {
        var users = _dbContext.Users.Where(user => user.Login == login);
        if (!users.Any())
            return false;
        var user = await users.FirstAsync();
        user.State = await _dbContext.UserStates.Where(state => state.Code == StateCode.Blocked).FirstAsync();
        await _dbContext.SaveChangesAsync();
        return true;
    }
}