using System;

namespace NAS.Core.Networking
{
    [Serializable]
    public sealed class ArSessionTelemetryRequest
    {
        public int customerSessionId;
        public string clientArSessionId;
        public int vehicleModelId;
        public string categoryName;
        public string startedAt;
        public string endedAt;
        public int placementCount;
        public int repositionCount;
        public int scaleCount;
        public int rotationCount;
    }
}
