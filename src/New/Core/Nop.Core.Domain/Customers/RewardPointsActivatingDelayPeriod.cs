namespace Nop.Core.Domain.Customers
{

    public enum RewardPointsActivatingDelayPeriod
    {

        Hours = 0,

        Days = 1
    }

    public static class RewardPointsActivatingDelayPeriodExtensions
    {

        public static int ToHours(this RewardPointsActivatingDelayPeriod period, int value)
        {
            switch (period)
            {
                case RewardPointsActivatingDelayPeriod.Hours:
                    return value;
                case RewardPointsActivatingDelayPeriod.Days:
                    return value * 24;
                default:
                    throw new ArgumentOutOfRangeException("RewardPointsActivatingDelayPeriod");
            }
        }
    }
}
