using NAS.Core.Models;

namespace NAS.Core.Services
{
    public static class UserSession
    {
        public static User CurrentUser { get; set; }
        
        public static void Logout()
        {
            CurrentUser = null;
        }
    }
}