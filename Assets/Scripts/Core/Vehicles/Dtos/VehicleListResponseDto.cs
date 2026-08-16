using System;

namespace NAS.Core.Vehicles.Dtos
{
    // Matches the backend's { "vehicles": [...] } object wrapper - JsonUtility
    // cannot parse a bare top-level JSON array, so the API deliberately never
    // returns one for this endpoint.
    [Serializable]
    public sealed class VehicleListResponseDto
    {
        public VehicleDto[] vehicles;
    }
}
