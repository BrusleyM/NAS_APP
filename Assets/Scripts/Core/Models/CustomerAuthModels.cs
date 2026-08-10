using System;

namespace NAS.Core.Models
{
    [Serializable]
    public class CustomerRegisterRequest
    {
        public string firstName;
        public string lastName;
        public string cellNumber;
        public string email;
        public string password;
    }

    [Serializable]
    public class CustomerLoginRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    public class CustomerAuthResponse
    {
        public string accessToken;
        public string expiresAtUtc;
        public CustomerResponse customer;
    }

    [Serializable]
    public class CustomerResponse
    {
        public int id;
        public string email;
        public string firstName;
        public string lastName;
        public string cellNumber;
        public string status;
        public string createdAt;
    }

    [Serializable]
    public class ApiProblemDetailsResponse
    {
        public string type;
        public string title;
        public int status;
        public string detail;
        public string instance;
        public string errorCode;
        public string traceId;
    }
}
