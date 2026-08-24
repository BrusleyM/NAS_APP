using System;

namespace NAS.Core.Networking
{
    [Serializable]
    public sealed class CustomerSessionTelemetryRequest
    {
        public string clientSessionId;
        // ISO 8601 ("o" format) - JsonUtility has no DateTime support, so
        // every telemetry timestamp in this project crosses the wire as a
        // string, same convention SubmitEstimateResponse.createdAt already uses.
        public string startedAt;
        public string appVersion;
        public string platform;
        public string deviceType;
    }
}
