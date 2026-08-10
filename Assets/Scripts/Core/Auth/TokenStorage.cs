using UnityEngine;

namespace NAS.Core.Auth
{
    public sealed class TokenStorage
    {
        private const string AccessTokenKey = "nas.customer.access_token";
        private const string ExpiryKey = "nas.customer.expires_at_utc";

        private readonly bool _persist;
        private string _accessToken;
        private string _expiresAtUtc;

        public TokenStorage(bool persist)
        {
            _persist = persist;
            if (_persist)
            {
                _accessToken = PlayerPrefs.GetString(AccessTokenKey, null);
                _expiresAtUtc = PlayerPrefs.GetString(ExpiryKey, null);
            }
        }

        public string AccessToken => _accessToken;
        public string ExpiresAtUtc => _expiresAtUtc;

        public void Save(string accessToken, string expiresAtUtc)
        {
            _accessToken = accessToken;
            _expiresAtUtc = expiresAtUtc;
            if (!_persist) return;

            PlayerPrefs.SetString(AccessTokenKey, accessToken ?? string.Empty);
            PlayerPrefs.SetString(ExpiryKey, expiresAtUtc ?? string.Empty);
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            _accessToken = null;
            _expiresAtUtc = null;
            PlayerPrefs.DeleteKey(AccessTokenKey);
            PlayerPrefs.DeleteKey(ExpiryKey);
            PlayerPrefs.Save();
        }
    }
}
