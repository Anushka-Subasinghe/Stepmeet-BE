namespace Stepmeet_BE.Models
{
    public class UserRegistration
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string?Password { get; set; }
        public List<int> Favorites { get; set; } = new List<int>();
    }
}
