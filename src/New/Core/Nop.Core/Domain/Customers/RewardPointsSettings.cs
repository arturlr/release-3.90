using Nop.Core.Configuration;

namespace Nop.Core.Domain.Customers;

public class RewardPointsSettings : ISettings
{
    public bool Enabled { get; set; }
    public decimal ExchangeRate { get; set; }
    public int MinimumRewardPointsToUse { get; set; }
    public int PointsForRegistration { get; set; }
    public decimal PointsForPurchases_Amount { get; set; }
    public int PointsForPurchases_Points { get; set; }
    public int ActivationDelay { get; set; }
    public int ActivationDelayPeriodId { get; set; }
    public bool DisplayHowMuchWillBeEarned { get; set; }
    public bool PointsAccumulatedForAllStores { get; set; }
    public int PageSize { get; set; }
}
