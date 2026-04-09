using Nop.Core;
using Nop.Core.Domain.Affiliates;
using Nop.Core.Domain.Common;
using Nop.Services.Seo;

namespace Nop.Services.Affiliates;

public static class AffiliateExtensions
{
    /// <summary>
    /// Get full name from the affiliate's address.
    /// No nav properties — address must be passed explicitly.
    /// </summary>
    public static string GetFullName(this Affiliate affiliate, Address address)
    {
        ArgumentNullException.ThrowIfNull(affiliate);
        ArgumentNullException.ThrowIfNull(address);

        var first = address.FirstName?.Trim();
        var last = address.LastName?.Trim();

        if (!string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(last))
            return $"{first} {last}";

        return first ?? last ?? string.Empty;
    }

    /// <summary>
    /// Generate affiliate URL using friendly name or ID.
    /// </summary>
    public static string GenerateUrl(this Affiliate affiliate, IWebHelper webHelper)
    {
        ArgumentNullException.ThrowIfNull(affiliate);
        ArgumentNullException.ThrowIfNull(webHelper);

        var storeUrl = webHelper.GetStoreLocation(false);

        return !string.IsNullOrEmpty(affiliate.FriendlyUrlName)
            ? webHelper.ModifyQueryString(storeUrl, "affiliate=" + affiliate.FriendlyUrlName, null)
            : webHelper.ModifyQueryString(storeUrl, "affiliateid=" + affiliate.Id, null);
    }

    /// <summary>
    /// Validate and ensure uniqueness of friendly URL name.
    /// No service locator — IAffiliateService passed explicitly.
    /// </summary>
    public static async Task<string> ValidateFriendlyUrlNameAsync(
        this Affiliate affiliate,
        IAffiliateService affiliateService,
        string? friendlyUrlName)
    {
        ArgumentNullException.ThrowIfNull(affiliate);
        ArgumentNullException.ThrowIfNull(affiliateService);

        // generate a clean slug
        friendlyUrlName = SeoExtensions.GetSeName(friendlyUrlName, convertNonWesternChars: false, allowUnicodeCharsInUrls: false);
        friendlyUrlName = CommonHelper.EnsureMaximumLength(friendlyUrlName, 200);

        if (string.IsNullOrEmpty(friendlyUrlName))
            return friendlyUrlName;

        // ensure uniqueness
        var tempName = friendlyUrlName;
        var i = 2;
        while (true)
        {
            var existing = await affiliateService.GetAffiliateByFriendlyUrlNameAsync(tempName);
            if (existing is null || existing.Id == affiliate.Id)
                break;

            tempName = $"{friendlyUrlName}-{i}";
            i++;
        }

        return tempName;
    }
}
