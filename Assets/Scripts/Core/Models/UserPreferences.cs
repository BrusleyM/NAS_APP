namespace NAS.Core.Models
{
    [System.Serializable]
    public class UserPreferences
    {
        public string preferredCurrency = "USD";
        public bool receiveNewsletter = false;
        public bool biometricLoginEnabled = false;
    }
}