using System;

namespace NAS.Core.Networking
{
    // Backend acks every telemetry POST with { id, clientXxxId } - only id is
    // ever needed (to capture the server-assigned CustomerSessionId after
    // starting a session), and JsonUtility.FromJson ignores JSON fields with
    // no matching member, so one shared response shape covers all 5 endpoints.
    [Serializable]
    public sealed class TelemetryAckResponse
    {
        public int id;
    }
}
