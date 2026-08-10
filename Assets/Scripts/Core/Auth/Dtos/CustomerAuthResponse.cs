using System;

namespace NAS.Core.Auth.Dtos
{
    [Serializable]
    public sealed class CustomerAuthResponse
    {
        public string accessToken;
        public string expiresAtUtc;
        public CustomerDto customer;
    }
}
