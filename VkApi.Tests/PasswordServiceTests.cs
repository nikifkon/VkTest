using VkApi;

namespace VkApiTests;

public class PasswordServiceTests
{
    [Fact]
    public void Verify_Correct_ReturnTrue()
    {
        // Arrange
        const string actualPassword = "very_strong_secret";
        const string toVerify = "very_strong_secret";
        var passwordService = new PasswordService();
        var encrypted = passwordService.Encrypt(actualPassword);
        
        // Act
        var isCorrect = passwordService.Verify(toVerify, encrypted);

        // Assert
        Assert.True(isCorrect);
    }

    [Fact]
    public void Verify_Incorrect_ReturnFalse()
    {
        // Arrange
        const string actualPassword = "very_strong_secret";
        const string toVerify = "1very_strong_secret";
        var passwordService = new PasswordService();
        var encrypted = passwordService.Encrypt(actualPassword);
        
        // Act
        var isCorrect = passwordService.Verify(toVerify, encrypted);

        // Assert
        Assert.False(isCorrect);
    }
}