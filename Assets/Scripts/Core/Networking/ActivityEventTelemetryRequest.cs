using System;

namespace NAS.Core.Networking
{
    [Serializable]
    public sealed class ActivityEventTelemetryRequest
    {
        public int customerSessionId;
        public string clientEventId;
        public string eventType;
        public string occurredAt;
        public int vehicleModelId;
        public string categoryName;
    }
}
