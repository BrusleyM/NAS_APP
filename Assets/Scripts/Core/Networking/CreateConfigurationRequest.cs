using System;

namespace NAS.Core.Networking
{
    // trimLevelId/exteriorColorOptionId/interiorOptionId/wheelOptionId are
    // intentionally omitted - there's no Customize UI yet for a customer to
    // pick any of them, so the backend fills all four in with that vehicle's
    // default option. When that UI exists, this request can start setting
    // them without any endpoint shape change (see CreateCustomerConfigurationRequestDto
    // on the backend).
    [Serializable]
    public sealed class CreateConfigurationRequest
    {
        public int vehicleModelId;
    }
}
