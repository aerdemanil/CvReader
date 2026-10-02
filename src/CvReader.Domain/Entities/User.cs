namespace CvReader.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.Employer;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class UserRoles
{
    public const string Employer = "Employer";
    public const string Admin = "Admin";
}
