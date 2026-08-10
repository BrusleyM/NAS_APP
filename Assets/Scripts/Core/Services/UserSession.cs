using NAS.Core.Models;

namespace NAS.Core.Services
{
    public static class UserSession
    {
        public static User CurrentUser { get; set; }
        public static string AccessToken { get; set; }
        public static string ExpiresAtUtc { get; set; }

        public static void Logout()
        {
            CurrentUser = null;
            AccessToken = null;
            ExpiresAtUtc = null;
        }
    }
}
