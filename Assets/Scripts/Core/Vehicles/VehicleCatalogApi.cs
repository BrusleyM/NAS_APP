using System;
using System.Collections.Generic;
using System.Linq;
using NAS.Core.Models;
using NAS.Core.Networking;
using NAS.Core.Vehicles.Dtos;
using UnityEngine;

namespace NAS.Core.Vehicles
{
    public sealed class VehicleCatalogApi : IVehicleCatalogApi
    {
        private readonly MonoBehaviour _coroutineRunner;
        private readonly ApiClient _client;

        public VehicleCatalogApi(MonoBehaviour coroutineRunner, ApiSettings settings, bool trustAnyCertificate = false)
        {
            _coroutineRunner = coroutineRunner;
            _client = new ApiClient(settings, trustAnyCertificate);
        }

        public void GetVehicles(int? dealershipId, string accessToken, Action<ApiResult<List<CarData>>> completed)
        {
            var path = "api/customer/vehicles" + (dealershipId.HasValue ? $"?dealershipId={dealershipId.Value}" : "");
            _coroutineRunner.StartCoroutine(_client.GetJson<VehicleListResponseDto>(path, result => completed?.Invoke(Map(result)), accessToken));
        }

        private static ApiResult<List<CarData>> Map(ApiResult<VehicleListResponseDto> result)
        {
            if (!result.Success)
                return ApiResult<List<CarData>>.FromError(result.Error);

            var cars = (result.Value?.vehicles ?? Array.Empty<VehicleDto>())
                .Select(VehicleDtoMapper.ToCarData)
                .ToList();
            return ApiResult<List<CarData>>.FromSuccess(cars);
        }
    }
}
