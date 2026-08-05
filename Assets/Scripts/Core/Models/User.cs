using System;

namespace NAS.Core.Models
{
    [System.Serializable]
    public class User
    {
        public string id;
        public string email;
        public string displayName;
        public DateTime createdAt;
        public UserPreferences preferences;

        public User()
        {
            id = Guid.NewGuid().ToString();
            createdAt = DateTime.UtcNow;
            preferences = new UserPreferences();
        }
    }
}