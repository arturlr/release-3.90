namespace Nop.Services.Payments;

public class CancelRecurringPaymentResult
{
    public List<string> Errors { get; set; } = [];
    public bool Success => Errors.Count == 0;

    public void AddError(string error) => Errors.Add(error);
}
