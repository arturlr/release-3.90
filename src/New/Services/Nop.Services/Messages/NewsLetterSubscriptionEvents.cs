using Nop.Core.Domain.Messages;

namespace Nop.Services.Messages;

/// <summary>
/// Published when a newsletter subscription becomes active.
/// </summary>
public class EmailSubscribedEvent(NewsLetterSubscription subscription)
{
    public NewsLetterSubscription Subscription { get; } = subscription;
}

/// <summary>
/// Published when a newsletter subscription becomes inactive.
/// </summary>
public class EmailUnsubscribedEvent(NewsLetterSubscription subscription)
{
    public NewsLetterSubscription Subscription { get; } = subscription;
}
