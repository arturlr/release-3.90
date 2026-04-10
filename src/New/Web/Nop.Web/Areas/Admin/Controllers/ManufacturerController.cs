using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Seo;
using Nop.Services.Catalog;
using Nop.Services.ExportImport;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class ManufacturerController(
    IManufacturerService manufacturerService,
    IManufacturerTemplateService manufacturerTemplateService,
    IUrlRecordService urlRecordService,
    IStoreService storeService,
    IExportManager exportManager,
    IImportManager importManager,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService,
    SeoSettings seoSettings,
    CatalogSettings catalogSettings) : BaseAdminController
{
    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List()
    {
        if (!permissionService.Authorize("ManageManufacturers"))
            return Forbid();

        var model = new ManufacturerListModel();
        model.AvailableStores.Add(new SelectListItem { Text = "All", Value = "0" });
        foreach (var s in await storeService.GetAllStoresAsync())
            model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });

        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> ManufacturerList(DataSourceRequest command, ManufacturerListModel model)
    {
        if (!permissionService.Authorize("ManageManufacturers"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var manufacturers = await manufacturerService.GetAllManufacturersAsync(
            model.SearchManufacturerName ?? string.Empty,
            model.SearchStoreId,
            command.Page - 1,
            command.PageSize,
            showHidden: true);

        var gridModel = new DataSourceResult
        {
            Data = manufacturers.Select(m => new ManufacturerGridModel
            {
                Id = m.Id,
                Name = m.Name,
                Published = m.Published,
                DisplayOrder = m.DisplayOrder
            }),
            Total = manufacturers.TotalCount
        };

        return Json(gridModel);
    }

    public async Task<IActionResult> Create()
    {
        if (!permissionService.Authorize("ManageManufacturers"))
            return Forbid();

        var model = new ManufacturerModel
        {
            PageSize = catalogSettings.DefaultManufacturerPageSize > 0 ? catalogSettings.DefaultManufacturerPageSize : 6,
            PageSizeOptions = catalogSettings.DefaultManufacturerPageSizeOptions ?? "6, 3, 9",
            Published = true,
            AllowCustomersToSelectPageSize = true
        };

        await PrepareManufacturerModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ManufacturerModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageManufacturers"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var manufacturer = MapModelToEntity(model, new Manufacturer());
            manufacturer.CreatedOnUtc = DateTime.UtcNow;
            manufacturer.UpdatedOnUtc = DateTime.UtcNow;

            await manufacturerService.InsertManufacturerAsync(manufacturer);

            var seName = await manufacturer.ValidateSeNameAsync(
                model.SeName, manufacturer.Name ?? string.Empty, true,
                urlRecordService, seoSettings);
            await urlRecordService.SaveSlugAsync(manufacturer, seName, 0);

            customerActivityService.InsertActivity("AddNewManufacturer", $"Added a new manufacturer ('{manufacturer.Name}')");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = manufacturer.Id });

            return RedirectToAction("List");
        }

        await PrepareManufacturerModelDropdownsAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageManufacturers"))
            return Forbid();

        var manufacturer = await manufacturerService.GetManufacturerByIdAsync(id);
        if (manufacturer is null || manufacturer.Deleted)
            return RedirectToAction("List");

        var model = MapEntityToModel(manufacturer);
        model.SeName = await urlRecordService.GetActiveSlugAsync(manufacturer.Id, "Manufacturer", 0);

        await PrepareManufacturerModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ManufacturerModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageManufacturers"))
            return Forbid();

        var manufacturer = await manufacturerService.GetManufacturerByIdAsync(model.Id);
        if (manufacturer is null || manufacturer.Deleted)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            MapModelToEntity(model, manufacturer);
            manufacturer.UpdatedOnUtc = DateTime.UtcNow;

            await manufacturerService.UpdateManufacturerAsync(manufacturer);

            var seName = await manufacturer.ValidateSeNameAsync(
                model.SeName, manufacturer.Name ?? string.Empty, true,
                urlRecordService, seoSettings);
            await urlRecordService.SaveSlugAsync(manufacturer, seName, 0);

            customerActivityService.InsertActivity("EditManufacturer", $"Edited a manufacturer ('{manufacturer.Name}')");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = manufacturer.Id });

            return RedirectToAction("List");
        }

        await PrepareManufacturerModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageManufacturers"))
            return Forbid();

        var manufacturer = await manufacturerService.GetManufacturerByIdAsync(id);
        if (manufacturer is null)
            return RedirectToAction("List");

        await manufacturerService.DeleteManufacturerAsync(manufacturer);

        customerActivityService.InsertActivity("DeleteManufacturer", $"Deleted a manufacturer ('{manufacturer.Name}')");

        return RedirectToAction("List");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSelected(IEnumerable<int>? selectedIds)
    {
        if (!permissionService.Authorize("ManageManufacturers"))
            return Forbid();

        if (selectedIds is not null)
        {
            foreach (var id in selectedIds)
            {
                var manufacturer = await manufacturerService.GetManufacturerByIdAsync(id);
                if (manufacturer is not null)
                    await manufacturerService.DeleteManufacturerAsync(manufacturer);
            }
        }

        return Json(new { Result = true });
    }

    [HttpPost]
    public async Task<IActionResult> ExportExcel()
    {
        if (!permissionService.Authorize("ManageManufacturers"))
            return Forbid();

        var manufacturers = (await manufacturerService.GetAllManufacturersAsync(showHidden: true))
            .Where(m => !m.Deleted)
            .ToList();

        var bytes = exportManager.ExportManufacturersToXlsx(manufacturers);
        return File(bytes, MimeTypes.TextXlsx, "manufacturers.xlsx");
    }

    [HttpPost]
    public async Task<IActionResult> ImportExcel(IFormFile? importexcelfile)
    {
        if (!permissionService.Authorize("ManageManufacturers"))
            return Forbid();

        if (importexcelfile is not null && importexcelfile.Length > 0)
        {
            await using var stream = importexcelfile.OpenReadStream();
            await importManager.ImportManufacturersFromXlsxAsync(stream);
        }

        return RedirectToAction("List");
    }
}
