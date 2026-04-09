namespace Nop.Services.Tax;

public class CalculateTaxResult
{
    public decimal TaxRate { get; set; }
    public IList<string> Errors { get; set; } = [];
    public bool Success => Errors.Count == 0;

    public void AddError(string error) => Errors.Add(error);
}
