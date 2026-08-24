using System;

namespace NAS.Core.Networking
{
    // trimLevelId/interiorOptionId/wheelOptionId are still intentionally
    // omitted - there's no Customize UI yet for a customer to pick any of
    // those, so the backend fills them in with that vehicle's default
    // option. When that UI exists, this request can start setting them too
    // without any endpoint shape change (see CreateCustomerConfigurationRequestDto
    // on the backend).
    [Serializable]
    public sealed class CreateConfigurationRequest
    {
        public int vehicleModelId;

        // 0 = customer never picked a paint swatch this AR visit - matches
        // the backend's ">0 means set" convention (see
        // SavedConfigurationService.CreateForCustomerAsync), since
        // JsonUtility can't send a real null for an unset int.
        public int exteriorColorOptionId;
    }
}
