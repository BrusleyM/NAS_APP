using System;
using UnityEngine;

namespace NAS.Core.Networking
{
    public sealed class ConfigurationApi : IConfigurationApi
    {
        private readonly MonoBehaviour _coroutineRunner;
        private readonly ApiClient _client;

        public ConfigurationApi(MonoBehaviour coroutineRunner, ApiSettings settings, bool trustAnyCertificate = false)
        {
            _coroutineRunner = coroutineRunner;
            _client = new ApiClient(settings, trustAnyCertificate);
        }

        public void CreateConfiguration(CreateConfigurationRequest request, string accessToken, Action<ApiResult<SavedConfigurationResponse>> completed)
        {
            _coroutineRunner.StartCoroutine(_client.PostJson<CreateConfigurationRequest, SavedConfigurationResponse>(
                "api/customer/configurations", request, completed, accessToken));
        }
    }
}
