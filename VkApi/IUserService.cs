namespace VkApi;

public interface IUserService
{
    Task<IList<User>> GetAll();
    Task<User?> Get(string login);
    Task<(User? newUser, string errorMsg)> Create(User user);
    Task<bool> Delete(string login);
}