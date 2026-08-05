using NAS.Core.Models;

namespace NAS.Core.Interfaces
{
    public interface IVehicleDataProvider
    {
        VehicleInfo GetCurrentVehicle();
        void UpdateVehicle(VehicleInfo vehicle);
    }
}