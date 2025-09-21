namespace ToDoListApp_Backend.Models
{
    public class CognitoAuthResult
    {
        public bool IsSuccess { get; set; }
        public string? AccessToken { get; set; }
        public string? IdToken { get; set; }
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class UserInfo
    {
        public string? Username { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Name { get; set; }
        public string? CognitoSub { get; set; }
    }
}