using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;
using Nop.Services.Stores;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Framework
{
    public static class HtmlExtensions
    {
        #region Admin area extensions

        public static IHtmlContent LocalizedEditor<T, TLocalizedModelLocal>(this IHtmlHelper<T> helper,
            string name,
            Func<int, IHtmlContent> localizedTemplate,
            Func<T, IHtmlContent> standardTemplate,
            bool ignoreIfSeveralStores = false)
            where T : ILocalizedModel<TLocalizedModelLocal>
            where TLocalizedModelLocal : ILocalizedModelLocal
        {
            var localizationSupported = helper.ViewData.Model.Locales.Count > 1;
            if (ignoreIfSeveralStores)
            {
                var storeService = EngineContext.Current.Resolve<IStoreService>();
                if (storeService.GetAllStores().Count >= 2)
                    localizationSupported = false;
            }
            if (localizationSupported)
            {
                var tabStrip = new StringBuilder();
                tabStrip.AppendLine(string.Format("<div id=\"{0}\" class=\"nav-tabs-custom nav-tabs-localized-fields\">", name));
                tabStrip.AppendLine("<ul class=\"nav nav-tabs\">");
                tabStrip.AppendLine("<li class=\"active\">");
                tabStrip.AppendLine(string.Format("<a data-tab-name=\"{0}-{1}-tab\" href=\"#{0}-{1}-tab\" data-toggle=\"tab\">{2}</a>",
                    name, "standard", EngineContext.Current.Resolve<ILocalizationService>().GetResource("Admin.Common.Standard")));
                tabStrip.AppendLine("</li>");

                var languageService = EngineContext.Current.Resolve<ILanguageService>();
                foreach (var locale in helper.ViewData.Model.Locales)
                {
                    var language = languageService.GetLanguageById(locale.LanguageId);
                    if (language == null) throw new Exception("Language cannot be loaded");
                    tabStrip.AppendLine("<li>");
                    tabStrip.AppendLine(string.Format("<a data-tab-name=\"{0}-{1}-tab\" href=\"#{0}-{1}-tab\" data-toggle=\"tab\">{2}</a>",
                        name, language.Id, WebUtility.HtmlEncode(language.Name)));
                    tabStrip.AppendLine("</li>");
                }
                tabStrip.AppendLine("</ul>");
                tabStrip.AppendLine("<div class=\"tab-content\">");
                tabStrip.AppendLine(string.Format("<div class=\"tab-pane active\" id=\"{0}-{1}-tab\">", name, "standard"));
                using (var sw = new StringWriter()) { standardTemplate(helper.ViewData.Model).WriteTo(sw, System.Text.Encodings.Web.HtmlEncoder.Default); tabStrip.AppendLine(sw.ToString()); }
                tabStrip.AppendLine("</div>");

                for (int i = 0; i < helper.ViewData.Model.Locales.Count; i++)
                {
                    var language = languageService.GetLanguageById(helper.ViewData.Model.Locales[i].LanguageId);
                    tabStrip.AppendLine(string.Format("<div class=\"tab-pane\" id=\"{0}-{1}-tab\">", name, language.Id));
                    using (var sw = new StringWriter()) { localizedTemplate(i).WriteTo(sw, System.Text.Encodings.Web.HtmlEncoder.Default); tabStrip.AppendLine(sw.ToString()); }
                    tabStrip.AppendLine("</div>");
                }
                tabStrip.AppendLine("</div>");
                tabStrip.AppendLine("</div>");
                return new HtmlString(tabStrip.ToString());
            }
            else
            {
                return standardTemplate(helper.ViewData.Model);
            }
        }

        public static IHtmlContent DeleteConfirmation<T>(this IHtmlHelper<T> helper, string buttonsSelector) where T : BaseNopEntityModel
        {
            return DeleteConfirmation(helper, "", buttonsSelector);
        }

        public static IHtmlContent DeleteConfirmation<T>(this IHtmlHelper<T> helper, string actionName, string buttonsSelector) where T : BaseNopEntityModel
        {
            if (String.IsNullOrEmpty(actionName))
                actionName = "Delete";

            var modelName = helper.ViewData.ModelMetadata.ModelType.Name.ToLower();
            var modalId = modelName + "-delete-confirmation";

            var window = new StringBuilder();
            window.AppendLine(string.Format("<div id='{0}' class=\"modal fade\" tabindex=\"-1\" role=\"dialog\" aria-labelledby=\"{0}-title\">", modalId));
            window.AppendLine("</div>");
            window.AppendLine("<script>");
            window.AppendLine("$(document).ready(function() {");
            window.AppendLine(string.Format("$('#{0}').attr(\"data-toggle\", \"modal\").attr(\"data-target\", \"#{1}\")", buttonsSelector, modalId));
            window.AppendLine("});");
            window.AppendLine("</script>");
            return new HtmlString(window.ToString());
        }

        public static IHtmlContent ActionConfirmation(this IHtmlHelper helper, string buttonId, string actionName = "")
        {
            if (string.IsNullOrEmpty(actionName))
                actionName = helper.ViewContext.RouteData.Values["action"]?.ToString() ?? "";

            var modalId = buttonId + "-action-confirmation";
            var window = new StringBuilder();
            window.AppendLine(string.Format("<div id='{0}' class=\"modal fade\" tabindex=\"-1\" role=\"dialog\" aria-labelledby=\"{0}-title\">", modalId));
            window.AppendLine("</div>");
            window.AppendLine("<script>");
            window.AppendLine("$(document).ready(function() {");
            window.AppendLine(string.Format("$('#{0}').attr(\"data-toggle\", \"modal\").attr(\"data-target\", \"#{1}\");", buttonId, modalId));
            window.AppendLine(string.Format("$('#{0}-submit-button').attr(\"name\", $(\"#{1}\").attr(\"name\"));", modalId, buttonId));
            window.AppendLine(string.Format("$(\"#{0}\").attr(\"name\", \"\")", buttonId));
            window.AppendLine(string.Format("if($(\"#{0}\").attr(\"type\") == \"submit\")$(\"#{0}\").attr(\"type\", \"button\")", buttonId));
            window.AppendLine("});");
            window.AppendLine("</script>");
            return new HtmlString(window.ToString());
        }

        public static IHtmlContent Hint(this IHtmlHelper helper, string value)
        {
            var builder = new TagBuilder("div");
            builder.MergeAttribute("title", value);
            builder.MergeAttribute("class", "ico-help");
            builder.InnerHtml.AppendHtml("<i class='fa fa-question-circle'></i>");
            using (var sw = new StringWriter()) { builder.WriteTo(sw, System.Text.Encodings.Web.HtmlEncoder.Default); return new HtmlString(sw.ToString()); }
        }

        public static string GetSelectedTabName(this IHtmlHelper helper)
        {
            var tabName = string.Empty;
            const string dataKey = "nop.selected-tab-name";
            if (helper.ViewData.ContainsKey(dataKey))
                tabName = helper.ViewData[dataKey].ToString();
            if (helper.ViewContext.TempData.ContainsKey(dataKey))
                tabName = helper.ViewContext.TempData[dataKey].ToString();
            return tabName;
        }

        #endregion

        #region Common extensions

        public static IHtmlContent RequiredHint(this IHtmlHelper helper, string additionalText = null)
        {
            var builder = new TagBuilder("span");
            builder.AddCssClass("required");
            var innerText = "*";
            if (!String.IsNullOrEmpty(additionalText))
                innerText += " " + additionalText;
            builder.InnerHtml.Append(innerText);
            using (var sw = new StringWriter()) { builder.WriteTo(sw, System.Text.Encodings.Web.HtmlEncoder.Default); return new HtmlString(sw.ToString()); }
        }

        public static string FieldNameFor<T, TResult>(this IHtmlHelper<T> html, Expression<Func<T, TResult>> expression)
        {
            var expressionProvider = html.ViewContext.HttpContext.RequestServices.GetService(typeof(ModelExpressionProvider)) as ModelExpressionProvider;
            var modelExpression = expressionProvider.CreateModelExpression(html.ViewData, expression);
            return html.ViewData.TemplateInfo.GetFullHtmlFieldName(modelExpression.Name);
        }

        public static string FieldIdFor<T, TResult>(this IHtmlHelper<T> html, Expression<Func<T, TResult>> expression)
        {
            var expressionProvider = html.ViewContext.HttpContext.RequestServices.GetService(typeof(ModelExpressionProvider)) as ModelExpressionProvider;
            var modelExpression = expressionProvider.CreateModelExpression(html.ViewData, expression);
            var id = html.ViewData.TemplateInfo.GetFullHtmlFieldName(modelExpression.Name);
            return id.Replace('.', '_').Replace('[', '_').Replace(']', '_');
        }

        public static IHtmlContent DatePickerDropDowns(this IHtmlHelper html,
            string dayName, string monthName, string yearName,
            int? beginYear = null, int? endYear = null,
            int? selectedDay = null, int? selectedMonth = null, int? selectedYear = null,
            bool localizeLabels = true, object htmlAttributes = null, bool wrapTags = false)
        {
            var days = new StringBuilder();
            var months = new StringBuilder();
            var years = new StringBuilder();

            string dayLocale, monthLocale, yearLocale;
            if (localizeLabels)
            {
                var locService = EngineContext.Current.Resolve<ILocalizationService>();
                dayLocale = locService.GetResource("Common.Day");
                monthLocale = locService.GetResource("Common.Month");
                yearLocale = locService.GetResource("Common.Year");
            }
            else { dayLocale = "Day"; monthLocale = "Month"; yearLocale = "Year"; }

            days.AppendFormat("<option value='{0}'>{1}</option>", "0", dayLocale);
            for (int i = 1; i <= 31; i++)
                days.AppendFormat("<option value='{0}'{1}>{0}</option>", i, (selectedDay.HasValue && selectedDay.Value == i) ? " selected=\"selected\"" : null);

            months.AppendFormat("<option value='{0}'>{1}</option>", "0", monthLocale);
            for (int i = 1; i <= 12; i++)
                months.AppendFormat("<option value='{0}'{1}>{2}</option>", i, (selectedMonth.HasValue && selectedMonth.Value == i) ? " selected=\"selected\"" : null, CultureInfo.CurrentUICulture.DateTimeFormat.GetMonthName(i));

            years.AppendFormat("<option value='{0}'>{1}</option>", "0", yearLocale);
            if (beginYear == null) beginYear = DateTime.UtcNow.Year - 100;
            if (endYear == null) endYear = DateTime.UtcNow.Year;

            if (endYear > beginYear)
                for (int i = beginYear.Value; i <= endYear.Value; i++)
                    years.AppendFormat("<option value='{0}'{1}>{0}</option>", i, (selectedYear.HasValue && selectedYear.Value == i) ? " selected=\"selected\"" : null);
            else
                for (int i = beginYear.Value; i >= endYear.Value; i--)
                    years.AppendFormat("<option value='{0}'{1}>{0}</option>", i, (selectedYear.HasValue && selectedYear.Value == i) ? " selected=\"selected\"" : null);

            var daysList = string.Format("<select name=\"{0}\">{1}</select>", dayName, days);
            var monthsList = string.Format("<select name=\"{0}\">{1}</select>", monthName, months);
            var yearsList = string.Format("<select name=\"{0}\">{1}</select>", yearName, years);

            if (wrapTags)
            {
                daysList = "<span class=\"days-list select-wrapper\">" + daysList + "</span>";
                monthsList = "<span class=\"months-list select-wrapper\">" + monthsList + "</span>";
                yearsList = "<span class=\"years-list select-wrapper\">" + yearsList + "</span>";
            }

            return new HtmlString(string.Concat(daysList, monthsList, yearsList));
        }

        #endregion
    }
}
