using System.ComponentModel.DataAnnotations;

namespace ToDoListApp_Backend.DTOs
{
    public class LoginRequest
    {
        [Required]
        public string UsernameOrPhone { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string? AccessToken { get; set; }
        public string? IdToken { get; set; }
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string? Message { get; set; }
    }

    public class RegisterRequest
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Username { get; set; } = string.Empty;
    }

    public class ConfirmSignUpRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string ConfirmationCode { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
        public string Username { get; set; }
    }
}