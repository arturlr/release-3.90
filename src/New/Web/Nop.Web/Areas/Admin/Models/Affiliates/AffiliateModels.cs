using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Affiliates;

public class AffiliateListModel : BaseNopModel
{
    public string? SearchFriendlyUrlName { get; set; }
    public string? SearchFirstName { get; set; }
    public string? SearchLastName { get; set; }
    public bool LoadOnlyWithOrders { get; set; }
    public DateTime? OrdersCreatedFromUtc { get; set; }
    public DateTime? OrdersCreatedToUtc { get; set; }
}

public class AffiliateModel : BaseNopEntityModel
{
    public string? Url { get; set; }
    public string? AdminComment { get; set; }
    public string? FriendlyUrlName { get; set; }
    public bool Active { get; set; }

    // Address fields (inline — no nested AddressModel)
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public int? CountryId { get; set; }
    public int? StateProvinceId { get; set; }
    public string? City { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? ZipPostalCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FaxNumber { get; set; }

    public List<SelectListItem> AvailableCountries { get; set; } = [];
    public List<SelectListItem> AvailableStates { get; set; } = [];
}

public class AffiliateGridModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool Active { get; set; }
}

public class AffiliatedOrderModel
{
    public int Id { get; set; }
    public string? CustomOrderNumber { get; set; }
    public string? OrderStatus { get; set; }
    public string? PaymentStatus { get; set; }
    public string? ShippingStatus { get; set; }
    public string? OrderTotal { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class AffiliatedCustomerModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
}
