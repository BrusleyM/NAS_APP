using NAS.Core.Models;
using NAS.Core.Vehicles.Dtos;
using UnityEngine;

namespace NAS.Core.Vehicles
{
    internal static class VehicleDtoMapper
    {
        public static CarData ToCarData(VehicleDto dto)
        {
            var car = ScriptableObject.CreateInstance<CarData>();
            car.id = dto.id;
            car.carName = dto.name;
            car.year = dto.year;
            car.retailPrice = dto.basePrice;
            car.type = dto.bodyType;
            car.category = dto.powertrain;
            car.imageUrl = dto.imageUrl;
            return car;
        }
    }
}
