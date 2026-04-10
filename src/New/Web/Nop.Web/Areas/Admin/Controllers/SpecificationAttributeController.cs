using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class SpecificationAttributeController(
    ISpecificationAttributeService specificationAttributeService,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService) : BaseAdminController
{
    #region Specification attributes

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        return View();
    }

    [HttpPost]
    public async Task<JsonResult> SpecificationAttributeList(DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var attributes = await specificationAttributeService.GetSpecificationAttributesAsync(
            command.Page - 1, command.PageSize);

        var gridModel = new DataSourceResult
        {
            Data = attributes.Select(a => new SpecificationAttributeModel
            {
                Id = a.Id,
                Name = a.Name,
                DisplayOrder = a.DisplayOrder
            }),
            Total = attributes.TotalCount
        };

        return Json(gridModel);
    }

    public IActionResult Create()
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        return View(new SpecificationAttributeModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(SpecificationAttributeModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var specificationAttribute = new SpecificationAttribute
            {
                Name = model.Name ?? string.Empty,
                DisplayOrder = model.DisplayOrder
            };
            await specificationAttributeService.InsertSpecificationAttributeAsync(specificationAttribute);

            customerActivityService.InsertActivity("AddNewSpecAttribute",
                $"Added a new specification attribute (ID = {specificationAttribute.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = specificationAttribute.Id });

            return RedirectToAction("List");
        }

        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        var specificationAttribute = await specificationAttributeService.GetSpecificationAttributeByIdAsync(id);
        if (specificationAttribute is null)
            return RedirectToAction("List");

        var model = new SpecificationAttributeModel
        {
            Id = specificationAttribute.Id,
            Name = specificationAttribute.Name,
            DisplayOrder = specificationAttribute.DisplayOrder
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(SpecificationAttributeModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        var specificationAttribute = await specificationAttributeService.GetSpecificationAttributeByIdAsync(model.Id);
        if (specificationAttribute is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            specificationAttribute.Name = model.Name ?? string.Empty;
            specificationAttribute.DisplayOrder = model.DisplayOrder;
            await specificationAttributeService.UpdateSpecificationAttributeAsync(specificationAttribute);

            customerActivityService.InsertActivity("EditSpecAttribute",
                $"Edited a specification attribute (ID = {specificationAttribute.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = specificationAttribute.Id });

            return RedirectToAction("List");
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        var specificationAttribute = await specificationAttributeService.GetSpecificationAttributeByIdAsync(id);
        if (specificationAttribute is null)
            return RedirectToAction("List");

        await specificationAttributeService.DeleteSpecificationAttributeAsync(specificationAttribute);

        customerActivityService.InsertActivity("DeleteSpecAttribute",
            $"Deleted a specification attribute (ID = {id})");

        return RedirectToAction("List");
    }

    #endregion

    #region Specification attribute options

    [HttpPost]
    public async Task<JsonResult> OptionList(int specificationAttributeId)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var options = await specificationAttributeService
            .GetSpecificationAttributeOptionsBySpecificationAttributeAsync(specificationAttributeId);

        var gridModel = new DataSourceResult
        {
            Data = options.Select(o => new SpecificationAttributeOptionModel
            {
                Id = o.Id,
                SpecificationAttributeId = o.SpecificationAttributeId,
                Name = o.Name,
                ColorSquaresRgb = o.ColorSquaresRgb,
                EnableColorSquaresRgb = !string.IsNullOrEmpty(o.ColorSquaresRgb),
                NumberOfAssociatedProducts = specificationAttributeService
                    .GetProductSpecificationAttributeCountAsync(0, o.Id).GetAwaiter().GetResult(),
                DisplayOrder = o.DisplayOrder
            }),
            Total = options.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    public async Task<JsonResult> OptionAdd(int specificationAttributeId, SpecificationAttributeOptionModel model)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        if (!ModelState.IsValid)
            return Json(new DataSourceResult { Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        await specificationAttributeService.InsertSpecificationAttributeOptionAsync(new SpecificationAttributeOption
        {
            SpecificationAttributeId = specificationAttributeId,
            Name = model.Name ?? string.Empty,
            ColorSquaresRgb = model.EnableColorSquaresRgb ? model.ColorSquaresRgb : null,
            DisplayOrder = model.DisplayOrder
        });

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> OptionUpdate(SpecificationAttributeOptionModel model)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        if (!ModelState.IsValid)
            return Json(new DataSourceResult { Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        var sao = await specificationAttributeService.GetSpecificationAttributeOptionByIdAsync(model.Id);
        if (sao is null)
            return Json(new DataSourceResult { Errors = "Option not found" });

        sao.Name = model.Name ?? string.Empty;
        sao.ColorSquaresRgb = model.EnableColorSquaresRgb ? model.ColorSquaresRgb : null;
        sao.DisplayOrder = model.DisplayOrder;
        await specificationAttributeService.UpdateSpecificationAttributeOptionAsync(sao);

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> OptionDelete(int id)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var sao = await specificationAttributeService.GetSpecificationAttributeOptionByIdAsync(id);
        if (sao is null)
            return Json(new DataSourceResult { Errors = "Option not found" });

        await specificationAttributeService.DeleteSpecificationAttributeOptionAsync(sao);

        return Json(new { });
    }

    #endregion

    #region AJAX helper

    [HttpGet]
    public async Task<JsonResult> GetOptionsByAttributeId(int attributeId)
    {
        // No permission check — used by product editing page for dropdown population
        var options = await specificationAttributeService
            .GetSpecificationAttributeOptionsBySpecificationAttributeAsync(attributeId);

        var result = options.Select(o => new { id = o.Id, name = o.Name });
        return Json(result);
    }

    #endregion
}
