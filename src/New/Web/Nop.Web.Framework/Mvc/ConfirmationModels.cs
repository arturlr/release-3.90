namespace Nop.Web.Framework.Mvc;

public class ActionConfirmationModel
{
    public string? ControllerName { get; set; }
    public string? ActionName { get; set; }
    public string? WindowId { get; set; }
    public string? AdditionalConfirmText { get; set; }
}

public class DeleteConfirmationModel : BaseNopEntityModel
{
    public string? ControllerName { get; set; }
    public string? ActionName { get; set; }
    public string? WindowId { get; set; }
}
