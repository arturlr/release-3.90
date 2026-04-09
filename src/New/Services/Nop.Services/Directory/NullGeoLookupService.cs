namespace Nop.Services.Directory;

/// <summary>
/// Null implementation of IGeoLookupService.
/// Returns null for all lookups until MaxMind GeoIP2 is integrated in [7.15].
/// </summary>
public class NullGeoLookupService : IGeoLookupService
{
    public string? LookupCountryIsoCode(string ipAddress) => null;
    public string? LookupCountryName(string ipAddress) => null;
}
