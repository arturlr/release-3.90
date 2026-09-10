using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Nop.Core.Domain.Catalog;
using Nop.Services.Common;

namespace Nop.Web.Extensions
{
    /// <summary>
    /// Parser helper
    /// </summary>
    /// <remarks>
    /// Task 7.3: <c>System.Web.Mvc.FormCollection</c> became
    /// <see cref="IFormCollection"/> — the interface, not the concrete
    /// <c>Microsoft.AspNetCore.Http.FormCollection</c>, because <c>HttpRequest.Form</c> is
    /// typed as the interface (the same choice task 6.2 made for
    /// <c>BasePaymentController.ValidatePaymentForm</c>).
    ///
    /// The indexer now yields <c>StringValues</c> rather than <c>string</c>, so the locals are
    /// declared <c>string</c> explicitly to force the implicit conversion. That conversion is
    /// behaviourally identical to 3.90: a single value is returned as-is and multiple values
    /// are joined with <c>","</c>, exactly as <c>NameValueCollection</c>'s indexer did — which
    /// is what the <c>Checkboxes</c> branch's <c>Split(',')</c> depends on. Left as <c>var</c>
    /// the file would not compile (<c>StringValues</c> has no <c>Split</c>/<c>Trim</c>).
    /// </remarks>
    public static class AttributeParserHelper
    {
        public static string ParseCustomAddressAttributes(this IFormCollection form,
            IAddressAttributeParser addressAttributeParser,
            IAddressAttributeService addressAttributeService)
        {
            if (form == null)
                throw new ArgumentNullException("form");

            string attributesXml = "";
            var attributes = addressAttributeService.GetAllAddressAttributes();
            foreach (var attribute in attributes)
            {
                string controlId = string.Format("address_attribute_{0}", attribute.Id);
                switch (attribute.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                        {
                            string ctrlAttributes = form[controlId];
                            if (!String.IsNullOrEmpty(ctrlAttributes))
                            {
                                int selectedAttributeId = int.Parse(ctrlAttributes);
                                if (selectedAttributeId > 0)
                                    attributesXml = addressAttributeParser.AddAddressAttribute(attributesXml,
                                        attribute, selectedAttributeId.ToString());
                            }
                        }
                        break;
                    case AttributeControlType.Checkboxes:
                        {
                            string cblAttributes = form[controlId];
                            if (!String.IsNullOrEmpty(cblAttributes))
                            {
                                foreach (var item in cblAttributes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    int selectedAttributeId = int.Parse(item);
                                    if (selectedAttributeId > 0)
                                        attributesXml = addressAttributeParser.AddAddressAttribute(attributesXml,
                                            attribute, selectedAttributeId.ToString());
                                }
                            }
                        }
                        break;
                    case AttributeControlType.ReadonlyCheckboxes:
                        {
                            //load read-only (already server-side selected) values
                            var attributeValues = addressAttributeService.GetAddressAttributeValues(attribute.Id);
                            foreach (var selectedAttributeId in attributeValues
                                .Where(v => v.IsPreSelected)
                                .Select(v => v.Id)
                                .ToList())
                            {
                                attributesXml = addressAttributeParser.AddAddressAttribute(attributesXml,
                                            attribute, selectedAttributeId.ToString());
                            }
                        }
                        break;
                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                        {
                            string ctrlAttributes = form[controlId];
                            if (!String.IsNullOrEmpty(ctrlAttributes))
                            {
                                string enteredText = ctrlAttributes.Trim();
                                attributesXml = addressAttributeParser.AddAddressAttribute(attributesXml,
                                    attribute, enteredText);
                            }
                        }
                        break;
                    case AttributeControlType.Datepicker:
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.ImageSquares:
                    case AttributeControlType.FileUpload:
                    //not supported address attributes
                    default:
                        break;
                }
            }

            return attributesXml;
        }
    }
}

