namespace Nop.Services.Directory;

/// <summary>
/// GEO lookup service — IP-to-country resolution.
/// Full implementation deferred to [7.15] (MaxMind GeoIP2 integration).
/// </summary>
public interface IGeoLookupService
{
    string? LookupCountryIsoCode(string ipAddress);
    string? LookupCountryName(string ipAddress);
}
