using System;

namespace NAS.Core.Networking
{
    public interface IEstimatorApi
    {
        void SubmitEstimate(SubmitEstimateRequest request, string accessToken, Action<ApiResult<SubmitEstimateResponse>> completed);
    }
}
