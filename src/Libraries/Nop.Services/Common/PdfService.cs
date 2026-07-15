// RTL Support provided by Credo inc (www.credo.co.il  ||   info@credo.co.il)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using iText.Kernel.Pdf;
using iText.Kernel.Font;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Font;
using iText.IO.Image;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Core.Html;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;

namespace Nop.Services.Common
{
    /// <summary>
    /// PDF service
    /// </summary>
    public partial class PdfService : IPdfService
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ILanguageService _languageService;
        private readonly IWorkContext _workContext;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IPriceFormatter _priceFormatter;
        private readonly ICurrencyService _currencyService;
        private readonly IMeasureService _measureService;
        private readonly IPictureService _pictureService;
        private readonly IProductService _productService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IStoreService _storeService;
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingContext;
        private readonly IAddressAttributeFormatter _addressAttributeFormatter;

        private readonly CatalogSettings _catalogSettings;
        private readonly CurrencySettings _currencySettings;
        private readonly MeasureSettings _measureSettings;
        private readonly PdfSettings _pdfSettings;
        private readonly TaxSettings _taxSettings;
        private readonly AddressSettings _addressSettings;

        #endregion

        #region Ctor

        public PdfService(ILocalizationService localizationService,
            ILanguageService languageService,
            IWorkContext workContext,
            IOrderService orderService,
            IPaymentService paymentService,
            IDateTimeHelper dateTimeHelper,
            IPriceFormatter priceFormatter,
            ICurrencyService currencyService,
            IMeasureService measureService,
            IPictureService pictureService,
            IProductService productService,
            IProductAttributeParser productAttributeParser,
            IStoreService storeService,
            IStoreContext storeContext,
            ISettingService settingContext,
            IAddressAttributeFormatter addressAttributeFormatter,
            CatalogSettings catalogSettings,
            CurrencySettings currencySettings,
            MeasureSettings measureSettings,
            PdfSettings pdfSettings,
            TaxSettings taxSettings,
            AddressSettings addressSettings)
        {
            this._localizationService = localizationService;
            this._languageService = languageService;
            this._workContext = workContext;
            this._orderService = orderService;
            this._paymentService = paymentService;
            this._dateTimeHelper = dateTimeHelper;
            this._priceFormatter = priceFormatter;
            this._currencyService = currencyService;
            this._measureService = measureService;
            this._pictureService = pictureService;
            this._productService = productService;
            this._productAttributeParser = productAttributeParser;
            this._storeService = storeService;
            this._storeContext = storeContext;
            this._settingContext = settingContext;
            this._addressAttributeFormatter = addressAttributeFormatter;
            this._currencySettings = currencySettings;
            this._catalogSettings = catalogSettings;
            this._measureSettings = measureSettings;
            this._pdfSettings = pdfSettings;
            this._taxSettings = taxSettings;
            this._addressSettings = addressSettings;
        }

        #endregion

        #region Utilities

        protected virtual PdfFont GetFont()
        {
            return GetFont(_pdfSettings.FontFileName);
        }

        protected virtual PdfFont GetFont(string fontFileName)
        {
            if (fontFileName == null)
                throw new ArgumentNullException("fontFileName");

            string fontPath = System.IO.Path.Combine(CommonHelper.MapPath("~/App_Data/Pdf/"), fontFileName);
            return PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
        }

        protected virtual PageSize GetPageSize()
        {
            return _pdfSettings.LetterPageSizeEnabled ? PageSize.LETTER : PageSize.A4;
        }

        protected virtual TextAlignment GetAlignment(Language lang, bool isOpposite = false)
        {
            if (!isOpposite)
                return lang.Rtl ? TextAlignment.RIGHT : TextAlignment.LEFT;
            return lang.Rtl ? TextAlignment.LEFT : TextAlignment.RIGHT;
        }

        #endregion


        #region Methods

        public virtual string PrintOrderToPdf(Order order, int languageId = 0, int vendorId = 0)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            string fileName = string.Format("order_{0}_{1}.pdf", order.OrderGuid, CommonHelper.GenerateRandomDigitCode(4));
            string filePath = System.IO.Path.Combine(CommonHelper.MapPath("~/content/files/ExportImport"), fileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var orders = new List<Order>();
                orders.Add(order);
                PrintOrdersToPdf(fileStream, orders, languageId, vendorId);
            }
            return filePath;
        }

        public virtual void PrintOrdersToPdf(Stream stream, IList<Order> orders, int languageId = 0, int vendorId = 0)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (orders == null)
                throw new ArgumentNullException("orders");

            var pdfWriter = new PdfWriter(stream);
            var pdfDoc = new PdfDocument(pdfWriter);
            var doc = new Document(pdfDoc, GetPageSize());

            var font = GetFont();
            var fontSize = 10f;
            var titleFontSize = 10f;

            int ordCount = orders.Count;
            int ordNum = 0;

            foreach (var order in orders)
            {
                var pdfSettingsByStore = _settingContext.LoadSetting<PdfSettings>(order.StoreId);
                var lang = _languageService.GetLanguageById(languageId == 0 ? order.CustomerLanguageId : languageId);
                if (lang == null || !lang.Published)
                    lang = _workContext.WorkingLanguage;

                // Header
                var store = _storeService.GetStoreById(order.StoreId) ?? _storeContext.CurrentStore;

                var headerParagraph = new Paragraph()
                    .SetFont(font).SetFontSize(titleFontSize)
                    .Add(String.Format(_localizationService.GetResource("PDFInvoice.Order#", lang.Id), order.CustomOrderNumber));
                doc.Add(headerParagraph);

                doc.Add(new Paragraph(store.Url.Trim(new[] { '/' }))
                    .SetFont(font).SetFontSize(fontSize));
                doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFInvoice.OrderDate", lang.Id),
                    _dateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc).ToString("D", new CultureInfo(lang.LanguageCulture))))
                    .SetFont(font).SetFontSize(fontSize));

                // Logo
                var logoPicture = _pictureService.GetPictureById(pdfSettingsByStore.LogoPictureId);
                if (logoPicture != null)
                {
                    var logoFilePath = _pictureService.GetThumbLocalPath(logoPicture, 0, false);
                    if (File.Exists(logoFilePath))
                    {
                        var logo = new Image(ImageDataFactory.Create(logoFilePath));
                        logo.ScaleToFit(65f, 65f);
                        logo.SetHorizontalAlignment(lang.Rtl ? HorizontalAlignment.LEFT : HorizontalAlignment.RIGHT);
                        doc.Add(logo);
                    }
                }

                doc.Add(new Paragraph(" ").SetFont(font).SetFontSize(fontSize));

                // Billing Address
                doc.Add(new Paragraph(_localizationService.GetResource("PDFInvoice.BillingInformation", lang.Id))
                    .SetFont(font).SetFontSize(titleFontSize));

                if (_addressSettings.CompanyEnabled && !String.IsNullOrEmpty(order.BillingAddress.Company))
                    doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Company", lang.Id), order.BillingAddress.Company))
                        .SetFont(font).SetFontSize(fontSize));

                doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Name", lang.Id),
                    order.BillingAddress.FirstName + " " + order.BillingAddress.LastName))
                    .SetFont(font).SetFontSize(fontSize));

                if (_addressSettings.PhoneEnabled)
                    doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Phone", lang.Id), order.BillingAddress.PhoneNumber))
                        .SetFont(font).SetFontSize(fontSize));

                if (_addressSettings.StreetAddressEnabled)
                    doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Address", lang.Id), order.BillingAddress.Address1))
                        .SetFont(font).SetFontSize(fontSize));

                if (_addressSettings.StreetAddress2Enabled && !String.IsNullOrEmpty(order.BillingAddress.Address2))
                    doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Address2", lang.Id), order.BillingAddress.Address2))
                        .SetFont(font).SetFontSize(fontSize));

                if (_addressSettings.CityEnabled || _addressSettings.StateProvinceEnabled || _addressSettings.ZipPostalCodeEnabled)
                    doc.Add(new Paragraph("   " + String.Format("{0}, {1} {2}", order.BillingAddress.City,
                        order.BillingAddress.StateProvince != null ? order.BillingAddress.StateProvince.GetLocalized(x => x.Name, lang.Id) : "",
                        order.BillingAddress.ZipPostalCode))
                        .SetFont(font).SetFontSize(fontSize));

                if (_addressSettings.CountryEnabled && order.BillingAddress.Country != null)
                    doc.Add(new Paragraph("   " + order.BillingAddress.Country.GetLocalized(x => x.Name, lang.Id))
                        .SetFont(font).SetFontSize(fontSize));

                if (!String.IsNullOrEmpty(order.VatNumber))
                    doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.VATNumber", lang.Id), order.VatNumber))
                        .SetFont(font).SetFontSize(fontSize));

                // Payment method
                if (vendorId == 0)
                {
                    var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(order.PaymentMethodSystemName);
                    string paymentMethodStr = paymentMethod != null ? paymentMethod.GetLocalizedFriendlyName(_localizationService, lang.Id) : order.PaymentMethodSystemName;
                    if (!String.IsNullOrEmpty(paymentMethodStr))
                    {
                        doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.PaymentMethod", lang.Id), paymentMethodStr))
                            .SetFont(font).SetFontSize(fontSize));
                    }
                }

                doc.Add(new Paragraph(" ").SetFont(font).SetFontSize(fontSize));

                // Shipping Address
                if (order.ShippingStatus != ShippingStatus.ShippingNotRequired)
                {
                    doc.Add(new Paragraph(_localizationService.GetResource("PDFInvoice.ShippingInformation", lang.Id))
                        .SetFont(font).SetFontSize(titleFontSize));

                    if (!order.PickUpInStore && order.ShippingAddress != null)
                    {
                        if (_addressSettings.CompanyEnabled && !String.IsNullOrEmpty(order.ShippingAddress.Company))
                            doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Company", lang.Id), order.ShippingAddress.Company))
                                .SetFont(font).SetFontSize(fontSize));

                        doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Name", lang.Id),
                            order.ShippingAddress.FirstName + " " + order.ShippingAddress.LastName))
                            .SetFont(font).SetFontSize(fontSize));

                        if (_addressSettings.PhoneEnabled)
                            doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Phone", lang.Id), order.ShippingAddress.PhoneNumber))
                                .SetFont(font).SetFontSize(fontSize));

                        if (_addressSettings.StreetAddressEnabled)
                            doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Address", lang.Id), order.ShippingAddress.Address1))
                                .SetFont(font).SetFontSize(fontSize));

                        if (_addressSettings.CityEnabled || _addressSettings.StateProvinceEnabled || _addressSettings.ZipPostalCodeEnabled)
                            doc.Add(new Paragraph("   " + String.Format("{0}, {1} {2}", order.ShippingAddress.City,
                                order.ShippingAddress.StateProvince != null ? order.ShippingAddress.StateProvince.GetLocalized(x => x.Name, lang.Id) : "",
                                order.ShippingAddress.ZipPostalCode))
                                .SetFont(font).SetFontSize(fontSize));

                        if (_addressSettings.CountryEnabled && order.ShippingAddress.Country != null)
                            doc.Add(new Paragraph("   " + order.ShippingAddress.Country.GetLocalized(x => x.Name, lang.Id))
                                .SetFont(font).SetFontSize(fontSize));
                    }

                    doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.ShippingMethod", lang.Id), order.ShippingMethod))
                        .SetFont(font).SetFontSize(fontSize));
                    doc.Add(new Paragraph(" ").SetFont(font).SetFontSize(fontSize));
                }

                // Products table
                var productsTable = new Table(new float[] { 4, 1, 1, 1 }).UseAllAvailableWidth();

                // Header cells
                var headerCellBg = new DeviceRgb(200, 200, 200);
                productsTable.AddHeaderCell(new Cell().Add(new Paragraph(_localizationService.GetResource("PDFInvoice.ProductName", lang.Id)).SetFont(font).SetFontSize(fontSize)).SetBackgroundColor(headerCellBg));
                productsTable.AddHeaderCell(new Cell().Add(new Paragraph(_localizationService.GetResource("PDFInvoice.ProductPrice", lang.Id)).SetFont(font).SetFontSize(fontSize)).SetBackgroundColor(headerCellBg));
                productsTable.AddHeaderCell(new Cell().Add(new Paragraph(_localizationService.GetResource("PDFInvoice.ProductQuantity", lang.Id)).SetFont(font).SetFontSize(fontSize)).SetBackgroundColor(headerCellBg));
                productsTable.AddHeaderCell(new Cell().Add(new Paragraph(_localizationService.GetResource("PDFInvoice.ProductTotal", lang.Id)).SetFont(font).SetFontSize(fontSize)).SetBackgroundColor(headerCellBg));

                foreach (var orderItem in order.OrderItems)
                {
                    if (vendorId > 0 && orderItem.Product.VendorId != vendorId)
                        continue;

                    var product = orderItem.Product;
                    var pName = product.GetLocalized(x => x.Name, lang.Id);

                    // Product name cell (with attributes)
                    var nameCell = new Cell();
                    nameCell.Add(new Paragraph(pName).SetFont(font).SetFontSize(fontSize));
                    if (!String.IsNullOrEmpty(orderItem.AttributeDescription))
                    {
                        nameCell.Add(new Paragraph(HtmlHelper.ConvertHtmlToPlainText(orderItem.AttributeDescription, true, true))
                            .SetFont(font).SetFontSize(fontSize - 1));
                    }
                    // SKU
                    var sku = product.FormatSku(orderItem.AttributesXml, _productAttributeParser);
                    if (!String.IsNullOrEmpty(sku))
                    {
                        nameCell.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFInvoice.SKU", lang.Id), sku))
                            .SetFont(font).SetFontSize(fontSize - 1));
                    }
                    productsTable.AddCell(nameCell);

                    // Price
                    string unitPrice;
                    if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
                    {
                        var unitPriceInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(orderItem.UnitPriceInclTax, order.CurrencyRate);
                        unitPrice = _priceFormatter.FormatPrice(unitPriceInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, lang, true);
                    }
                    else
                    {
                        var unitPriceExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(orderItem.UnitPriceExclTax, order.CurrencyRate);
                        unitPrice = _priceFormatter.FormatPrice(unitPriceExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, lang, false);
                    }
                    productsTable.AddCell(new Cell().Add(new Paragraph(unitPrice).SetFont(font).SetFontSize(fontSize)));

                    // Quantity
                    productsTable.AddCell(new Cell().Add(new Paragraph(orderItem.Quantity.ToString()).SetFont(font).SetFontSize(fontSize)));

                    // Total
                    string subTotal;
                    if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
                    {
                        var priceInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(orderItem.PriceInclTax, order.CurrencyRate);
                        subTotal = _priceFormatter.FormatPrice(priceInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, lang, true);
                    }
                    else
                    {
                        var priceExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(orderItem.PriceExclTax, order.CurrencyRate);
                        subTotal = _priceFormatter.FormatPrice(priceExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, lang, false);
                    }
                    productsTable.AddCell(new Cell().Add(new Paragraph(subTotal).SetFont(font).SetFontSize(fontSize)));
                }

                doc.Add(productsTable);
                doc.Add(new Paragraph(" ").SetFont(font).SetFontSize(fontSize));

                // Totals
                if (vendorId == 0)
                {
                    // Subtotal
                    var orderSubtotal = order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax
                        ? _currencyService.ConvertCurrency(order.OrderSubtotalInclTax, order.CurrencyRate)
                        : _currencyService.ConvertCurrency(order.OrderSubtotalExclTax, order.CurrencyRate);
                    var orderSubtotalStr = _priceFormatter.FormatPrice(orderSubtotal, true, order.CustomerCurrencyCode, lang, order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax);
                    doc.Add(new Paragraph(String.Format("{0} {1}", _localizationService.GetResource("PDFInvoice.Sub-Total", lang.Id), orderSubtotalStr))
                        .SetFont(font).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));

                    // Shipping
                    if (order.ShippingStatus != ShippingStatus.ShippingNotRequired)
                    {
                        var orderShipping = order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax
                            ? _currencyService.ConvertCurrency(order.OrderShippingInclTax, order.CurrencyRate)
                            : _currencyService.ConvertCurrency(order.OrderShippingExclTax, order.CurrencyRate);
                        var orderShippingStr = _priceFormatter.FormatShippingPrice(orderShipping, true, order.CustomerCurrencyCode, lang, order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax);
                        doc.Add(new Paragraph(String.Format("{0} {1}", _localizationService.GetResource("PDFInvoice.Shipping", lang.Id), orderShippingStr))
                            .SetFont(font).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));
                    }

                    // Tax
                    var orderTax = _currencyService.ConvertCurrency(order.OrderTax, order.CurrencyRate);
                    var taxStr = _priceFormatter.FormatPrice(orderTax, true, order.CustomerCurrencyCode, false, lang);
                    if (orderTax > 0)
                    {
                        doc.Add(new Paragraph(String.Format("{0} {1}", _localizationService.GetResource("PDFInvoice.Tax", lang.Id), taxStr))
                            .SetFont(font).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));
                    }

                    // Discount
                    if (order.OrderDiscount > 0)
                    {
                        var orderDiscountInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderDiscount, order.CurrencyRate);
                        var orderDiscountStr = _priceFormatter.FormatPrice(-orderDiscountInCustomerCurrency, true, order.CustomerCurrencyCode, false, lang);
                        doc.Add(new Paragraph(String.Format("{0} {1}", _localizationService.GetResource("PDFInvoice.Discount", lang.Id), orderDiscountStr))
                            .SetFont(font).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));
                    }

                    // Total
                    var orderTotal = _currencyService.ConvertCurrency(order.OrderTotal, order.CurrencyRate);
                    var orderTotalStr = _priceFormatter.FormatPrice(orderTotal, true, order.CustomerCurrencyCode, false, lang);
                    doc.Add(new Paragraph(String.Format("{0} {1}", _localizationService.GetResource("PDFInvoice.OrderTotal", lang.Id), orderTotalStr))
                        .SetFont(font).SetFontSize(titleFontSize).SetTextAlignment(TextAlignment.RIGHT));
                }

                ordNum++;
                if (ordNum < ordCount)
                {
                    doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                }
            }

            doc.Close();
        }


        public virtual void PrintPackagingSlipsToPdf(Stream stream, IList<Shipment> shipments, int languageId = 0)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (shipments == null)
                throw new ArgumentNullException("shipments");

            var pdfWriter = new PdfWriter(stream);
            var pdfDoc = new PdfDocument(pdfWriter);
            var doc = new Document(pdfDoc, GetPageSize());

            var font = GetFont();
            var fontSize = 10f;
            var titleFontSize = 10f;

            int shipmentCount = shipments.Count;
            int shipmentNum = 0;

            foreach (var shipment in shipments)
            {
                var order = shipment.Order;
                var lang = _languageService.GetLanguageById(languageId == 0 ? order.CustomerLanguageId : languageId);
                if (lang == null || !lang.Published)
                    lang = _workContext.WorkingLanguage;

                doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFPackagingSlip.Shipment", lang.Id), shipment.Id))
                    .SetFont(font).SetFontSize(titleFontSize));
                doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFPackagingSlip.Order", lang.Id), order.CustomOrderNumber))
                    .SetFont(font).SetFontSize(titleFontSize));

                if (!order.PickUpInStore)
                {
                    if (order.ShippingAddress == null)
                        throw new NopException(string.Format("Shipping is required, but address is not available. Order ID = {0}", order.Id));

                    if (_addressSettings.CompanyEnabled && !String.IsNullOrEmpty(order.ShippingAddress.Company))
                        doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFPackagingSlip.Company", lang.Id), order.ShippingAddress.Company))
                            .SetFont(font).SetFontSize(fontSize));

                    doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFPackagingSlip.Name", lang.Id),
                        order.ShippingAddress.FirstName + " " + order.ShippingAddress.LastName))
                        .SetFont(font).SetFontSize(fontSize));

                    if (_addressSettings.PhoneEnabled)
                        doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFPackagingSlip.Phone", lang.Id), order.ShippingAddress.PhoneNumber))
                            .SetFont(font).SetFontSize(fontSize));

                    if (_addressSettings.StreetAddressEnabled)
                        doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFPackagingSlip.Address", lang.Id), order.ShippingAddress.Address1))
                            .SetFont(font).SetFontSize(fontSize));

                    if (_addressSettings.StreetAddress2Enabled && !String.IsNullOrEmpty(order.ShippingAddress.Address2))
                        doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFPackagingSlip.Address2", lang.Id), order.ShippingAddress.Address2))
                            .SetFont(font).SetFontSize(fontSize));

                    if (_addressSettings.CityEnabled || _addressSettings.StateProvinceEnabled || _addressSettings.ZipPostalCodeEnabled)
                        doc.Add(new Paragraph(String.Format("{0}, {1} {2}", order.ShippingAddress.City,
                            order.ShippingAddress.StateProvince != null ? order.ShippingAddress.StateProvince.GetLocalized(x => x.Name, lang.Id) : "",
                            order.ShippingAddress.ZipPostalCode))
                            .SetFont(font).SetFontSize(fontSize));

                    if (_addressSettings.CountryEnabled && order.ShippingAddress.Country != null)
                        doc.Add(new Paragraph(order.ShippingAddress.Country.GetLocalized(x => x.Name, lang.Id))
                            .SetFont(font).SetFontSize(fontSize));
                }
                else if (order.PickupAddress != null)
                {
                    doc.Add(new Paragraph(_localizationService.GetResource("PDFInvoice.Pickup", lang.Id))
                        .SetFont(font).SetFontSize(titleFontSize));
                    if (!string.IsNullOrEmpty(order.PickupAddress.Address1))
                        doc.Add(new Paragraph("   " + string.Format(_localizationService.GetResource("PDFInvoice.Address", lang.Id), order.PickupAddress.Address1))
                            .SetFont(font).SetFontSize(fontSize));
                    if (!string.IsNullOrEmpty(order.PickupAddress.City))
                        doc.Add(new Paragraph("   " + order.PickupAddress.City).SetFont(font).SetFontSize(fontSize));
                    if (order.PickupAddress.Country != null)
                        doc.Add(new Paragraph("   " + order.PickupAddress.Country.GetLocalized(x => x.Name, lang.Id)).SetFont(font).SetFontSize(fontSize));
                    if (!string.IsNullOrEmpty(order.PickupAddress.ZipPostalCode))
                        doc.Add(new Paragraph("   " + order.PickupAddress.ZipPostalCode).SetFont(font).SetFontSize(fontSize));
                }

                doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFPackagingSlip.ShippingMethod", lang.Id), order.ShippingMethod))
                    .SetFont(font).SetFontSize(fontSize));
                doc.Add(new Paragraph(" ").SetFont(font).SetFontSize(fontSize));

                // Products table
                var productsTable = new Table(new float[] { 3, 1, 1 }).UseAllAvailableWidth();
                var headerCellBg = new DeviceRgb(200, 200, 200);

                productsTable.AddHeaderCell(new Cell().Add(new Paragraph(_localizationService.GetResource("PDFPackagingSlip.ProductName", lang.Id)).SetFont(font).SetFontSize(fontSize)).SetBackgroundColor(headerCellBg));
                productsTable.AddHeaderCell(new Cell().Add(new Paragraph(_localizationService.GetResource("PDFPackagingSlip.SKU", lang.Id)).SetFont(font).SetFontSize(fontSize)).SetBackgroundColor(headerCellBg));
                productsTable.AddHeaderCell(new Cell().Add(new Paragraph(_localizationService.GetResource("PDFPackagingSlip.QTY", lang.Id)).SetFont(font).SetFontSize(fontSize)).SetBackgroundColor(headerCellBg));

                foreach (var si in shipment.ShipmentItems)
                {
                    var orderItem = _orderService.GetOrderItemById(si.OrderItemId);
                    if (orderItem == null)
                        continue;

                    var p = orderItem.Product;
                    string name = p.GetLocalized(x => x.Name, lang.Id);

                    var nameCell = new Cell();
                    nameCell.Add(new Paragraph(name).SetFont(font).SetFontSize(fontSize));
                    if (!String.IsNullOrEmpty(orderItem.AttributeDescription))
                    {
                        nameCell.Add(new Paragraph(HtmlHelper.ConvertHtmlToPlainText(orderItem.AttributeDescription, true, true))
                            .SetFont(font).SetFontSize(fontSize - 1));
                    }
                    if (orderItem.Product.IsRental)
                    {
                        var rentalStartDate = orderItem.RentalStartDateUtc.HasValue ? orderItem.Product.FormatRentalDate(orderItem.RentalStartDateUtc.Value) : "";
                        var rentalEndDate = orderItem.RentalEndDateUtc.HasValue ? orderItem.Product.FormatRentalDate(orderItem.RentalEndDateUtc.Value) : "";
                        var rentalInfo = string.Format(_localizationService.GetResource("Order.Rental.FormattedDate"), rentalStartDate, rentalEndDate);
                        nameCell.Add(new Paragraph(rentalInfo).SetFont(font).SetFontSize(fontSize - 1));
                    }
                    productsTable.AddCell(nameCell);

                    var sku = p.FormatSku(orderItem.AttributesXml, _productAttributeParser);
                    productsTable.AddCell(new Cell().Add(new Paragraph(sku ?? String.Empty).SetFont(font).SetFontSize(fontSize)).SetTextAlignment(TextAlignment.CENTER));
                    productsTable.AddCell(new Cell().Add(new Paragraph(si.Quantity.ToString()).SetFont(font).SetFontSize(fontSize)).SetTextAlignment(TextAlignment.CENTER));
                }
                doc.Add(productsTable);

                shipmentNum++;
                if (shipmentNum < shipmentCount)
                {
                    doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                }
            }

            doc.Close();
        }

        public virtual void PrintProductsToPdf(Stream stream, IList<Product> products)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (products == null)
                throw new ArgumentNullException("products");

            var lang = _workContext.WorkingLanguage;
            var pdfWriter = new PdfWriter(stream);
            var pdfDoc = new PdfDocument(pdfWriter);
            var doc = new Document(pdfDoc, GetPageSize());

            var font = GetFont();
            var fontSize = 10f;
            var titleFontSize = 10f;

            int productCount = products.Count;
            int productNum = 0;

            foreach (var product in products)
            {
                string productName = product.GetLocalized(x => x.Name, lang.Id);
                string productDescription = HtmlHelper.ConvertHtmlToPlainText(product.GetLocalized(x => x.FullDescription, lang.Id), false, true);
                var sku = product.FormatSku(null, _productAttributeParser);

                doc.Add(new Paragraph(productName).SetFont(font).SetFontSize(titleFontSize));

                if (!String.IsNullOrEmpty(sku))
                    doc.Add(new Paragraph(String.Format("{0}: {1}", _localizationService.GetResource("PDFProductCatalog.SKU", lang.Id), sku))
                        .SetFont(font).SetFontSize(fontSize));

                if (product.ProductType == ProductType.SimpleProduct)
                {
                    doc.Add(new Paragraph(String.Format("{0}: {1}", _localizationService.GetResource("PDFProductCatalog.Price", lang.Id),
                        _priceFormatter.FormatPrice(product.Price, true, _workContext.WorkingCurrency, lang, false)))
                        .SetFont(font).SetFontSize(fontSize));
                }

                if (product.IsShipEnabled && product.Weight > 0)
                {
                    doc.Add(new Paragraph(String.Format("{0}: {1} {2}", _localizationService.GetResource("PDFProductCatalog.Weight", lang.Id),
                        product.Weight.ToString("G29"),
                        _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId) != null
                            ? _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId).Name
                            : ""))
                        .SetFont(font).SetFontSize(fontSize));
                }

                if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock)
                {
                    doc.Add(new Paragraph(String.Format("{0}: {1}", _localizationService.GetResource("PDFProductCatalog.StockQuantity", lang.Id), product.GetTotalStockQuantity()))
                        .SetFont(font).SetFontSize(fontSize));
                }

                doc.Add(new Paragraph(" ").SetFont(font).SetFontSize(fontSize));

                if (!String.IsNullOrEmpty(productDescription))
                    doc.Add(new Paragraph(productDescription).SetFont(font).SetFontSize(fontSize));

                doc.Add(new Paragraph(" ").SetFont(font).SetFontSize(fontSize));

                // Product picture
                var pictures = _pictureService.GetPicturesByProductId(product.Id);
                if (pictures.Any())
                {
                    var picturePath = _pictureService.GetThumbLocalPath(pictures.First(), 200, false);
                    if (!string.IsNullOrEmpty(picturePath) && File.Exists(picturePath))
                    {
                        var productImage = new Image(ImageDataFactory.Create(picturePath));
                        productImage.ScaleToFit(200f, 200f);
                        doc.Add(productImage);
                    }
                }

                productNum++;
                if (productNum < productCount)
                {
                    doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                }
            }

            doc.Close();
        }

        #endregion
    }
}
