using System;
using NAS.Core.Auth.Dtos;
using NAS.Core.Networking;
using UnityEngine;

namespace NAS.Core.Auth
{
    public sealed class CustomerAuthApi : ICustomerAuthApi
    {
        private readonly MonoBehaviour _coroutineRunner;
        private readonly ApiClient _client;

        public CustomerAuthApi(MonoBehaviour coroutineRunner, ApiSettings settings)
        {
            _coroutineRunner = coroutineRunner;
            _client = new ApiClient(settings);
        }

        public void Login(CustomerLoginRequest request, Action<ApiResult<CustomerAuthResponse>> completed)
        {
            _coroutineRunner.StartCoroutine(_client.PostJson("api/customer/auth/login", request, completed));
        }

        public void Register(CustomerRegisterRequest request, Action<ApiResult<CustomerAuthResponse>> completed)
        {
            _coroutineRunner.StartCoroutine(_client.PostJson("api/customer/auth/register", request, completed));
        }
    }
}
