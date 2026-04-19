using NAS.Core.Interfaces;
using UnityEngine;

namespace NAS.Core.Services
{
    public class LoanCalculator : ILoanCalculator
    {
        public float CalculateMonthlyPayment(float financed, float balloon, int months, float annualRatePercent)
        {
            float principal = financed - balloon;
            if (principal <= 0) return 0;
            
            float monthlyRate = annualRatePercent / 100f / 12f;
            if (monthlyRate == 0f) return principal / months;
            
            float factor = Mathf.Pow(1 + monthlyRate, months);
            return principal * (monthlyRate * factor) / (factor - 1);
        }
    }
}