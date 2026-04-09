using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;

namespace Nop.Services.Common;

/// <summary>
/// PDF generation service — produces order invoices, packing slips, and product catalogs.
/// Full implementation requires a PDF library (QuestPDF, iText7, or similar)
/// and depends on Phase 4 services (IOrderService, IProductService, etc.).
/// </summary>
public interface IPdfService
{
    Task PrintOrderToPdfAsync(Stream stream, Order order, int languageId = 0, int vendorId = 0);
    Task PrintOrdersToPdfAsync(Stream stream, IList<Order> orders, int languageId = 0, int vendorId = 0);
    Task PrintPackagingSlipsToPdfAsync(Stream stream, IList<Shipment> shipments, int languageId = 0);
}
