using System;

namespace NAS.Core.Auth.Dtos
{
    [Serializable]
    public sealed class CustomerDto
    {
        public int id;
        public string email;
        public string firstName;
        public string lastName;
        public string cellNumber;
        public string status;
        public string createdAt;
    }
}
