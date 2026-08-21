using System;
using UnityEngine;

namespace NAS.Core.Networking
{
    public sealed class EstimatorApi : IEstimatorApi
    {
        private readonly MonoBehaviour _coroutineRunner;
        private readonly ApiClient _client;

        public EstimatorApi(MonoBehaviour coroutineRunner, ApiSettings settings, bool trustAnyCertificate = false)
        {
            _coroutineRunner = coroutineRunner;
            _client = new ApiClient(settings, trustAnyCertificate);
        }

        public void SubmitEstimate(SubmitEstimateRequest request, string accessToken, Action<ApiResult<SubmitEstimateResponse>> completed)
        {
            _coroutineRunner.StartCoroutine(_client.PostJson<SubmitEstimateRequest, SubmitEstimateResponse>(
                "api/estimator/submissions", request, completed, accessToken));
        }
    }
}
