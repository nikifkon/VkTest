namespace VkApi;

public interface IPasswordService
{
    string Encrypt(string password);
    bool Verify(string password, string encrypted);
}