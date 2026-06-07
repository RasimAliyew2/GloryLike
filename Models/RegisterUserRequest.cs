namespace GloryLikeBackend.Models
{
    public class RegisterUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
