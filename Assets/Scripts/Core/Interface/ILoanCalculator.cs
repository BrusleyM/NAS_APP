namespace NAS.Core.Interfaces
{
    public interface ILoanCalculator
    {
        float CalculateMonthlyPayment(float financed, float balloon, int months, float annualRatePercent);
    }
}