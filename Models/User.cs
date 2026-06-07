namespace GloryLikeBackend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string Surname { get; set; }

        public string FatherName { get; set; }

        public string Email { get; set; }
        public string PasswordHash { get; set; } = string.Empty;

        public int Age { get; set; }


    }
}
