namespace ST10296904_POE_Part1_Prog7312.Models
{
    public class UserModel
    {
       
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }

    public static class UserDatabase
    {
        // In-memory user list for demo
        public static List<UserModel> Users = new List<UserModel>
        {
            new UserModel { Username = "admin", Password = "admin123", Role = "Admin" }
        };
    }
}


