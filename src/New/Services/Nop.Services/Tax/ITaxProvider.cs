namespace Nop.Services.Tax;

/// <summary>
/// Tax provider interface — plugin-based tax rate calculation.
/// Does NOT extend IPlugin (deferred to [2.10] plugin system).
/// </summary>
public interface ITaxProvider
{
    CalculateTaxResult GetTaxRate(CalculateTaxRequest calculateTaxRequest);
}
