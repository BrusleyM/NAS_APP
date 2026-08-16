using System;
using System.Collections.Generic;
using NAS.Core.Models;
using NAS.Core.Networking;

namespace NAS.Core.Vehicles
{
    public interface IVehicleCatalogApi
    {
        void GetVehicles(int? dealershipId, string accessToken, Action<ApiResult<List<CarData>>> completed);
    }
}
