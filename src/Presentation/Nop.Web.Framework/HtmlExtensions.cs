using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Net;
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
                {
                    localizationSupported = false;
                }
            }
            if (localizationSupported)
            {
                var tabStrip = new StringBuilder();
                tabStrip.AppendLine($"<div id=\"{name}\" class=\"nav-tabs-custom nav-tabs-localized-fields\">");
                tabStrip.AppendLine("<ul class=\"nav nav-tabs\">");

                tabStrip.AppendLine("<li class=\"active\">");
                tabStrip.AppendLine($"<a data-tab-name=\"{name}-standard-tab\" href=\"#{name}-standard-tab\" data-toggle=\"tab\">{EngineContext.Current.Resolve<ILocalizationService>().GetResource("Admin.Common.Standard")}</a>");
                tabStrip.AppendLine("</li>");

                var languageService = EngineContext.Current.Resolve<ILanguageService>();
                foreach (var locale in helper.ViewData.Model.Locales)
                {
                    var language = languageService.GetLanguageById(locale.LanguageId);
                    if (language == null)
                        throw new Exception("Language cannot be loaded");

                    tabStrip.AppendLine("<li>");
                    tabStrip.AppendLine($"<a data-tab-name=\"{name}-{language.Id}-tab\" href=\"#{name}-{language.Id}-tab\" data-toggle=\"tab\"><img alt='' src='/Content/images/flags/{language.FlagImageFileName}'>{WebUtility.HtmlEncode(language.Name)}</a>");
                    tabStrip.AppendLine("</li>");
                }
                tabStrip.AppendLine("</ul>");

                tabStrip.AppendLine("<div class=\"tab-content\">");
                tabStrip.AppendLine($"<div class=\"tab-pane active\" id=\"{name}-standard-tab\">");
                tabStrip.AppendLine(GetHtmlContentString(standardTemplate(helper.ViewData.Model)));
                tabStrip.AppendLine("</div>");

                for (int i = 0; i < helper.ViewData.Model.Locales.Count; i++)
                {
                    var language = languageService.GetLanguageById(helper.ViewData.Model.Locales[i].LanguageId);
                    tabStrip.AppendLine($"<div class=\"tab-pane\" id=\"{name}-{language.Id}-tab\">");
                    tabStrip.AppendLine(GetHtmlContentString(localizedTemplate(i)));
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

        public static IHtmlContent DeleteConfirmation<T>(this IHtmlHelper<T> helper, string actionName,
            string buttonsSelector) where T : BaseNopEntityModel
        {
            if (string.IsNullOrEmpty(actionName))
                actionName = "Delete";

            var modalId = helper.ViewData.ModelMetadata.ModelType.Name.ToLower() + "-delete-confirmation";

            var deleteConfirmationModel = new DeleteConfirmationModel
            {
                Id = helper.ViewData.Model.Id,
                ControllerName = helper.ViewContext.RouteData.Values["controller"]?.ToString(),
                ActionName = actionName,
                WindowId = modalId
            };

            var window = new StringBuilder();
            window.AppendLine($"<div id='{modalId}' class=\"modal fade\" tabindex=\"-1\" role=\"dialog\" aria-labelledby=\"{modalId}-title\">");
            window.AppendLine("<!-- Delete partial view would be rendered here -->");
            window.AppendLine("</div>");

            window.AppendLine("<script>");
            window.AppendLine("$(document).ready(function() {");
            window.AppendLine($"$('#{buttonsSelector}').attr(\"data-toggle\", \"modal\").attr(\"data-target\", \"#{modalId}\")");
            window.AppendLine("});");
            window.AppendLine("</script>");

            return new HtmlString(window.ToString());
        }

        public static IHtmlContent ActionConfirmation(this IHtmlHelper helper, string buttonId, string actionName = "")
        {
            if (string.IsNullOrEmpty(actionName))
                actionName = helper.ViewContext.RouteData.Values["action"]?.ToString();

            var modalId = buttonId + "-action-confirmation";

            var window = new StringBuilder();
            window.AppendLine($"<div id='{modalId}' class=\"modal fade\" tabindex=\"-1\" role=\"dialog\" aria-labelledby=\"{modalId}-title\">");
            window.AppendLine("<!-- Action confirmation partial view would be rendered here -->");
            window.AppendLine("</div>");

            window.AppendLine("<script>");
            window.AppendLine("$(document).ready(function() {");
            window.AppendLine($"$('#{buttonId}').attr(\"data-toggle\", \"modal\").attr(\"data-target\", \"#{modalId}\");");
            window.AppendLine($"$('#{modalId}-submit-button').attr(\"name\", $(\"#{buttonId}\").attr(\"name\"));");
            window.AppendLine($"$(\"#{buttonId}\").attr(\"name\", \"\")");
            window.AppendLine($"if($(\"#{buttonId}\").attr(\"type\") == \"submit\")$(\"#{buttonId}\").attr(\"type\", \"button\")");
            window.AppendLine("});");
            window.AppendLine("</script>");

            return new HtmlString(window.ToString());
        }

        /// <summary>
        /// Render CSS styles of selected index 
        /// </summary>
        public static IHtmlContent RenderBootstrapTabContent(this IHtmlHelper helper, string currentTabName,
            IHtmlContent content, bool isDefaultTab = false, string tabNameToSelect = "")
        {
            if (helper == null)
                throw new ArgumentNullException(nameof(helper));

            if (string.IsNullOrEmpty(tabNameToSelect))
                tabNameToSelect = helper.GetSelectedTabName();

            if (string.IsNullOrEmpty(tabNameToSelect) && isDefaultTab)
                tabNameToSelect = currentTabName;

            var cssClass = $"tab-pane{(tabNameToSelect == currentTabName ? " active" : "")}";
            var result = $"<div class=\"{cssClass}\" id=\"{currentTabName}\">{GetHtmlContentString(content)}</div>";

            return new HtmlString(result);
        }

        /// <summary>
        /// Render CSS styles of selected index 
        /// </summary>
        public static IHtmlContent RenderBootstrapTabHeader(this IHtmlHelper helper, string currentTabName,
            LocalizedString title, bool isDefaultTab = false, string tabNameToSelect = "", string customCssClass = "")
        {
            if (helper == null)
                throw new ArgumentNullException(nameof(helper));

            if (string.IsNullOrEmpty(tabNameToSelect))
                tabNameToSelect = helper.GetSelectedTabName();

            if (string.IsNullOrEmpty(tabNameToSelect) && isDefaultTab)
                tabNameToSelect = currentTabName;

            var a = $"<a data-tab-name=\"{currentTabName}\" href=\"#{currentTabName}\" data-toggle=\"tab\">{title.Text}</a>";

            var liClassValue = "";
            if (tabNameToSelect == currentTabName)
                liClassValue = "active";
            if (!string.IsNullOrEmpty(customCssClass))
            {
                if (!string.IsNullOrEmpty(liClassValue))
                    liClassValue += " ";
                liClassValue += customCssClass;
            }

            var li = $"<li class=\"{liClassValue}\">{a}</li>";
            return new HtmlString(li);
        }

        /// <summary>
        /// Gets a selected tab name (used in admin area to store selected tab name)
        /// </summary>
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

        #region Form fields

        public static IHtmlContent Hint(this IHtmlHelper helper, string value)
        {
            var result = $"<div title=\"{WebUtility.HtmlEncode(value)}\" class=\"ico-help\"><i class='fa fa-question-circle'></i></div>";
            return new HtmlString(result);
        }

        public static IHtmlContent RequiredHint(this IHtmlHelper helper, string additionalText = null)
        {
            var innerText = "*";
            if (!string.IsNullOrEmpty(additionalText))
                innerText += " " + additionalText;
            return new HtmlString($"<span class=\"required\">{WebUtility.HtmlEncode(innerText)}</span>");
        }

        /// <summary>
        /// Creates a days, months, years drop down list using an HTML select control. 
        /// </summary>
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
            else
            {
                dayLocale = "Day";
                monthLocale = "Month";
                yearLocale = "Year";
            }

            days.AppendFormat("<option value='{0}'>{1}</option>", "0", dayLocale);
            for (int i = 1; i <= 31; i++)
                days.AppendFormat("<option value='{0}'{1}>{0}</option>", i,
                    (selectedDay.HasValue && selectedDay.Value == i) ? " selected=\"selected\"" : null);

            months.AppendFormat("<option value='{0}'>{1}</option>", "0", monthLocale);
            for (int i = 1; i <= 12; i++)
            {
                months.AppendFormat("<option value='{0}'{1}>{2}</option>",
                    i,
                    (selectedMonth.HasValue && selectedMonth.Value == i) ? " selected=\"selected\"" : null,
                    CultureInfo.CurrentUICulture.DateTimeFormat.GetMonthName(i));
            }

            years.AppendFormat("<option value='{0}'>{1}</option>", "0", yearLocale);
            if (beginYear == null)
                beginYear = DateTime.UtcNow.Year - 100;
            if (endYear == null)
                endYear = DateTime.UtcNow.Year;

            if (endYear > beginYear)
            {
                for (int i = beginYear.Value; i <= endYear.Value; i++)
                    years.AppendFormat("<option value='{0}'{1}>{0}</option>", i,
                        (selectedYear.HasValue && selectedYear.Value == i) ? " selected=\"selected\"" : null);
            }
            else
            {
                for (int i = beginYear.Value; i >= endYear.Value; i--)
                    years.AppendFormat("<option value='{0}'{1}>{0}</option>", i,
                        (selectedYear.HasValue && selectedYear.Value == i) ? " selected=\"selected\"" : null);
            }

            var daysHtml = wrapTags ? $"<span class=\"days-list-wrapper\"><select name=\"{dayName}\">{days}</select></span>"
                : $"<select name=\"{dayName}\">{days}</select>";
            var monthsHtml = wrapTags ? $"<span class=\"months-list-wrapper\"><select name=\"{monthName}\">{months}</select></span>"
                : $"<select name=\"{monthName}\">{months}</select>";
            var yearsHtml = wrapTags ? $"<span class=\"years-list-wrapper\"><select name=\"{yearName}\">{years}</select></span>"
                : $"<select name=\"{yearName}\">{years}</select>";

            return new HtmlString(daysHtml + monthsHtml + yearsHtml);
        }

        #endregion

        #endregion

        #region Helper methods

        private static string GetHtmlContentString(IHtmlContent content)
        {
            if (content == null)
                return string.Empty;

            using (var writer = new System.IO.StringWriter())
            {
                content.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
                return writer.ToString();
            }
        }

        #endregion
    }
}
