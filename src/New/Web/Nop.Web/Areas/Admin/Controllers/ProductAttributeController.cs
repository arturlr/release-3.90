using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class ProductAttributeController(
    IProductAttributeService productAttributeService,
    IProductService productService,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService) : BaseAdminController
{
    #region Product attributes

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        return View();
    }

    [HttpPost]
    public async Task<JsonResult> ProductAttributeList(DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var attributes = await productAttributeService.GetAllProductAttributesAsync(
            command.Page - 1, command.PageSize);

        var gridModel = new DataSourceResult
        {
            Data = attributes.Select(a => new ProductAttributeModel
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description
            }),
            Total = attributes.TotalCount
        };

        return Json(gridModel);
    }

    public IActionResult Create()
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        return View(new ProductAttributeModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductAttributeModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var productAttribute = new ProductAttribute
            {
                Name = model.Name ?? string.Empty,
                Description = model.Description
            };
            await productAttributeService.InsertProductAttributeAsync(productAttribute);

            customerActivityService.InsertActivity("AddNewProductAttribute",
                $"Added a new product attribute (ID = {productAttribute.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = productAttribute.Id });

            return RedirectToAction("List");
        }

        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        var productAttribute = await productAttributeService.GetProductAttributeByIdAsync(id);
        if (productAttribute is null)
            return RedirectToAction("List");

        var model = new ProductAttributeModel
        {
            Id = productAttribute.Id,
            Name = productAttribute.Name,
            Description = productAttribute.Description
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProductAttributeModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        var productAttribute = await productAttributeService.GetProductAttributeByIdAsync(model.Id);
        if (productAttribute is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            productAttribute.Name = model.Name ?? string.Empty;
            productAttribute.Description = model.Description;
            await productAttributeService.UpdateProductAttributeAsync(productAttribute);

            customerActivityService.InsertActivity("EditProductAttribute",
                $"Edited a product attribute (ID = {productAttribute.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = productAttribute.Id });

            return RedirectToAction("List");
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Forbid();

        var productAttribute = await productAttributeService.GetProductAttributeByIdAsync(id);
        if (productAttribute is null)
            return RedirectToAction("List");

        await productAttributeService.DeleteProductAttributeAsync(productAttribute);

        customerActivityService.InsertActivity("DeleteProductAttribute",
            $"Deleted a product attribute (ID = {id})");

        return RedirectToAction("List");
    }

    #endregion

    #region Used by products

    [HttpPost]
    public async Task<JsonResult> UsedByProducts(int productAttributeId, DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var products = await productService.GetProductsByProductAttributeIdAsync(
            productAttributeId, command.Page - 1, command.PageSize);

        var gridModel = new DataSourceResult
        {
            Data = products.Select(p => new UsedByProductModel
            {
                Id = p.Id,
                ProductName = p.Name,
                Published = p.Published
            }),
            Total = products.TotalCount
        };

        return Json(gridModel);
    }

    #endregion

    #region Predefined values

    [HttpPost]
    public async Task<JsonResult> PredefinedValueList(int productAttributeId)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var values = await productAttributeService.GetPredefinedProductAttributeValuesAsync(productAttributeId);

        var gridModel = new DataSourceResult
        {
            Data = values.Select(v => new PredefinedProductAttributeValueModel
            {
                Id = v.Id,
                ProductAttributeId = v.ProductAttributeId,
                Name = v.Name,
                PriceAdjustment = v.PriceAdjustment,
                WeightAdjustment = v.WeightAdjustment,
                Cost = v.Cost,
                IsPreSelected = v.IsPreSelected,
                DisplayOrder = v.DisplayOrder
            }),
            Total = values.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    public async Task<JsonResult> PredefinedValueAdd(int productAttributeId, PredefinedProductAttributeValueModel model)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        if (!ModelState.IsValid)
            return Json(new DataSourceResult { Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        await productAttributeService.InsertPredefinedProductAttributeValueAsync(new PredefinedProductAttributeValue
        {
            ProductAttributeId = productAttributeId,
            Name = model.Name ?? string.Empty,
            PriceAdjustment = model.PriceAdjustment,
            WeightAdjustment = model.WeightAdjustment,
            Cost = model.Cost,
            IsPreSelected = model.IsPreSelected,
            DisplayOrder = model.DisplayOrder
        });

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> PredefinedValueUpdate(PredefinedProductAttributeValueModel model)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        if (!ModelState.IsValid)
            return Json(new DataSourceResult { Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        var ppav = await productAttributeService.GetPredefinedProductAttributeValueByIdAsync(model.Id);
        if (ppav is null)
            return Json(new DataSourceResult { Errors = "Predefined value not found" });

        ppav.Name = model.Name ?? string.Empty;
        ppav.PriceAdjustment = model.PriceAdjustment;
        ppav.WeightAdjustment = model.WeightAdjustment;
        ppav.Cost = model.Cost;
        ppav.IsPreSelected = model.IsPreSelected;
        ppav.DisplayOrder = model.DisplayOrder;
        await productAttributeService.UpdatePredefinedProductAttributeValueAsync(ppav);

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> PredefinedValueDelete(int id)
    {
        if (!permissionService.Authorize("ManageAttributes"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var ppav = await productAttributeService.GetPredefinedProductAttributeValueByIdAsync(id);
        if (ppav is null)
            return Json(new DataSourceResult { Errors = "Predefined value not found" });

        await productAttributeService.DeletePredefinedProductAttributeValueAsync(ppav);

        return Json(new { });
    }

    #endregion
}
