namespace Nop.Services.Customers;

public class CustomerRegistrationResult
{
    public IList<string> Errors { get; set; } = [];

    public bool Success => Errors.Count == 0;

    public void AddError(string error) => Errors.Add(error);
}
