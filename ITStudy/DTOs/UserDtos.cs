using System.ComponentModel.DataAnnotations;

namespace ITStudy.DTOs
{
    public enum UserRole
    {
        Student = 1,
        Teacher = 2,
        Admin = 3
    }

    public class UserRegisterDto
    {
        [Required(ErrorMessage = "Username is required.")]
        [MinLength(3, ErrorMessage = "Username must be at least 3 characters long.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        public UserRole Role { get; set; } = UserRole.Student;
    }

    public class UserLoginDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters long.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UserPreferencesDto
    {
        [Required(ErrorMessage = "Preferred language is required.")]
        [RegularExpression("^(en|ar)$", ErrorMessage = "Language must be either 'en' or 'ar'.")]
        public string PreferredLanguage { get; set; } = "en";

        [Required(ErrorMessage = "Preferred theme is required.")]
        [RegularExpression("^(light|dark)$", ErrorMessage = "Theme must be either 'light' or 'dark'.")]
        public string PreferredTheme { get; set; } = "light";
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string PreferredLanguage { get; set; } = "en";
        public string PreferredTheme { get; set; } = "light";
    }
}