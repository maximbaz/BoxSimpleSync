namespace BoxSimpleSync.API
{
    public class User
    {
        public string Login { get; private set; }
        public string Password { get; private set; }
        public AuthInfo AuthInfo { get; private set; }

        public User(string login, string password) {
            Login = login;
            Password = password;
            AuthInfo = new AuthInfo { ApiKey = "eu3dj9zermgofty4fi52qq1t0gfy0tih" };
        }
    }
}