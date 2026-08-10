using System;

namespace NAS.Core.Auth.Dtos
{
    [Serializable]
    public sealed class CustomerLoginRequest
    {
        public string email;
        public string password;
    }
}
