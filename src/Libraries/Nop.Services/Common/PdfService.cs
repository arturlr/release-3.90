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

        /// <summary>
        /// Get font
        /// </summary>
        /// <returns>Font</returns>
        protected virtual PdfFont GetFont()
        {
            return GetFont(_pdfSettings.FontFileName);
        }
        /// <summary>
        /// Get font
        /// </summary>
        /// <param name="fontFileName">Font file name</param>
        /// <returns>Font</returns>
        protected virtual PdfFont GetFont(string fontFileName)
        {
            if (fontFileName == null)
                throw new ArgumentNullException("fontFileName");

            string fontPath = System.IO.Path.Combine(CommonHelper.MapPath("~/App_Data/Pdf/"), fontFileName);
            var font = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
            return font;
        }

        /// <summary>
        /// Get text alignment based on language direction
        /// </summary>
        /// <param name="lang">Language</param>
        /// <param name="isOpposite">Is opposite?</param>
        /// <returns>Text alignment</returns>
        protected virtual TextAlignment GetAlignment(Language lang, bool isOpposite = false)
        {
            if (!isOpposite)
                return lang.Rtl ? TextAlignment.RIGHT : TextAlignment.LEFT;
            return lang.Rtl ? TextAlignment.LEFT : TextAlignment.RIGHT;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Print an order to PDF
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
        /// <param name="vendorId">Vendor identifier to limit products; 0 to to print all products. If specified, then totals won't be printed</param>
        /// <returns>A path of generated file</returns>
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

        /// <summary>
        /// Print orders to PDF
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="orders">Orders</param>
        /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
        /// <param name="vendorId">Vendor identifier to limit products; 0 to to print all products. If specified, then totals won't be printed</param>
        public virtual void PrintOrdersToPdf(Stream stream, IList<Order> orders, int languageId = 0, int vendorId = 0)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (orders == null)
                throw new ArgumentNullException("orders");

            var pageSize = _pdfSettings.LetterPageSizeEnabled ? PageSize.LETTER : PageSize.A4;

            var pdfWriter = new PdfWriter(stream);
            var pdfDoc = new PdfDocument(pdfWriter);
            pdfDoc.SetDefaultPageSize(pageSize);
            var doc = new Document(pdfDoc);

            //fonts
            var pdfFont = GetFont();

            int ordCount = orders.Count;
            int ordNum = 0;

            foreach (var order in orders)
            {
                var pdfSettingsByStore = _settingContext.LoadSetting<PdfSettings>(order.StoreId);

                var lang = _languageService.GetLanguageById(languageId == 0 ? order.CustomerLanguageId : languageId);
                if (lang == null || !lang.Published)
                    lang = _workContext.WorkingLanguage;

                #region Header

                var logoPicture = _pictureService.GetPictureById(pdfSettingsByStore.LogoPictureId);
                var logoExists = logoPicture != null;

                var store = _storeService.GetStoreById(order.StoreId) ?? _storeContext.CurrentStore;

                // Order header
                var headerParagraph = new Paragraph()
                    .SetFont(pdfFont)
                    .SetFontSize(14)
                    .SetBold()
                    .Add(String.Format(_localizationService.GetResource("PDFInvoice.Order#", lang.Id), order.CustomOrderNumber));
                doc.Add(headerParagraph);

                doc.Add(new Paragraph(store.Url.Trim(new[] { '/' })).SetFont(pdfFont).SetFontSize(10));
                doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFInvoice.OrderDate", lang.Id), _dateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc).ToString("D", new CultureInfo(lang.LanguageCulture)))).SetFont(pdfFont).SetFontSize(10));

                if (logoExists)
                {
                    var logoFilePath = _pictureService.GetThumbLocalPath(logoPicture, 0, false);
                    if (!string.IsNullOrEmpty(logoFilePath) && File.Exists(logoFilePath))
                    {
                        var logo = ImageDataFactory.Create(logoFilePath);
                        var logoImage = new iText.Layout.Element.Image(logo).ScaleToFit(65f, 65f);
                        doc.Add(logoImage);
                    }
                }

                doc.Add(new Paragraph(" "));

                #endregion

                #region Addresses

                // Billing info
                doc.Add(new Paragraph(_localizationService.GetResource("PDFInvoice.BillingInformation", lang.Id)).SetFont(pdfFont).SetBold().SetFontSize(10));

                if (_addressSettings.CompanyEnabled && !String.IsNullOrEmpty(order.BillingAddress.Company))
                    doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Company", lang.Id), order.BillingAddress.Company)).SetFont(pdfFont).SetFontSize(10));

                doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Name", lang.Id), order.BillingAddress.FirstName + " " + order.BillingAddress.LastName)).SetFont(pdfFont).SetFontSize(10));
                if (_addressSettings.PhoneEnabled)
                    doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Phone", lang.Id), order.BillingAddress.PhoneNumber)).SetFont(pdfFont).SetFontSize(10));
                if (_addressSettings.FaxEnabled && !String.IsNullOrEmpty(order.BillingAddress.FaxNumber))
                    doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Fax", lang.Id), order.BillingAddress.FaxNumber)).SetFont(pdfFont).SetFontSize(10));
                if (_addressSettings.StreetAddressEnabled)
                    doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Address", lang.Id), order.BillingAddress.Address1)).SetFont(pdfFont).SetFontSize(10));
                if (_addressSettings.StreetAddress2Enabled && !String.IsNullOrEmpty(order.BillingAddress.Address2))
                    doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Address2", lang.Id), order.BillingAddress.Address2)).SetFont(pdfFont).SetFontSize(10));
                if (_addressSettings.CityEnabled || _addressSettings.StateProvinceEnabled || _addressSettings.ZipPostalCodeEnabled)
                    doc.Add(new Paragraph("   " + String.Format("{0}, {1} {2}", order.BillingAddress.City, order.BillingAddress.StateProvince != null ? order.BillingAddress.StateProvince.GetLocalized(x => x.Name, lang.Id) : "", order.BillingAddress.ZipPostalCode)).SetFont(pdfFont).SetFontSize(10));
                if (_addressSettings.CountryEnabled && order.BillingAddress.Country != null)
                    doc.Add(new Paragraph("   " + order.BillingAddress.Country.GetLocalized(x => x.Name, lang.Id)).SetFont(pdfFont).SetFontSize(10));

                if (!String.IsNullOrEmpty(order.VatNumber))
                    doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.VATNumber", lang.Id), order.VatNumber)).SetFont(pdfFont).SetFontSize(10));

                var customBillingAddressAttributes = _addressAttributeFormatter.FormatAttributes(order.BillingAddress.CustomAttributes);
                if (!String.IsNullOrEmpty(customBillingAddressAttributes))
                    doc.Add(new Paragraph("   " + HtmlHelper.ConvertHtmlToPlainText(customBillingAddressAttributes, true, true)).SetFont(pdfFont).SetFontSize(10));

                // Payment method
                if (vendorId == 0)
                {
                    var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(order.PaymentMethodSystemName);
                    string paymentMethodStr = paymentMethod != null ? paymentMethod.GetLocalizedFriendlyName(_localizationService, lang.Id) : order.PaymentMethodSystemName;
                    if (!String.IsNullOrEmpty(paymentMethodStr))
                    {
                        doc.Add(new Paragraph(" "));
                        doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.PaymentMethod", lang.Id), paymentMethodStr)).SetFont(pdfFont).SetFontSize(10));
                    }

                    var customValues = order.DeserializeCustomValues();
                    if (customValues != null)
                    {
                        foreach (var item in customValues)
                        {
                            doc.Add(new Paragraph("   " + item.Key + ": " + item.Value).SetFont(pdfFont).SetFontSize(10));
                        }
                    }
                }

                // Shipping info
                if (order.ShippingStatus != ShippingStatus.ShippingNotRequired)
                {
                    doc.Add(new Paragraph(" "));

                    if (!order.PickUpInStore)
                    {
                        if (order.ShippingAddress == null)
                            throw new NopException(string.Format("Shipping is required, but address is not available. Order ID = {0}", order.Id));

                        doc.Add(new Paragraph(_localizationService.GetResource("PDFInvoice.ShippingInformation", lang.Id)).SetFont(pdfFont).SetBold().SetFontSize(10));
                        if (!String.IsNullOrEmpty(order.ShippingAddress.Company))
                            doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Company", lang.Id), order.ShippingAddress.Company)).SetFont(pdfFont).SetFontSize(10));
                        doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Name", lang.Id), order.ShippingAddress.FirstName + " " + order.ShippingAddress.LastName)).SetFont(pdfFont).SetFontSize(10));
                        if (_addressSettings.PhoneEnabled)
                            doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Phone", lang.Id), order.ShippingAddress.PhoneNumber)).SetFont(pdfFont).SetFontSize(10));
                        if (_addressSettings.FaxEnabled && !String.IsNullOrEmpty(order.ShippingAddress.FaxNumber))
                            doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Fax", lang.Id), order.ShippingAddress.FaxNumber)).SetFont(pdfFont).SetFontSize(10));
                        if (_addressSettings.StreetAddressEnabled)
                            doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Address", lang.Id), order.ShippingAddress.Address1)).SetFont(pdfFont).SetFontSize(10));
                        if (_addressSettings.StreetAddress2Enabled && !String.IsNullOrEmpty(order.ShippingAddress.Address2))
                            doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.Address2", lang.Id), order.ShippingAddress.Address2)).SetFont(pdfFont).SetFontSize(10));
                        if (_addressSettings.CityEnabled || _addressSettings.StateProvinceEnabled || _addressSettings.ZipPostalCodeEnabled)
                            doc.Add(new Paragraph("   " + String.Format("{0}, {1} {2}", order.ShippingAddress.City, order.ShippingAddress.StateProvince != null ? order.ShippingAddress.StateProvince.GetLocalized(x => x.Name, lang.Id) : "", order.ShippingAddress.ZipPostalCode)).SetFont(pdfFont).SetFontSize(10));
                        if (_addressSettings.CountryEnabled && order.ShippingAddress.Country != null)
                            doc.Add(new Paragraph("   " + order.ShippingAddress.Country.GetLocalized(x => x.Name, lang.Id)).SetFont(pdfFont).SetFontSize(10));
                        var customShippingAddressAttributes = _addressAttributeFormatter.FormatAttributes(order.ShippingAddress.CustomAttributes);
                        if (!String.IsNullOrEmpty(customShippingAddressAttributes))
                            doc.Add(new Paragraph("   " + HtmlHelper.ConvertHtmlToPlainText(customShippingAddressAttributes, true, true)).SetFont(pdfFont).SetFontSize(10));
                    }
                    else if (order.PickupAddress != null)
                    {
                        doc.Add(new Paragraph(_localizationService.GetResource("PDFInvoice.Pickup", lang.Id)).SetFont(pdfFont).SetBold().SetFontSize(10));
                        if (!string.IsNullOrEmpty(order.PickupAddress.Address1))
                            doc.Add(new Paragraph("   " + string.Format(_localizationService.GetResource("PDFInvoice.Address", lang.Id), order.PickupAddress.Address1)).SetFont(pdfFont).SetFontSize(10));
                        if (!string.IsNullOrEmpty(order.PickupAddress.City))
                            doc.Add(new Paragraph("   " + order.PickupAddress.City).SetFont(pdfFont).SetFontSize(10));
                        if (order.PickupAddress.Country != null)
                            doc.Add(new Paragraph("   " + order.PickupAddress.Country.GetLocalized(x => x.Name, lang.Id)).SetFont(pdfFont).SetFontSize(10));
                        if (!string.IsNullOrEmpty(order.PickupAddress.ZipPostalCode))
                            doc.Add(new Paragraph("   " + order.PickupAddress.ZipPostalCode).SetFont(pdfFont).SetFontSize(10));
                    }
                    doc.Add(new Paragraph("   " + String.Format(_localizationService.GetResource("PDFInvoice.ShippingMethod", lang.Id), order.ShippingMethod)).SetFont(pdfFont).SetFontSize(10));
                }

                doc.Add(new Paragraph(" "));

                #endregion

                #region Products

                doc.Add(new Paragraph(_localizationService.GetResource("PDFInvoice.Product(s)", lang.Id)).SetFont(pdfFont).SetBold().SetFontSize(10));
                doc.Add(new Paragraph(" "));

                var orderItems = order.OrderItems;
                var numColumns = _catalogSettings.ShowSkuOnProductDetailsPage ? 5 : 4;
                var productsTable = new Table(numColumns).UseAllAvailableWidth();

                // Headers
                productsTable.AddHeaderCell(new Cell().Add(new Paragraph(_localizationService.GetResource("PDFInvoice.ProductName", lang.Id)).SetFont(pdfFont).SetFontSize(9)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                if (_catalogSettings.ShowSkuOnProductDetailsPage)
                    productsTable.AddHeaderCell(new Cell().Add(new Paragraph(_localizationService.GetResource("PDFInvoice.SKU", lang.Id)).SetFont(pdfFont).SetFontSize(9)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                productsTable.AddHeaderCell(new Cell().Add(new Paragraph(_localizationService.GetResource("PDFInvoice.ProductPrice", lang.Id)).SetFont(pdfFont).SetFontSize(9)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                productsTable.AddHeaderCell(new Cell().Add(new Paragraph(_localizationService.GetResource("PDFInvoice.ProductQuantity", lang.Id)).SetFont(pdfFont).SetFontSize(9)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                productsTable.AddHeaderCell(new Cell().Add(new Paragraph(_localizationService.GetResource("PDFInvoice.ProductTotal", lang.Id)).SetFont(pdfFont).SetFontSize(9)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                foreach (var orderItem in orderItems)
                {
                    var p = orderItem.Product;

                    if (vendorId > 0 && p.VendorId != vendorId)
                        continue;

                    string name = p.GetLocalized(x => x.Name, lang.Id);
                    var nameCell = new Cell().Add(new Paragraph(name).SetFont(pdfFont).SetFontSize(9));
                    if (!String.IsNullOrEmpty(orderItem.AttributeDescription))
                        nameCell.Add(new Paragraph(HtmlHelper.ConvertHtmlToPlainText(orderItem.AttributeDescription, true, true)).SetFont(pdfFont).SetFontSize(8).SetItalic());
                    if (orderItem.Product.IsRental)
                    {
                        var rentalStartDate = orderItem.RentalStartDateUtc.HasValue ? orderItem.Product.FormatRentalDate(orderItem.RentalStartDateUtc.Value) : "";
                        var rentalEndDate = orderItem.RentalEndDateUtc.HasValue ? orderItem.Product.FormatRentalDate(orderItem.RentalEndDateUtc.Value) : "";
                        var rentalInfo = string.Format(_localizationService.GetResource("Order.Rental.FormattedDate"), rentalStartDate, rentalEndDate);
                        nameCell.Add(new Paragraph(rentalInfo).SetFont(pdfFont).SetFontSize(8).SetItalic());
                    }
                    productsTable.AddCell(nameCell);

                    if (_catalogSettings.ShowSkuOnProductDetailsPage)
                    {
                        var sku = p.FormatSku(orderItem.AttributesXml, _productAttributeParser);
                        productsTable.AddCell(new Cell().Add(new Paragraph(sku ?? String.Empty).SetFont(pdfFont).SetFontSize(9)));
                    }

                    // price
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
                    productsTable.AddCell(new Cell().Add(new Paragraph(unitPrice).SetFont(pdfFont).SetFontSize(9)));

                    // qty
                    productsTable.AddCell(new Cell().Add(new Paragraph(orderItem.Quantity.ToString()).SetFont(pdfFont).SetFontSize(9)));

                    // total
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
                    productsTable.AddCell(new Cell().Add(new Paragraph(subTotal).SetFont(pdfFont).SetFontSize(9)));
                }
                doc.Add(productsTable);

                #endregion

                #region Totals

                if (vendorId == 0)
                {
                    doc.Add(new Paragraph(" "));

                    // Order subtotal
                    if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax && !_taxSettings.ForceTaxExclusionFromOrderSubtotal)
                    {
                        var orderSubtotalInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderSubtotalInclTax, order.CurrencyRate);
                        string orderSubtotalInclTaxStr = _priceFormatter.FormatPrice(orderSubtotalInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, lang, true);
                        doc.Add(new Paragraph(String.Format("{0} {1}", _localizationService.GetResource("PDFInvoice.Sub-Total", lang.Id), orderSubtotalInclTaxStr)).SetFont(pdfFont).SetFontSize(10).SetTextAlignment(TextAlignment.RIGHT));
                    }
                    else
                    {
                        var orderSubtotalExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderSubtotalExclTax, order.CurrencyRate);
                        string orderSubtotalExclTaxStr = _priceFormatter.FormatPrice(orderSubtotalExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, lang, false);
                        doc.Add(new Paragraph(String.Format("{0} {1}", _localizationService.GetResource("PDFInvoice.Sub-Total", lang.Id), orderSubtotalExclTaxStr)).SetFont(pdfFont).SetFontSize(10).SetTextAlignment(TextAlignment.RIGHT));
                    }

                    // Shipping
                    if (order.ShippingStatus != ShippingStatus.ShippingNotRequired)
                    {
                        if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
                        {
                            var orderShippingInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderShippingInclTax, order.CurrencyRate);
                            string orderShippingInclTaxStr = _priceFormatter.FormatShippingPrice(orderShippingInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, lang, true);
                            doc.Add(new Paragraph(String.Format("{0} {1}", _localizationService.GetResource("PDFInvoice.Shipping", lang.Id), orderShippingInclTaxStr)).SetFont(pdfFont).SetFontSize(10).SetTextAlignment(TextAlignment.RIGHT));
                        }
                        else
                        {
                            var orderShippingExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderShippingExclTax, order.CurrencyRate);
                            string orderShippingExclTaxStr = _priceFormatter.FormatShippingPrice(orderShippingExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, lang, false);
                            doc.Add(new Paragraph(String.Format("{0} {1}", _localizationService.GetResource("PDFInvoice.Shipping", lang.Id), orderShippingExclTaxStr)).SetFont(pdfFont).SetFontSize(10).SetTextAlignment(TextAlignment.RIGHT));
                        }
                    }

                    // Tax
                    if (order.OrderTax > 0 || !_taxSettings.HideZeroTax)
                    {
                        if (!(_taxSettings.HideTaxInOrderSummary && order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax))
                        {
                            var orderTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTax, order.CurrencyRate);
                            string taxStr = _priceFormatter.FormatPrice(orderTaxInCustomerCurrency, true, order.CustomerCurrencyCode, false, lang);
                            doc.Add(new Paragraph(String.Format("{0} {1}", _localizationService.GetResource("PDFInvoice.Tax", lang.Id), taxStr)).SetFont(pdfFont).SetFontSize(10).SetTextAlignment(TextAlignment.RIGHT));
                        }
                    }

                    // Discount
                    if (order.OrderDiscount > decimal.Zero)
                    {
                        var orderDiscountInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderDiscount, order.CurrencyRate);
                        string orderDiscountInCustomerCurrencyStr = _priceFormatter.FormatPrice(-orderDiscountInCustomerCurrency, true, order.CustomerCurrencyCode, false, lang);
                        doc.Add(new Paragraph(String.Format("{0} {1}", _localizationService.GetResource("PDFInvoice.Discount", lang.Id), orderDiscountInCustomerCurrencyStr)).SetFont(pdfFont).SetFontSize(10).SetTextAlignment(TextAlignment.RIGHT));
                    }

                    // Gift cards
                    foreach (var gcuh in order.GiftCardUsageHistory)
                    {
                        string gcTitle = string.Format(_localizationService.GetResource("PDFInvoice.GiftCardInfo", lang.Id), gcuh.GiftCard.GiftCardCouponCode);
                        string gcAmountStr = _priceFormatter.FormatPrice(-(_currencyService.ConvertCurrency(gcuh.UsedValue, order.CurrencyRate)), true, order.CustomerCurrencyCode, false, lang);
                        doc.Add(new Paragraph(String.Format("{0} {1}", gcTitle, gcAmountStr)).SetFont(pdfFont).SetFontSize(10).SetTextAlignment(TextAlignment.RIGHT));
                    }

                    // Reward points
                    if (order.RedeemedRewardPointsEntry != null)
                    {
                        string rpTitle = string.Format(_localizationService.GetResource("PDFInvoice.RewardPoints", lang.Id), -order.RedeemedRewardPointsEntry.Points);
                        string rpAmount = _priceFormatter.FormatPrice(-(_currencyService.ConvertCurrency(order.RedeemedRewardPointsEntry.UsedAmount, order.CurrencyRate)), true, order.CustomerCurrencyCode, false, lang);
                        doc.Add(new Paragraph(String.Format("{0} {1}", rpTitle, rpAmount)).SetFont(pdfFont).SetFontSize(10).SetTextAlignment(TextAlignment.RIGHT));
                    }

                    // Order total
                    var orderTotalInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTotal, order.CurrencyRate);
                    string orderTotalStr = _priceFormatter.FormatPrice(orderTotalInCustomerCurrency, true, order.CustomerCurrencyCode, false, lang);
                    doc.Add(new Paragraph(String.Format("{0} {1}", _localizationService.GetResource("PDFInvoice.OrderTotal", lang.Id), orderTotalStr)).SetFont(pdfFont).SetBold().SetFontSize(10).SetTextAlignment(TextAlignment.RIGHT));
                }

                #endregion

                #region Order notes

                if (pdfSettingsByStore.RenderOrderNotes)
                {
                    var orderNotes = order.OrderNotes
                        .Where(on => on.DisplayToCustomer)
                        .OrderByDescending(on => on.CreatedOnUtc)
                        .ToList();
                    if (orderNotes.Any())
                    {
                        doc.Add(new Paragraph(" "));
                        doc.Add(new Paragraph(_localizationService.GetResource("PDFInvoice.OrderNotes", lang.Id)).SetFont(pdfFont).SetBold().SetFontSize(10));
                        doc.Add(new Paragraph(" "));

                        var notesTable = new Table(2).UseAllAvailableWidth();
                        notesTable.AddHeaderCell(new Cell().Add(new Paragraph(_localizationService.GetResource("PDFInvoice.OrderNotes.CreatedOn", lang.Id)).SetFont(pdfFont).SetFontSize(9)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                        notesTable.AddHeaderCell(new Cell().Add(new Paragraph(_localizationService.GetResource("PDFInvoice.OrderNotes.Note", lang.Id)).SetFont(pdfFont).SetFontSize(9)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                        foreach (var orderNote in orderNotes)
                        {
                            notesTable.AddCell(new Cell().Add(new Paragraph(_dateTimeHelper.ConvertToUserTime(orderNote.CreatedOnUtc, DateTimeKind.Utc).ToString()).SetFont(pdfFont).SetFontSize(9)));
                            notesTable.AddCell(new Cell().Add(new Paragraph(HtmlHelper.ConvertHtmlToPlainText(orderNote.FormatOrderNoteText(), true, true)).SetFont(pdfFont).SetFontSize(9)));
                        }
                        doc.Add(notesTable);
                    }
                }

                #endregion

                ordNum++;
                if (ordNum < ordCount)
                {
                    doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                }
            }
            doc.Close();
        }
        
        /// <summary>
        /// Print packaging slips to PDF
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="shipments">Shipments</param>
        /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
        public virtual void PrintPackagingSlipsToPdf(Stream stream, IList<Shipment> shipments, int languageId = 0)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (shipments == null)
                throw new ArgumentNullException("shipments");

            var pageSize = _pdfSettings.LetterPageSizeEnabled ? PageSize.LETTER : PageSize.A4;

            var pdfWriter = new PdfWriter(stream);
            var pdfDoc = new PdfDocument(pdfWriter);
            pdfDoc.SetDefaultPageSize(pageSize);
            var doc = new Document(pdfDoc);

            var pdfFont = GetFont();

            int shipmentCount = shipments.Count;
            int shipmentNum = 0;

            foreach (var shipment in shipments)
            {
                var order = shipment.Order;

                var lang = _languageService.GetLanguageById(languageId == 0 ? order.CustomerLanguageId : languageId);
                if (lang == null || !lang.Published)
                    lang = _workContext.WorkingLanguage;

                doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFPackagingSlip.Shipment", lang.Id), shipment.Id)).SetFont(pdfFont).SetBold().SetFontSize(14));
                doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFPackagingSlip.Order", lang.Id), order.CustomOrderNumber)).SetFont(pdfFont).SetBold().SetFontSize(14));

                if (!order.PickUpInStore)
                {
                    if (order.ShippingAddress == null)
                        throw new NopException(string.Format("Shipping is required, but address is not available. Order ID = {0}", order.Id));

                    if (_addressSettings.CompanyEnabled && !String.IsNullOrEmpty(order.ShippingAddress.Company))
                        doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFPackagingSlip.Company", lang.Id), order.ShippingAddress.Company)).SetFont(pdfFont).SetFontSize(10));
                    doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFPackagingSlip.Name", lang.Id), order.ShippingAddress.FirstName + " " + order.ShippingAddress.LastName)).SetFont(pdfFont).SetFontSize(10));
                    if (_addressSettings.PhoneEnabled)
                        doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFPackagingSlip.Phone", lang.Id), order.ShippingAddress.PhoneNumber)).SetFont(pdfFont).SetFontSize(10));
                    if (_addressSettings.StreetAddressEnabled)
                        doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFPackagingSlip.Address", lang.Id), order.ShippingAddress.Address1)).SetFont(pdfFont).SetFontSize(10));
                    if (_addressSettings.StreetAddress2Enabled && !String.IsNullOrEmpty(order.ShippingAddress.Address2))
                        doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFPackagingSlip.Address2", lang.Id), order.ShippingAddress.Address2)).SetFont(pdfFont).SetFontSize(10));
                    if (_addressSettings.CityEnabled || _addressSettings.StateProvinceEnabled || _addressSettings.ZipPostalCodeEnabled)
                        doc.Add(new Paragraph(String.Format("{0}, {1} {2}", order.ShippingAddress.City, order.ShippingAddress.StateProvince != null ? order.ShippingAddress.StateProvince.GetLocalized(x => x.Name, lang.Id) : "", order.ShippingAddress.ZipPostalCode)).SetFont(pdfFont).SetFontSize(10));
                    if (_addressSettings.CountryEnabled && order.ShippingAddress.Country != null)
                        doc.Add(new Paragraph(order.ShippingAddress.Country.GetLocalized(x => x.Name, lang.Id)).SetFont(pdfFont).SetFontSize(10));
                    var customShippingAddressAttributes = _addressAttributeFormatter.FormatAttributes(order.ShippingAddress.CustomAttributes);
                    if (!String.IsNullOrEmpty(customShippingAddressAttributes))
                        doc.Add(new Paragraph(HtmlHelper.ConvertHtmlToPlainText(customShippingAddressAttributes, true, true)).SetFont(pdfFont).SetFontSize(10));
                }
                else if (order.PickupAddress != null)
                {
                    doc.Add(new Paragraph(_localizationService.GetResource("PDFInvoice.Pickup", lang.Id)).SetFont(pdfFont).SetBold().SetFontSize(10));
                    if (!string.IsNullOrEmpty(order.PickupAddress.Address1))
                        doc.Add(new Paragraph("   " + string.Format(_localizationService.GetResource("PDFInvoice.Address", lang.Id), order.PickupAddress.Address1)).SetFont(pdfFont).SetFontSize(10));
                    if (!string.IsNullOrEmpty(order.PickupAddress.City))
                        doc.Add(new Paragraph("   " + order.PickupAddress.City).SetFont(pdfFont).SetFontSize(10));
                    if (order.PickupAddress.Country != null)
                        doc.Add(new Paragraph("   " + order.PickupAddress.Country.GetLocalized(x => x.Name, lang.Id)).SetFont(pdfFont).SetFontSize(10));
                    if (!string.IsNullOrEmpty(order.PickupAddress.ZipPostalCode))
                        doc.Add(new Paragraph("   " + order.PickupAddress.ZipPostalCode).SetFont(pdfFont).SetFontSize(10));
                }

                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph(String.Format(_localizationService.GetResource("PDFPackagingSlip.ShippingMethod", lang.Id), order.ShippingMethod)).SetFont(pdfFont).SetFontSize(10));
                doc.Add(new Paragraph(" "));

                var productsTable = new Table(3).UseAllAvailableWidth();
                productsTable.AddHeaderCell(new Cell().Add(new Paragraph(_localizationService.GetResource("PDFPackagingSlip.ProductName", lang.Id)).SetFont(pdfFont).SetFontSize(9)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                productsTable.AddHeaderCell(new Cell().Add(new Paragraph(_localizationService.GetResource("PDFPackagingSlip.SKU", lang.Id)).SetFont(pdfFont).SetFontSize(9)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                productsTable.AddHeaderCell(new Cell().Add(new Paragraph(_localizationService.GetResource("PDFPackagingSlip.QTY", lang.Id)).SetFont(pdfFont).SetFontSize(9)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                foreach (var si in shipment.ShipmentItems)
                {
                    var orderItem = _orderService.GetOrderItemById(si.OrderItemId);
                    if (orderItem == null)
                        continue;

                    var p = orderItem.Product;
                    string name = p.GetLocalized(x => x.Name, lang.Id);
                    var nameCell = new Cell().Add(new Paragraph(name).SetFont(pdfFont).SetFontSize(9));
                    if (!String.IsNullOrEmpty(orderItem.AttributeDescription))
                        nameCell.Add(new Paragraph(HtmlHelper.ConvertHtmlToPlainText(orderItem.AttributeDescription, true, true)).SetFont(pdfFont).SetFontSize(8).SetItalic());
                    if (orderItem.Product.IsRental)
                    {
                        var rentalStartDate = orderItem.RentalStartDateUtc.HasValue ? orderItem.Product.FormatRentalDate(orderItem.RentalStartDateUtc.Value) : "";
                        var rentalEndDate = orderItem.RentalEndDateUtc.HasValue ? orderItem.Product.FormatRentalDate(orderItem.RentalEndDateUtc.Value) : "";
                        var rentalInfo = string.Format(_localizationService.GetResource("Order.Rental.FormattedDate"), rentalStartDate, rentalEndDate);
                        nameCell.Add(new Paragraph(rentalInfo).SetFont(pdfFont).SetFontSize(8).SetItalic());
                    }
                    productsTable.AddCell(nameCell);

                    var sku = p.FormatSku(orderItem.AttributesXml, _productAttributeParser);
                    productsTable.AddCell(new Cell().Add(new Paragraph(sku ?? String.Empty).SetFont(pdfFont).SetFontSize(9)));

                    productsTable.AddCell(new Cell().Add(new Paragraph(si.Quantity.ToString()).SetFont(pdfFont).SetFontSize(9)));
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

        /// <summary>
        /// Print products to PDF
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="products">Products</param>
        public virtual void PrintProductsToPdf(Stream stream, IList<Product> products)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (products == null)
                throw new ArgumentNullException("products");

            var lang = _workContext.WorkingLanguage;

            var pageSize = _pdfSettings.LetterPageSizeEnabled ? PageSize.LETTER : PageSize.A4;

            var pdfWriter = new PdfWriter(stream);
            var pdfDoc = new PdfDocument(pdfWriter);
            pdfDoc.SetDefaultPageSize(pageSize);
            var doc = new Document(pdfDoc);

            var pdfFont = GetFont();

            int productNumber = 1;
            int prodCount = products.Count;

            foreach (var product in products)
            {
                string productName = product.GetLocalized(x => x.Name, lang.Id);
                string productDescription = product.GetLocalized(x => x.FullDescription, lang.Id);

                doc.Add(new Paragraph(String.Format("{0}. {1}", productNumber, productName)).SetFont(pdfFont).SetBold().SetFontSize(14));
                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph(HtmlHelper.StripTags(HtmlHelper.ConvertHtmlToPlainText(productDescription, decode: true))).SetFont(pdfFont).SetFontSize(10));
                doc.Add(new Paragraph(" "));

                if (product.ProductType == ProductType.SimpleProduct)
                {
                    var priceStr = string.Format("{0} {1}", product.Price.ToString("0.00"), _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode);
                    if (product.IsRental)
                        priceStr = _priceFormatter.FormatRentalProductPeriod(product, priceStr);
                    doc.Add(new Paragraph(String.Format("{0}: {1}", _localizationService.GetResource("PDFProductCatalog.Price", lang.Id), priceStr)).SetFont(pdfFont).SetFontSize(10));
                    doc.Add(new Paragraph(String.Format("{0}: {1}", _localizationService.GetResource("PDFProductCatalog.SKU", lang.Id), product.Sku)).SetFont(pdfFont).SetFontSize(10));

                    if (product.IsShipEnabled && product.Weight > Decimal.Zero)
                        doc.Add(new Paragraph(String.Format("{0}: {1} {2}", _localizationService.GetResource("PDFProductCatalog.Weight", lang.Id), product.Weight.ToString("0.00"), _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId).Name)).SetFont(pdfFont).SetFontSize(10));

                    if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock)
                        doc.Add(new Paragraph(String.Format("{0}: {1}", _localizationService.GetResource("PDFProductCatalog.StockQuantity", lang.Id), product.GetTotalStockQuantity())).SetFont(pdfFont).SetFontSize(10));

                    doc.Add(new Paragraph(" "));
                }

                var pictures = _pictureService.GetPicturesByProductId(product.Id);
                if (pictures.Any())
                {
                    foreach (var pic in pictures)
                    {
                        var picBinary = _pictureService.LoadPictureBinary(pic);
                        if (picBinary != null && picBinary.Length > 0)
                        {
                            var pictureLocalPath = _pictureService.GetThumbLocalPath(pic, 200, false);
                            if (!string.IsNullOrEmpty(pictureLocalPath) && File.Exists(pictureLocalPath))
                            {
                                var imageData = ImageDataFactory.Create(pictureLocalPath);
                                var image = new iText.Layout.Element.Image(imageData).ScaleToFit(200f, 200f);
                                doc.Add(image);
                            }
                        }
                    }
                    doc.Add(new Paragraph(" "));
                }

                if (product.ProductType == ProductType.GroupedProduct)
                {
                    int pvNum = 1;
                    foreach (var associatedProduct in _productService.GetAssociatedProducts(product.Id, showHidden: true))
                    {
                        doc.Add(new Paragraph(String.Format("{0}-{1}. {2}", productNumber, pvNum, associatedProduct.GetLocalized(x => x.Name, lang.Id))).SetFont(pdfFont).SetFontSize(10));
                        doc.Add(new Paragraph(" "));
                        doc.Add(new Paragraph(String.Format("{0}: {1} {2}", _localizationService.GetResource("PDFProductCatalog.Price", lang.Id), associatedProduct.Price.ToString("0.00"), _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode)).SetFont(pdfFont).SetFontSize(10));
                        doc.Add(new Paragraph(String.Format("{0}: {1}", _localizationService.GetResource("PDFProductCatalog.SKU", lang.Id), associatedProduct.Sku)).SetFont(pdfFont).SetFontSize(10));

                        if (associatedProduct.IsShipEnabled && associatedProduct.Weight > Decimal.Zero)
                            doc.Add(new Paragraph(String.Format("{0}: {1} {2}", _localizationService.GetResource("PDFProductCatalog.Weight", lang.Id), associatedProduct.Weight.ToString("0.00"), _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId).Name)).SetFont(pdfFont).SetFontSize(10));

                        if (associatedProduct.ManageInventoryMethod == ManageInventoryMethod.ManageStock)
                            doc.Add(new Paragraph(String.Format("{0}: {1}", _localizationService.GetResource("PDFProductCatalog.StockQuantity", lang.Id), associatedProduct.GetTotalStockQuantity())).SetFont(pdfFont).SetFontSize(10));

                        doc.Add(new Paragraph(" "));
                        pvNum++;
                    }
                }

                productNumber++;

                if (productNumber <= prodCount)
                {
                    doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                }
            }

            doc.Close();
        }

        #endregion
    }
}
