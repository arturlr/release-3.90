using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Tax;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class CheckoutAttributeController(
    ICheckoutAttributeService checkoutAttributeService,
    ITaxCategoryService taxCategoryService,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService) : BaseAdminController
{
    #region Helpers

    private async Task<CheckoutAttributeModel> PrepareModelAsync(CheckoutAttribute? entity = null)
    {
        var model = new CheckoutAttributeModel();

        if (entity is not null)
        {
            model.Id = entity.Id;
            model.Name = entity.Name;
            model.TextPrompt = entity.TextPrompt;
            model.IsRequired = entity.IsRequired;
            model.ShippableProductRequired = entity.ShippableProductRequired;
            model.IsTaxExempt = entity.IsTaxExempt;
            model.TaxCategoryId = entity.TaxCategoryId;
            model.AttributeControlTypeId = entity.AttributeControlTypeId;
            model.DisplayOrder = entity.DisplayOrder;
            model.ValidationMinLength = entity.ValidationMinLength;
            model.ValidationMaxLength = entity.ValidationMaxLength;
            model.ValidationFileAllowedExtensions = entity.ValidationFileAllowedExtensions;
            model.ValidationFileMaximumSize = entity.ValidationFileMaximumSize;
            model.DefaultValue = entity.DefaultValue;
        }

        // tax categories
        var taxCategories = await taxCategoryService.GetAllTaxCategoriesAsync();
        model.AvailableTaxCategories.Add(new TaxCategoryItem { Id = 0, Name = "None" });
        foreach (var tc in taxCategories)
            model.AvailableTaxCategories.Add(new TaxCategoryItem { Id = tc.Id, Name = tc.Name });

        // control types
        foreach (var ct in Enum.GetValues<AttributeControlType>())
            model.AvailableControlTypes.Add(new ControlTypeItem { Id = (int)ct, Name = ct.ToString() });

        return model;
    }

    #endregion

    #region Checkout attributes

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        return View();
    }

    [HttpPost]
    public async Task<JsonResult> CheckoutAttributeList()
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var attributes = await checkoutAttributeService.GetAllCheckoutAttributesAsync();

        var gridModel = new DataSourceResult
        {
            Data = attributes.Select(a => new CheckoutAttributeModel
            {
                Id = a.Id,
                Name = a.Name,
                AttributeControlTypeName = ((AttributeControlType)a.AttributeControlTypeId).ToString(),
                IsRequired = a.IsRequired,
                DisplayOrder = a.DisplayOrder
            }),
            Total = attributes.Count
        };

        return Json(gridModel);
    }

    public async Task<IActionResult> Create()
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        var model = await PrepareModelAsync();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CheckoutAttributeModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var checkoutAttribute = new CheckoutAttribute
            {
                Name = model.Name ?? string.Empty,
                TextPrompt = model.TextPrompt,
                IsRequired = model.IsRequired,
                ShippableProductRequired = model.ShippableProductRequired,
                IsTaxExempt = model.IsTaxExempt,
                TaxCategoryId = model.TaxCategoryId,
                AttributeControlTypeId = model.AttributeControlTypeId,
                DisplayOrder = model.DisplayOrder,
                ValidationMinLength = model.ValidationMinLength,
                ValidationMaxLength = model.ValidationMaxLength,
                ValidationFileAllowedExtensions = model.ValidationFileAllowedExtensions,
                ValidationFileMaximumSize = model.ValidationFileMaximumSize,
                DefaultValue = model.DefaultValue
            };
            await checkoutAttributeService.InsertCheckoutAttributeAsync(checkoutAttribute);

            customerActivityService.InsertActivity("AddNewCheckoutAttribute",
                $"Added a new checkout attribute (ID = {checkoutAttribute.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = checkoutAttribute.Id });

            return RedirectToAction("List");
        }

        // redisplay form with dropdowns
        var preparedModel = await PrepareModelAsync();
        model.AvailableTaxCategories = preparedModel.AvailableTaxCategories;
        model.AvailableControlTypes = preparedModel.AvailableControlTypes;
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        var checkoutAttribute = await checkoutAttributeService.GetCheckoutAttributeByIdAsync(id);
        if (checkoutAttribute is null)
            return RedirectToAction("List");

        var model = await PrepareModelAsync(checkoutAttribute);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CheckoutAttributeModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        var checkoutAttribute = await checkoutAttributeService.GetCheckoutAttributeByIdAsync(model.Id);
        if (checkoutAttribute is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            checkoutAttribute.Name = model.Name ?? string.Empty;
            checkoutAttribute.TextPrompt = model.TextPrompt;
            checkoutAttribute.IsRequired = model.IsRequired;
            checkoutAttribute.ShippableProductRequired = model.ShippableProductRequired;
            checkoutAttribute.IsTaxExempt = model.IsTaxExempt;
            checkoutAttribute.TaxCategoryId = model.TaxCategoryId;
            checkoutAttribute.AttributeControlTypeId = model.AttributeControlTypeId;
            checkoutAttribute.DisplayOrder = model.DisplayOrder;
            checkoutAttribute.ValidationMinLength = model.ValidationMinLength;
            checkoutAttribute.ValidationMaxLength = model.ValidationMaxLength;
            checkoutAttribute.ValidationFileAllowedExtensions = model.ValidationFileAllowedExtensions;
            checkoutAttribute.ValidationFileMaximumSize = model.ValidationFileMaximumSize;
            checkoutAttribute.DefaultValue = model.DefaultValue;
            await checkoutAttributeService.UpdateCheckoutAttributeAsync(checkoutAttribute);

            customerActivityService.InsertActivity("EditCheckoutAttribute",
                $"Edited a checkout attribute (ID = {checkoutAttribute.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = checkoutAttribute.Id });

            return RedirectToAction("List");
        }

        var preparedModel = await PrepareModelAsync(checkoutAttribute);
        model.AvailableTaxCategories = preparedModel.AvailableTaxCategories;
        model.AvailableControlTypes = preparedModel.AvailableControlTypes;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        var checkoutAttribute = await checkoutAttributeService.GetCheckoutAttributeByIdAsync(id);
        if (checkoutAttribute is null)
            return RedirectToAction("List");

        await checkoutAttributeService.DeleteCheckoutAttributeAsync(checkoutAttribute);

        customerActivityService.InsertActivity("DeleteCheckoutAttribute",
            $"Deleted a checkout attribute (ID = {id})");

        return RedirectToAction("List");
    }

    #endregion

    #region Checkout attribute values

    [HttpPost]
    public async Task<JsonResult> ValueList(int checkoutAttributeId)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var values = await checkoutAttributeService.GetCheckoutAttributeValuesAsync(checkoutAttributeId);

        var gridModel = new DataSourceResult
        {
            Data = values.Select(v => new CheckoutAttributeValueModel
            {
                Id = v.Id,
                CheckoutAttributeId = v.CheckoutAttributeId,
                Name = v.Name,
                ColorSquaresRgb = v.ColorSquaresRgb,
                PriceAdjustment = v.PriceAdjustment,
                WeightAdjustment = v.WeightAdjustment,
                IsPreSelected = v.IsPreSelected,
                DisplayOrder = v.DisplayOrder
            }),
            Total = values.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    public async Task<JsonResult> ValueAdd(int checkoutAttributeId, CheckoutAttributeValueModel model)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        if (!ModelState.IsValid)
            return Json(new DataSourceResult { Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        await checkoutAttributeService.InsertCheckoutAttributeValueAsync(new CheckoutAttributeValue
        {
            CheckoutAttributeId = checkoutAttributeId,
            Name = model.Name ?? string.Empty,
            ColorSquaresRgb = model.ColorSquaresRgb,
            PriceAdjustment = model.PriceAdjustment,
            WeightAdjustment = model.WeightAdjustment,
            IsPreSelected = model.IsPreSelected,
            DisplayOrder = model.DisplayOrder
        });

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> ValueUpdate(CheckoutAttributeValueModel model)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        if (!ModelState.IsValid)
            return Json(new DataSourceResult { Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        var cav = await checkoutAttributeService.GetCheckoutAttributeValueByIdAsync(model.Id);
        if (cav is null)
            return Json(new DataSourceResult { Errors = "Value not found" });

        cav.Name = model.Name ?? string.Empty;
        cav.ColorSquaresRgb = model.ColorSquaresRgb;
        cav.PriceAdjustment = model.PriceAdjustment;
        cav.WeightAdjustment = model.WeightAdjustment;
        cav.IsPreSelected = model.IsPreSelected;
        cav.DisplayOrder = model.DisplayOrder;
        await checkoutAttributeService.UpdateCheckoutAttributeValueAsync(cav);

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> ValueDelete(int id)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var cav = await checkoutAttributeService.GetCheckoutAttributeValueByIdAsync(id);
        if (cav is null)
            return Json(new DataSourceResult { Errors = "Value not found" });

        await checkoutAttributeService.DeleteCheckoutAttributeValueAsync(cav);

        return Json(new { });
    }

    #endregion
}
