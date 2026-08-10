using System;
using NAS.Core.Auth.Dtos;
using NAS.Core.Networking;

namespace NAS.Core.Auth
{
    public interface ICustomerAuthApi
    {
        void Login(CustomerLoginRequest request, Action<ApiResult<CustomerAuthResponse>> completed);
        void Register(CustomerRegisterRequest request, Action<ApiResult<CustomerAuthResponse>> completed);
    }
}
