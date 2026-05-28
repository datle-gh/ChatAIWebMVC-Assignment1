namespace BusinessLogic.Services;

public static class UserRoleNames
{
    public const string Admin = "Admin";
    public const string Teacher = "Teacher";
    public const string Student = "Student";

    public static readonly string[] All =
    [
        Admin,
        Teacher,
        Student
    ];

    public static bool IsValid(string role)
    {
        return All.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    public static string Normalize(string role)
    {
        return All.First(item => string.Equals(item, role, StringComparison.OrdinalIgnoreCase));
    }
}
