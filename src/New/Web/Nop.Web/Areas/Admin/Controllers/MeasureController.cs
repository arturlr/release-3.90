using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Directory;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Directory;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class MeasureController(
    IMeasureService measureService,
    MeasureSettings measureSettings,
    ISettingService settingService,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService) : BaseAdminController
{
    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        return View();
    }

    #region Dimensions

    [HttpPost]
    public async Task<JsonResult> DimensionList()
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var dimensions = await measureService.GetAllMeasureDimensionsAsync();
        var gridModel = new DataSourceResult
        {
            Data = dimensions.Select(d => new MeasureDimensionModel
            {
                Id = d.Id,
                Name = d.Name,
                SystemKeyword = d.SystemKeyword,
                Ratio = d.Ratio,
                DisplayOrder = d.DisplayOrder,
                IsPrimaryDimension = d.Id == measureSettings.BaseDimensionId
            }),
            Total = dimensions.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    public async Task<JsonResult> CreateDimension(MeasureDimensionModel model)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        if (!ModelState.IsValid)
            return Json(new DataSourceResult { Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        var dimension = new MeasureDimension
        {
            Name = model.Name,
            SystemKeyword = model.SystemKeyword,
            Ratio = model.Ratio,
            DisplayOrder = model.DisplayOrder
        };
        await measureService.InsertMeasureDimensionAsync(dimension);

        customerActivityService.InsertActivity("AddNewMeasureDimension", $"Added a new measure dimension (ID = {dimension.Id})");

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> EditDimension(MeasureDimensionModel model)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        if (!ModelState.IsValid)
            return Json(new DataSourceResult { Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        var dimension = await measureService.GetMeasureDimensionByIdAsync(model.Id);
        if (dimension is null)
            return Json(new DataSourceResult { Errors = "Dimension not found" });

        dimension.Name = model.Name;
        dimension.SystemKeyword = model.SystemKeyword;
        dimension.Ratio = model.Ratio;
        dimension.DisplayOrder = model.DisplayOrder;
        await measureService.UpdateMeasureDimensionAsync(dimension);

        customerActivityService.InsertActivity("EditMeasureDimension", $"Edited a measure dimension (ID = {dimension.Id})");

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> DeleteDimension(int id)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var dimension = await measureService.GetMeasureDimensionByIdAsync(id);
        if (dimension is null)
            return Json(new DataSourceResult { Errors = "Dimension not found" });

        if (dimension.Id == measureSettings.BaseDimensionId)
            return Json(new DataSourceResult { Errors = "Cannot delete the primary dimension" });

        await measureService.DeleteMeasureDimensionAsync(dimension);

        customerActivityService.InsertActivity("DeleteMeasureDimension", $"Deleted a measure dimension (ID = {id})");

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> MarkAsPrimaryDimension(int id)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Json(new { result = false });

        var dimension = await measureService.GetMeasureDimensionByIdAsync(id);
        if (dimension is null)
            return Json(new { result = false });

        measureSettings.BaseDimensionId = id;
        await settingService.SaveSettingAsync(measureSettings);

        return Json(new { result = true });
    }

    #endregion

    #region Weights

    [HttpPost]
    public async Task<JsonResult> WeightList()
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var weights = await measureService.GetAllMeasureWeightsAsync();
        var gridModel = new DataSourceResult
        {
            Data = weights.Select(w => new MeasureWeightModel
            {
                Id = w.Id,
                Name = w.Name,
                SystemKeyword = w.SystemKeyword,
                Ratio = w.Ratio,
                DisplayOrder = w.DisplayOrder,
                IsPrimaryWeight = w.Id == measureSettings.BaseWeightId
            }),
            Total = weights.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    public async Task<JsonResult> CreateWeight(MeasureWeightModel model)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        if (!ModelState.IsValid)
            return Json(new DataSourceResult { Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        var weight = new MeasureWeight
        {
            Name = model.Name,
            SystemKeyword = model.SystemKeyword,
            Ratio = model.Ratio,
            DisplayOrder = model.DisplayOrder
        };
        await measureService.InsertMeasureWeightAsync(weight);

        customerActivityService.InsertActivity("AddNewMeasureWeight", $"Added a new measure weight (ID = {weight.Id})");

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> EditWeight(MeasureWeightModel model)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        if (!ModelState.IsValid)
            return Json(new DataSourceResult { Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        var weight = await measureService.GetMeasureWeightByIdAsync(model.Id);
        if (weight is null)
            return Json(new DataSourceResult { Errors = "Weight not found" });

        weight.Name = model.Name;
        weight.SystemKeyword = model.SystemKeyword;
        weight.Ratio = model.Ratio;
        weight.DisplayOrder = model.DisplayOrder;
        await measureService.UpdateMeasureWeightAsync(weight);

        customerActivityService.InsertActivity("EditMeasureWeight", $"Edited a measure weight (ID = {weight.Id})");

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> DeleteWeight(int id)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var weight = await measureService.GetMeasureWeightByIdAsync(id);
        if (weight is null)
            return Json(new DataSourceResult { Errors = "Weight not found" });

        if (weight.Id == measureSettings.BaseWeightId)
            return Json(new DataSourceResult { Errors = "Cannot delete the primary weight" });

        await measureService.DeleteMeasureWeightAsync(weight);

        customerActivityService.InsertActivity("DeleteMeasureWeight", $"Deleted a measure weight (ID = {id})");

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> MarkAsPrimaryWeight(int id)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Json(new { result = false });

        var weight = await measureService.GetMeasureWeightByIdAsync(id);
        if (weight is null)
            return Json(new { result = false });

        measureSettings.BaseWeightId = id;
        await settingService.SaveSettingAsync(measureSettings);

        return Json(new { result = true });
    }

    #endregion
}
