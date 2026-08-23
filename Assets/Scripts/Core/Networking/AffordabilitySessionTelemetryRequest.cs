using System;

namespace NAS.Core.Networking
{
    [Serializable]
    public sealed class AffordabilitySessionTelemetryRequest
    {
        public int customerSessionId;
        public string clientAffordabilitySessionId;
        public int vehicleModelId;
        public string startedAt;
        public string endedAt;
        public float initialDeposit;
        public float finalDeposit;
        public float initialTradeIn;
        public float finalTradeIn;
        public int initialTermMonths;
        public int finalTermMonths;
        public float initialInterestRate;
        public float finalInterestRate;
        public float initialMonthlyPayment;
        public float finalMonthlyPayment;
        public int calculationCount;
        public int depositChangeCount;
        public int tradeInChangeCount;
        public int termChangeCount;
        public int interestRateChangeCount;
    }
}
