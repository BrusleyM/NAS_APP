using System;

namespace NAS.Core.Networking
{
    [Serializable]
    public sealed class SubmitEstimateRequest
    {
        public int vehicleModelId;
        public float depositAmount;
        public float tradeInValue;
        public int termMonths;
        public float interestRate;
        public float estimatedMonthly;
        public float balloonAmount;
    }
}
