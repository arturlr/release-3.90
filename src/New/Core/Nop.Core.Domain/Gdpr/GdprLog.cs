namespace Nop.Core.Domain.Gdpr;

public class GdprLog : BaseEntity
{
    public int CustomerId { get; set; }

    public int RequestTypeId { get; set; }

    public string? RequestDetails { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public GdprRequestType RequestType
    {
        get => (GdprRequestType)RequestTypeId;
        set => RequestTypeId = (int)value;
    }
}
