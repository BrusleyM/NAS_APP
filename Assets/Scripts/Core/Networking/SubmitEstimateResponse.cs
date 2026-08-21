using System;

namespace NAS.Core.Networking
{
    [Serializable]
    public sealed class SubmitEstimateResponse
    {
        public int id;
        public string status;
        public string createdAt;
    }
}
