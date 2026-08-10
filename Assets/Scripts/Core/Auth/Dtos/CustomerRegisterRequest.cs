using System;

namespace NAS.Core.Auth.Dtos
{
    [Serializable]
    public sealed class CustomerRegisterRequest
    {
        public string firstName;
        public string lastName;
        public string cellNumber;
        public string email;
        public string password;
    }
}
