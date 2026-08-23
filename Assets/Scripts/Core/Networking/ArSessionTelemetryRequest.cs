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
        // No gesture-manipulation system exists yet (only tap-to-place is
        // implemented) - always 0 until that gets built as its own feature.
        // See .claude/CLAUDE.md's AR viewport section for the gap.
        public int repositionCount;
        public int scaleCount;
    }
}
