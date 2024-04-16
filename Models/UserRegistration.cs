namespace Stepmeet_BE.Models
{
    public class UserRegistration
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? DPUrl { get; set; } = "";
        public bool IsPrivate { get; set; } = false; // Default value set to false
        public List<int> Favorites { get; set; } = new List<int>();
        public List<string> Following { get; set; } = new List<string>(); // Array of strings for following
        public List<string> Completed { get; set; } = new List<string>(); // Array of strings for completed trails
    }
}
