using System;

namespace NAS.Core.Vehicles.Dtos
{
    [Serializable]
    public sealed class VehicleColorDto
    {
        public int id;
        public string name;
        public string hexCode;
        public float priceAdjustment;
    }
}
