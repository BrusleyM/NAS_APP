using System;

namespace NAS.Core.Networking
{
    public interface IConfigurationApi
    {
        void CreateConfiguration(CreateConfigurationRequest request, string accessToken, Action<ApiResult<SavedConfigurationResponse>> completed);
    }
}
