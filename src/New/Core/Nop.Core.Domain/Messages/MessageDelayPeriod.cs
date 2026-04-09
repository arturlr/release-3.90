namespace Nop.Core.Domain.Messages
{

    public enum MessageDelayPeriod
    {

        Hours = 0,

        Days = 1
    }

    public static class MessageDelayPeriodExtensions
    {

        public static int ToHours(this MessageDelayPeriod period, int value)
        {
            switch (period)
            {
                case MessageDelayPeriod.Hours:
                    return value;
                case MessageDelayPeriod.Days:
                    return value * 24;
                default:
                    throw new ArgumentOutOfRangeException("MessageDelayPeriod");
            }
        }
    }
}
