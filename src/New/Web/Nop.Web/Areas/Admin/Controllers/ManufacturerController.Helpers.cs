using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Catalog;
using Nop.Web.Areas.Admin.Models.Catalog;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class ManufacturerController
{
    private async Task PrepareManufacturerModelDropdownsAsync(ManufacturerModel model)
    {
        foreach (var t in await manufacturerTemplateService.GetAllManufacturerTemplatesAsync())
            model.AvailableManufacturerTemplates.Add(new SelectListItem { Text = t.Name, Value = t.Id.ToString() });
    }

    private static Manufacturer MapModelToEntity(ManufacturerModel model, Manufacturer manufacturer)
    {
        manufacturer.Name = model.Name;
        manufacturer.Description = model.Description;
        manufacturer.ManufacturerTemplateId = model.ManufacturerTemplateId;
        manufacturer.MetaKeywords = model.MetaKeywords;
        manufacturer.MetaDescription = model.MetaDescription;
        manufacturer.MetaTitle = model.MetaTitle;
        manufacturer.PictureId = model.PictureId;
        manufacturer.PageSize = model.PageSize;
        manufacturer.AllowCustomersToSelectPageSize = model.AllowCustomersToSelectPageSize;
        manufacturer.PageSizeOptions = model.PageSizeOptions;
        manufacturer.PriceRanges = model.PriceRanges;
        manufacturer.Published = model.Published;
        manufacturer.DisplayOrder = model.DisplayOrder;
        return manufacturer;
    }

    private static ManufacturerModel MapEntityToModel(Manufacturer manufacturer)
    {
        return new ManufacturerModel
        {
            Id = manufacturer.Id,
            Name = manufacturer.Name,
            Description = manufacturer.Description,
            ManufacturerTemplateId = manufacturer.ManufacturerTemplateId,
            MetaKeywords = manufacturer.MetaKeywords,
            MetaDescription = manufacturer.MetaDescription,
            MetaTitle = manufacturer.MetaTitle,
            PictureId = manufacturer.PictureId,
            PageSize = manufacturer.PageSize,
            AllowCustomersToSelectPageSize = manufacturer.AllowCustomersToSelectPageSize,
            PageSizeOptions = manufacturer.PageSizeOptions,
            PriceRanges = manufacturer.PriceRanges,
            Published = manufacturer.Published,
            DisplayOrder = manufacturer.DisplayOrder
        };
    }
}
