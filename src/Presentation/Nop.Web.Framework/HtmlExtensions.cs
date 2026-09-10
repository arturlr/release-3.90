using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;
using Nop.Services.Stores;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Framework
{
    /// <summary>
    /// HTML helper extensions.
    /// </summary>
    /// <remarks>
    /// Ported in task 6.3 (Requirements 4.1, 4.2, 4.4). The mechanical part of the port:
    /// <c>System.Web.Mvc.HtmlHelper</c>/<c>HtmlHelper&lt;T&gt;</c> → <see cref="IHtmlHelper"/>/
    /// <see cref="IHtmlHelper{TModel}"/>, <c>MvcHtmlString</c> → <see cref="IHtmlContent"/>
    /// (concretely <see cref="HtmlString"/>), <c>UrlHelper</c> → <see cref="IUrlHelper"/>.
    ///
    /// The non-mechanical parts are called out inline; the four that change behaviour or shape are
    /// <see cref="AddFormControlClassToHtmlAttributes"/> (a latent
    /// <see cref="KeyNotFoundException"/> introduced by the platform), <see cref="Widget"/> (child
    /// action → view component), <see cref="FieldIdFor{T,TResult}"/> and
    /// <see cref="LabelFor{TModel,TValue}"/> (no <c>GetFullHtmlFieldId</c> in ASP.NET Core).
    /// </remarks>
    public static class HtmlExtensions
    {
        #region Utilities

        /// <summary>
        /// Read a route value as a string.
        /// </summary>
        /// <remarks>
        /// Replaces MVC 5's <c>RouteData.GetRequiredString(key)</c>, which does not exist in
        /// ASP.NET Core. ASP.NET Core route values are always strings (see task 6.2's note on
        /// <c>StoreClosedAttribute</c>), so a plain <c>ToString()</c> is faithful. The "required"
        /// part is preserved: a missing value throws rather than silently yielding null.
        /// </remarks>
        private static string GetRequiredRouteValue(ViewContext viewContext, string key)
        {
            var value = viewContext?.RouteData?.Values[key]?.ToString();
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException($"The route value '{key}' is required but was not present.");

            return value;
        }

        /// <summary>
        /// Build a <see cref="ModelExpression"/> for a lambda expression.
        /// </summary>
        /// <remarks>
        /// Replaces <c>ModelMetadata.FromLambdaExpression(expression, viewData)</c>, which has no
        /// ASP.NET Core counterpart — the equivalent machinery
        /// (<c>ExpressionMetadataProvider</c>) is <c>internal</c>. The public seam is
        /// <see cref="IModelExpressionProvider"/>, which yields both the metadata and the fully
        /// qualified field name. A directly constructed <see cref="ModelExpressionProvider"/> is
        /// used as a fallback so these helpers keep working outside a fully wired request scope
        /// (for example under a unit test that supplies only a metadata provider).
        /// </remarks>
        private static ModelExpression GetModelExpression<TModel, TValue>(IHtmlHelper<TModel> helper,
            Expression<Func<TModel, TValue>> expression)
        {
            var provider = helper.ViewContext?.HttpContext?.RequestServices?.GetService<IModelExpressionProvider>()
                           ?? new ModelExpressionProvider(helper.MetadataProvider);

            return provider.CreateModelExpression(helper.ViewData, expression);
        }

        private static IUrlHelper GetUrlHelper(IHtmlHelper helper)
        {
            return helper.ViewContext.HttpContext.RequestServices
                .GetRequiredService<IUrlHelperFactory>()
                .GetUrlHelper(helper.ViewContext);
        }

        #endregion

        #region Admin area extensions

        public static HelperResult LocalizedEditor<T, TLocalizedModelLocal>(this IHtmlHelper<T> helper,
            string name,
            Func<int, HelperResult> localizedTemplate,
            Func<T, HelperResult> standardTemplate,
            bool ignoreIfSeveralStores = false)
            where T : ILocalizedModel<TLocalizedModelLocal>
            where TLocalizedModelLocal : ILocalizedModelLocal
        {
            //NOTE: ASP.NET Core's HelperResult takes a Func<TextWriter, Task>, not MVC 5's
            //Action<TextWriter>. The body is synchronous, so it completes eagerly.
            return new HelperResult(writer =>
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
                    tabStrip.AppendLine(string.Format("<div id=\"{0}\" class=\"nav-tabs-custom nav-tabs-localized-fields\">", name));
                    tabStrip.AppendLine("<ul class=\"nav nav-tabs\">");

                    //default tab
                    tabStrip.AppendLine("<li class=\"active\">");
                    tabStrip.AppendLine(string.Format("<a data-tab-name=\"{0}-{1}-tab\" href=\"#{0}-{1}-tab\" data-toggle=\"tab\">{2}</a>",
                            name, 
                            "standard",
                            EngineContext.Current.Resolve<ILocalizationService>().GetResource("Admin.Common.Standard")));
                    tabStrip.AppendLine("</li>");

                    var languageService = EngineContext.Current.Resolve<ILanguageService>();
                    var urlHelper = GetUrlHelper(helper);
                    foreach (var locale in helper.ViewData.Model.Locales)
                    {
                        //languages
                        var language = languageService.GetLanguageById(locale.LanguageId);
                        if (language == null)
                            throw new Exception("Language cannot be loaded");

                        tabStrip.AppendLine("<li>");
                        var iconUrl = urlHelper.Content("~/Content/images/flags/" + language.FlagImageFileName);
                        tabStrip.AppendLine(string.Format("<a data-tab-name=\"{0}-{1}-tab\" href=\"#{0}-{1}-tab\" data-toggle=\"tab\"><img alt='' src='{2}'>{3}</a>",
                                name, 
                                language.Id,
                                iconUrl,
                                //System.Web.HttpUtility -> System.Net.WebUtility, the same swap made
                                //across Nop.Core, Nop.Services and task 6.2. HtmlEncode is identical.
                                WebUtility.HtmlEncode(language.Name)));

                        tabStrip.AppendLine("</li>");
                    }
                    tabStrip.AppendLine("</ul>");
                    
                    //default tab
                    tabStrip.AppendLine("<div class=\"tab-content\">");
                    tabStrip.AppendLine(string.Format("<div class=\"tab-pane active\" id=\"{0}-{1}-tab\">", name, "standard"));
                    tabStrip.AppendLine(standardTemplate(helper.ViewData.Model).ToHtmlString());
                    tabStrip.AppendLine("</div>");

                    for (int i = 0; i < helper.ViewData.Model.Locales.Count; i++)
                    {
                        //languages
                        var language = languageService.GetLanguageById(helper.ViewData.Model.Locales[i].LanguageId);

                        tabStrip.AppendLine(string.Format("<div class=\"tab-pane\" id=\"{0}-{1}-tab\">",
                            name,
                            language.Id));
                        tabStrip.AppendLine(localizedTemplate(i).ToHtmlString());
                        tabStrip.AppendLine("</div>");
                    }
                    tabStrip.AppendLine("</div>");
                    tabStrip.AppendLine("</div>");

                    //TextWriter.Write(string) emits raw markup, which is what MVC 5's
                    //Write(MvcHtmlString) did
                    writer.Write(tabStrip.ToString());
                }
                else
                {
                    standardTemplate(helper.ViewData.Model).WriteTo(writer, HtmlEncoder.Default);
                }

                return Task.CompletedTask;
            });
        }

        public static IHtmlContent DeleteConfirmation<T>(this IHtmlHelper<T> helper, string buttonsSelector) where T : BaseNopEntityModel
        {
            return DeleteConfirmation(helper, "", buttonsSelector);
        }

        public static IHtmlContent DeleteConfirmation<T>(this IHtmlHelper<T> helper, string actionName,
            string buttonsSelector) where T : BaseNopEntityModel
        {
            if (String.IsNullOrEmpty(actionName))
                actionName = "Delete";

            var modalId = helper.ViewData.ModelMetadata.ModelType.Name.ToLower() + "-delete-confirmation";

            var deleteConfirmationModel = new DeleteConfirmationModel
            {
                Id = helper.ViewData.Model.Id,
                ControllerName = GetRequiredRouteValue(helper.ViewContext, "controller"),
                ActionName = actionName,
                WindowId = modalId
            };

            var window = new StringBuilder();
            window.AppendLine(string.Format("<div id='{0}' class=\"modal fade\"  tabindex=\"-1\" role=\"dialog\" aria-labelledby=\"{0}-title\">", modalId));
            window.AppendLine(helper.Partial("Delete", deleteConfirmationModel).ToHtmlString());
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
                actionName = GetRequiredRouteValue(helper.ViewContext, "action");

            var modalId = buttonId + "-action-confirmation";

            var actionConfirmationModel = new ActionConfirmationModel()
            {
                ControllerName = GetRequiredRouteValue(helper.ViewContext, "controller"),
                ActionName = actionName,
                WindowId = modalId
            };

            var window = new StringBuilder();
            window.AppendLine(string.Format("<div id='{0}' class=\"modal fade\"  tabindex=\"-1\" role=\"dialog\" aria-labelledby=\"{0}-title\">", modalId));
            window.AppendLine(helper.Partial("Confirm", actionConfirmationModel).ToHtmlString());
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

        public static IHtmlContent OverrideStoreCheckboxFor<TModel, TValue>(this IHtmlHelper<TModel> helper,
            Expression<Func<TModel, bool>> expression,
            Expression<Func<TModel, TValue>> forInputExpression,
            int activeStoreScopeConfiguration)
        {
            var dataInputIds = new List<string>();
            dataInputIds.Add(helper.FieldIdFor(forInputExpression));
            return OverrideStoreCheckboxFor(helper, expression, activeStoreScopeConfiguration, null, dataInputIds.ToArray());
        }
        public static IHtmlContent OverrideStoreCheckboxFor<TModel, TValue1, TValue2>(this IHtmlHelper<TModel> helper,
            Expression<Func<TModel, bool>> expression,
            Expression<Func<TModel, TValue1>> forInputExpression1,
            Expression<Func<TModel, TValue2>> forInputExpression2,
            int activeStoreScopeConfiguration)
        {
            var dataInputIds = new List<string>();
            dataInputIds.Add(helper.FieldIdFor(forInputExpression1));
            dataInputIds.Add(helper.FieldIdFor(forInputExpression2));
            return OverrideStoreCheckboxFor(helper, expression, activeStoreScopeConfiguration, null, dataInputIds.ToArray());
        }
        public static IHtmlContent OverrideStoreCheckboxFor<TModel, TValue1, TValue2, TValue3>(this IHtmlHelper<TModel> helper,
            Expression<Func<TModel, bool>> expression,
            Expression<Func<TModel, TValue1>> forInputExpression1,
            Expression<Func<TModel, TValue2>> forInputExpression2,
            Expression<Func<TModel, TValue3>> forInputExpression3,
            int activeStoreScopeConfiguration)
        {
            var dataInputIds = new List<string>();
            dataInputIds.Add(helper.FieldIdFor(forInputExpression1));
            dataInputIds.Add(helper.FieldIdFor(forInputExpression2));
            dataInputIds.Add(helper.FieldIdFor(forInputExpression3));
            return OverrideStoreCheckboxFor(helper, expression, activeStoreScopeConfiguration, null, dataInputIds.ToArray());
        }
        public static IHtmlContent OverrideStoreCheckboxFor<TModel>(this IHtmlHelper<TModel> helper,
            Expression<Func<TModel, bool>> expression,
            string parentContainer,
            int activeStoreScopeConfiguration)
        {
            return OverrideStoreCheckboxFor(helper, expression, activeStoreScopeConfiguration, parentContainer);
        }
        private static IHtmlContent OverrideStoreCheckboxFor<TModel>(this IHtmlHelper<TModel> helper,
            Expression<Func<TModel, bool>> expression,
            int activeStoreScopeConfiguration,
            string parentContainer = null,
            params string[] datainputIds)
        {
            if (String.IsNullOrEmpty(parentContainer) && datainputIds == null)
                throw new ArgumentException("Specify at least one selector");

            var result = new StringBuilder();
            if (activeStoreScopeConfiguration > 0)
            {
                //render only when a certain store is chosen
                const string cssClass = "multi-store-override-option";
                string dataInputSelector = "";
                if (!String.IsNullOrEmpty(parentContainer))
                {
                    dataInputSelector = "#" + parentContainer + " input, #" + parentContainer + " textarea, #" + parentContainer + " select";
                }
                if (datainputIds != null && datainputIds.Length > 0)
                {
                    dataInputSelector = "#" + String.Join(", #", datainputIds);
                }
                var onClick = string.Format("checkOverriddenStoreValue(this, '{0}')", dataInputSelector);
                //NOTE: CheckBoxFor now returns IHtmlContent, whose ToString() is NOT the markup.
                //ToHtmlString() renders it (see HtmlContentExtensions).
                result.Append(helper.CheckBoxFor(expression, new Dictionary<string, object>
                {
                    { "class", cssClass },
                    { "onclick", onClick },
                    { "data-for-input-selector", dataInputSelector },
                }).ToHtmlString());
            }
            return new HtmlString(result.ToString());
        }
        
        /// <summary>
        /// Render CSS styles of selected index 
        /// </summary>
        /// <param name="helper">HTML helper</param>
        /// <param name="currentTabName">Current tab name (where appropriate CSS style should be rendred)</param>
        /// <param name="content">Tab content</param>
        /// <param name="isDefaultTab">Indicates that the tab is default</param>
        /// <param name="tabNameToSelect">Tab name to select</param>
        /// <returns>Tab content markup</returns>
        public static IHtmlContent RenderBootstrapTabContent(this IHtmlHelper helper, string currentTabName,
            HelperResult content, bool isDefaultTab = false, string tabNameToSelect = "")
        {
            if (helper == null)
                throw new ArgumentNullException("helper");

            if (string.IsNullOrEmpty(tabNameToSelect))
                tabNameToSelect = helper.GetSelectedTabName();

            if (string.IsNullOrEmpty(tabNameToSelect) && isDefaultTab)
                tabNameToSelect = currentTabName;

            //NOTE: ASP.NET Core's TagBuilder has no settable InnerHtml string and no collection
            //initializer over Attributes taking KeyValuePairs - InnerHtml is an
            //IHtmlContentBuilder and Attributes is an AttributeDictionary.
            var tag = new TagBuilder("div");
            tag.Attributes.Add("class", string.Format("tab-pane{0}", tabNameToSelect == currentTabName ? " active" : ""));
            tag.Attributes.Add("id", string.Format("{0}", currentTabName));
            tag.InnerHtml.AppendHtml(content);

            return new HtmlString(tag.ToHtmlString(TagRenderMode.Normal));
        }

        /// <summary>
        /// Render CSS styles of selected index 
        /// </summary>
        /// <param name="helper">HTML helper</param>
        /// <param name="currentTabName">Current tab name (where appropriate CSS style should be rendred)</param>
        /// <param name="title">Tab title</param>
        /// <param name="isDefaultTab">Indicates that the tab is default</param>
        /// <param name="tabNameToSelect">Tab name to select</param>
        /// <param name="customCssClass">Tab name to select</param>
        /// <returns>Tab header markup</returns>
        public static IHtmlContent RenderBootstrapTabHeader(this IHtmlHelper helper, string currentTabName,
            LocalizedString title, bool isDefaultTab = false, string tabNameToSelect = "", string customCssClass = "")
        {
            if (helper == null)
                throw new ArgumentNullException("helper");

            if (string.IsNullOrEmpty(tabNameToSelect))
                tabNameToSelect = helper.GetSelectedTabName();

            if (string.IsNullOrEmpty(tabNameToSelect) && isDefaultTab)
                tabNameToSelect = currentTabName;

            var a = new TagBuilder("a");
            a.Attributes.Add("data-tab-name", currentTabName);
            a.Attributes.Add("href", string.Format("#{0}", currentTabName));
            a.Attributes.Add("data-toggle", "tab");
            //3.90 assigned InnerHtml, i.e. RAW markup - AppendHtml preserves that, Append would
            //start HTML-encoding the localized title
            a.InnerHtml.AppendHtml(title.Text);

            var liClassValue = "";
            if (tabNameToSelect == currentTabName)
            {
                liClassValue = "active";
            }
            if (!String.IsNullOrEmpty(customCssClass))
            {
                if (!String.IsNullOrEmpty(liClassValue))
                    liClassValue += " ";
                liClassValue += customCssClass;
            }

            var li = new TagBuilder("li");
            li.Attributes.Add("class", liClassValue);
            a.TagRenderMode = TagRenderMode.Normal;
            li.InnerHtml.AppendHtml(a);

            return new HtmlString(li.ToHtmlString(TagRenderMode.Normal));
        }

        /// <summary>
        /// Gets a selected tab name (used in admin area to store selected tab name)
        /// </summary>
        /// <returns>Name</returns>
        public static string GetSelectedTabName(this IHtmlHelper helper)
        {
            //keep this method synchornized with
            //"SaveSelectedTab" method of \Administration\Controllers\BaseAdminController.cs
            var tabName = string.Empty;
            const string dataKey = "nop.selected-tab-name";

            if (helper.ViewData.ContainsKey(dataKey))
                tabName = helper.ViewData[dataKey].ToString();

            //ViewContext.Controller does not exist in ASP.NET Core; IHtmlHelper exposes TempData
            //directly, which is the same dictionary the controller used
            if (helper.TempData.ContainsKey(dataKey))
                tabName = helper.TempData[dataKey].ToString();

            return tabName;
        }

        #region Form fields

        public static IHtmlContent Hint(this IHtmlHelper helper, string value)
        {
            //create tag builder
            var builder = new TagBuilder("div");
            builder.MergeAttribute("title", value);
            builder.MergeAttribute("class", "ico-help");
            builder.InnerHtml.AppendHtml("<i class='fa fa-question-circle'></i>");
            //render tag
            return new HtmlString(builder.ToHtmlString(TagRenderMode.Normal));
        }

        public static IHtmlContent NopLabelFor<TModel, TValue>(this IHtmlHelper<TModel> helper,
                Expression<Func<TModel, TValue>> expression, bool displayHint = true)
        {
            var result = new StringBuilder();
            var metadata = GetModelExpression(helper, expression).Metadata;
            var hintResource = string.Empty;
            object value;

            result.Append(helper.LabelFor(expression, new { title = hintResource, @class = "control-label" }).ToHtmlString());

            if (metadata.AdditionalValues.TryGetValue("NopResourceDisplayName", out value))
            {
                var resourceDisplayName = value as NopResourceDisplayName;
                if (resourceDisplayName != null && displayHint)
                {
                    var langId = EngineContext.Current.Resolve<IWorkContext>().WorkingLanguage.Id;
                    hintResource = EngineContext.Current.Resolve<ILocalizationService>()
                        .GetResource(resourceDisplayName.ResourceKey + ".Hint",  langId, returnEmptyIfNotFound: true, logIfNotFound: false);
                    if (!String.IsNullOrEmpty(hintResource))
                    {
                        result.Append(helper.Hint(hintResource).ToHtmlString());
                    }
                }
            }

            var laberWrapper = new TagBuilder("div");
            laberWrapper.Attributes.Add("class", "label-wrapper");
            laberWrapper.InnerHtml.AppendHtml(result.ToString());

            return new HtmlString(laberWrapper.ToHtmlString(TagRenderMode.Normal));
        }

        public static IHtmlContent NopEditorFor<TModel, TValue>(this IHtmlHelper<TModel> helper,
            Expression<Func<TModel, TValue>> expression, string postfix = "",
            bool? renderFormControlClass = null, bool required = false)
        {
            var result = new StringBuilder();

            object htmlAttributes = null;
            var metadata = GetModelExpression(helper, expression).Metadata;
            if ((!renderFormControlClass.HasValue && metadata.ModelType.Name.Equals("String")) ||
                (renderFormControlClass.HasValue && renderFormControlClass.Value))
                htmlAttributes = new {@class = "form-control"};

            if (required)
                result.AppendFormat(
                    "<div class=\"input-group input-group-required\">{0}<div class=\"input-group-btn\"><span class=\"required\">*</span></div></div>",
                    helper.EditorFor(expression, new {htmlAttributes, postfix}).ToHtmlString());
            else
                result.Append(helper.EditorFor(expression, new {htmlAttributes, postfix}).ToHtmlString());

            return new HtmlString(result.ToString());
        }

        public static IHtmlContent NopDropDownList<TModel>(this IHtmlHelper<TModel> helper, string name,
            IEnumerable<SelectListItem> itemList, object htmlAttributes = null, 
            bool renderFormControlClass = true, bool required = false)
        {
            var result = new StringBuilder();

            var attrs = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (renderFormControlClass)
                attrs = AddFormControlClassToHtmlAttributes(attrs);

            if (required)
                result.AppendFormat(
                    "<div class=\"input-group input-group-required\">{0}<div class=\"input-group-btn\"><span class=\"required\">*</span></div></div>",
                    helper.DropDownList(name, itemList, attrs).ToHtmlString());
            else
                result.Append(helper.DropDownList(name, itemList, attrs).ToHtmlString());

            return new HtmlString(result.ToString());
        }

        public static IHtmlContent NopDropDownListFor<TModel, TValue>(this IHtmlHelper<TModel> helper,
            Expression<Func<TModel, TValue>> expression, IEnumerable<SelectListItem> itemList,
            object htmlAttributes = null, bool renderFormControlClass = true, bool required = false)
        {
            var result = new StringBuilder();

            var attrs = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (renderFormControlClass)
                attrs = AddFormControlClassToHtmlAttributes(attrs);

            if (required)
                result.AppendFormat(
                    "<div class=\"input-group input-group-required\">{0}<div class=\"input-group-btn\"><span class=\"required\">*</span></div></div>",
                    helper.DropDownListFor(expression, itemList, attrs).ToHtmlString());
            else
                result.Append(helper.DropDownListFor(expression, itemList, attrs).ToHtmlString());

            return new HtmlString(result.ToString());
        }

        public static IHtmlContent NopTextAreaFor<TModel, TValue>(this IHtmlHelper<TModel> helper,
            Expression<Func<TModel, TValue>> expression, object htmlAttributes = null,
            bool renderFormControlClass = true, int rows = 4, int columns = 20, bool required = false)
        {
            var result = new StringBuilder();

            var attrs = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (renderFormControlClass)
                attrs = AddFormControlClassToHtmlAttributes(attrs);

            if (required)
                result.AppendFormat(
                    "<div class=\"input-group input-group-required\">{0}<div class=\"input-group-btn\"><span class=\"required\">*</span></div></div>",
                    helper.TextAreaFor(expression, rows, columns, attrs).ToHtmlString());
            else
                result.Append(helper.TextAreaFor(expression, rows, columns, attrs).ToHtmlString());

            return new HtmlString(result.ToString());
        }


        public static IHtmlContent NopDisplayFor<TModel, TValue>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TValue>> expression)
        {
            var result = new TagBuilder("div");
            result.Attributes.Add("class", "form-text-row");
            result.InnerHtml.AppendHtml(helper.DisplayFor(expression));

            return new HtmlString(result.ToHtmlString(TagRenderMode.Normal));
        }

        public static IHtmlContent NopDisplay<TModel>(this IHtmlHelper<TModel> helper, string expression)
        {
            var result = new TagBuilder("div");
            result.Attributes.Add("class", "form-text-row");
            //3.90 assigned InnerHtml, i.e. raw markup
            result.InnerHtml.AppendHtml(expression);

            return new HtmlString(result.ToHtmlString(TagRenderMode.Normal));
        }

        /// <summary>
        /// Ensure the html attribute dictionary carries the bootstrap "form-control" class.
        /// </summary>
        /// <remarks>
        /// <b>Signature change and a bug fix, both forced by the platform.</b>
        /// MVC 5's <c>HtmlHelper.AnonymousObjectToHtmlAttributes</c> returned a
        /// <c>System.Web.Routing.RouteValueDictionary</c>, so this method could both declare that
        /// return type and rely on <c>RouteValueDictionary</c>'s indexer returning <c>null</c> for
        /// an absent key.
        ///
        /// ASP.NET Core's <see cref="HtmlHelper.AnonymousObjectToHtmlAttributes"/> returns a plain
        /// <c>Dictionary&lt;string, object&gt;</c>. Two consequences:
        /// <list type="number">
        /// <item>
        /// the declared return type becomes <c>IDictionary&lt;string, object&gt;</c> — an
        /// <c>as RouteValueDictionary</c> cast would now yield <c>null</c> and silently drop every
        /// html attribute;
        /// </item>
        /// <item>
        /// <c>htmlAttributes["class"]</c> on a missing key throws
        /// <see cref="KeyNotFoundException"/> instead of returning <c>null</c>. That would have
        /// thrown for every <c>NopDropDownList</c>/<c>NopDropDownListFor</c>/<c>NopTextAreaFor</c>
        /// call that does not pass an explicit class — i.e. most of them. Rewritten with
        /// <c>TryGetValue</c>, which reproduces the MVC 5 behaviour exactly.
        /// </item>
        /// </list>
        /// </remarks>
        public static IDictionary<string, object> AddFormControlClassToHtmlAttributes(IDictionary<string, object> htmlAttributes)
        {
            if (htmlAttributes == null)
                htmlAttributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            htmlAttributes.TryGetValue("class", out var existingClass);

            if (existingClass == null || string.IsNullOrEmpty(existingClass.ToString()))
                htmlAttributes["class"] = "form-control";
            else
                if (!existingClass.ToString().Contains("form-control"))
                htmlAttributes["class"] = existingClass + " form-control";

            return htmlAttributes;
        }

        #endregion

        #endregion

        #region Common extensions

        public static IHtmlContent RequiredHint(this IHtmlHelper helper, string additionalText = null)
        {
            // Create tag builder
            var builder = new TagBuilder("span");
            builder.AddCssClass("required");
            var innerText = "*";
            //add additional text if specified
            if (!String.IsNullOrEmpty(additionalText))
                innerText += " " + additionalText;
            //TagBuilder.SetInnerText is gone; InnerHtml.SetContent HTML-encodes, which is what
            //SetInnerText did
            builder.InnerHtml.SetContent(innerText);
            // Render tag
            return new HtmlString(builder.ToHtmlString(TagRenderMode.Normal));
        }

        /// <summary>
        /// Get the fully qualified html field name for an expression.
        /// </summary>
        /// <remarks>
        /// 3.90 used <c>ViewData.TemplateInfo.GetFullHtmlFieldName(ExpressionHelper.GetExpressionText(expression))</c>.
        /// <c>IHtmlHelper&lt;TModel&gt;.NameFor</c> is exactly that composition and is the public
        /// ASP.NET Core equivalent.
        /// </remarks>
        public static string FieldNameFor<T, TResult>(this IHtmlHelper<T> html, Expression<Func<T, TResult>> expression)
        {
            return html.NameFor(expression);
        }
        /// <summary>
        /// Get the sanitized html element id for an expression.
        /// </summary>
        /// <remarks>
        /// 3.90 used <c>ViewData.TemplateInfo.GetFullHtmlFieldId(...)</c> plus a manual
        /// <c>'[' -&gt; '_'</c> / <c>']' -&gt; '_'</c> replacement, because MVC 5's
        /// <c>GetFullHtmlFieldId</c> only replaced <c>'.'</c>.
        /// <b><c>TemplateInfo.GetFullHtmlFieldId</c> does not exist in ASP.NET Core.</b>
        /// <c>IHtmlHelper&lt;TModel&gt;.IdFor</c> is the replacement, and it subsumes the manual
        /// replacement: it runs the name through
        /// <c>TagBuilder.CreateSanitizedId(name, IdAttributeDotReplacement)</c>, which replaces
        /// every character that is not valid in an HTML4 id — including <c>[</c> and <c>]</c> —
        /// with the same <c>"_"</c>. Output is therefore identical.
        /// </remarks>
        public static string FieldIdFor<T, TResult>(this IHtmlHelper<T> html, Expression<Func<T, TResult>> expression)
        {
            return html.IdFor(expression);
        }

        /// <summary>
        /// Creates a days, months, years drop down list using an HTML select control. 
        /// The parameters represent the value of the "name" attribute on the select control.
        /// </summary>
        /// <param name="html">HTML helper</param>
        /// <param name="dayName">"Name" attribute of the day drop down list.</param>
        /// <param name="monthName">"Name" attribute of the month drop down list.</param>
        /// <param name="yearName">"Name" attribute of the year drop down list.</param>
        /// <param name="beginYear">Begin year</param>
        /// <param name="endYear">End year</param>
        /// <param name="selectedDay">Selected day</param>
        /// <param name="selectedMonth">Selected month</param>
        /// <param name="selectedYear">Selected year</param>
        /// <param name="localizeLabels">Localize labels</param>
        /// <param name="htmlAttributes">HTML attributes</param>
		/// <param name="wrapTags">Wrap HTML select controls with span tags for styling/layout</param>
        /// <returns></returns>
        public static IHtmlContent DatePickerDropDowns(this IHtmlHelper html,
            string dayName, string monthName, string yearName,
            int? beginYear = null, int? endYear = null,
            int? selectedDay = null, int? selectedMonth = null, int? selectedYear = null,
            bool localizeLabels = true, object htmlAttributes = null, bool wrapTags = false)
        {
            var daysList = new TagBuilder("select");
            var monthsList = new TagBuilder("select");
            var yearsList = new TagBuilder("select");

            daysList.Attributes.Add("name", dayName);
            monthsList.Attributes.Add("name", monthName);
            yearsList.Attributes.Add("name", yearName);

            var htmlAttributesDictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            daysList.MergeAttributes(htmlAttributesDictionary, true);
            monthsList.MergeAttributes(htmlAttributesDictionary, true);
            yearsList.MergeAttributes(htmlAttributesDictionary, true);

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

            daysList.InnerHtml.AppendHtml(days.ToString());
            monthsList.InnerHtml.AppendHtml(months.ToString());
            yearsList.InnerHtml.AppendHtml(years.ToString());

            //NOTE: 3.90 relied on TagBuilder.ToString() rendering the element. ASP.NET Core's
            //TagBuilder does not override ToString(), so every render is explicit.
            var renderedDaysList = daysList.ToHtmlString(TagRenderMode.Normal);
            var renderedMonthsList = monthsList.ToHtmlString(TagRenderMode.Normal);
            var renderedYearsList = yearsList.ToHtmlString(TagRenderMode.Normal);

            if (wrapTags) 
            {
                string wrapDaysList = "<span class=\"days-list select-wrapper\">" + renderedDaysList + "</span>";
                string wrapMonthsList = "<span class=\"months-list select-wrapper\">" + renderedMonthsList + "</span>";
                string wrapYearsList = "<span class=\"years-list select-wrapper\">" + renderedYearsList + "</span>";

                return new HtmlString(string.Concat(wrapDaysList, wrapMonthsList, wrapYearsList));
            }
            else
            {
                return new HtmlString(string.Concat(renderedDaysList, renderedMonthsList, renderedYearsList));
            }

        }

        /// <summary>
        /// Render the widgets registered for a widget zone.
        /// </summary>
        /// <remarks>
        /// <b>Child action → View Component (Requirement 4.2, design §5).</b>
        /// 3.90 called <c>Html.Action("WidgetsByZone", "Widget", …)</c> against
        /// <c>Nop.Web.Controllers.WidgetController.WidgetsByZone</c>, which carried
        /// <c>[ChildActionOnly]</c>. Neither <c>Html.Action</c> nor child actions exist in
        /// ASP.NET Core, so this now invokes a <c>Widget</c> view component.
        ///
        /// Three things a caller needs to know:
        /// <list type="bullet">
        /// <item>
        /// The signature is unchanged and stays SYNCHRONOUS, so the ~205 <c>@Html.Widget(...)</c>
        /// call sites across the Nop.Web and Nop.Admin views compile and behave as before. The
        /// sync-over-async is the same trade-off already accepted in task 4.2/6.2 — ASP.NET Core
        /// installs no <c>SynchronizationContext</c>, so it cannot deadlock.
        /// </item>
        /// <item>
        /// The <paramref name="area"/> parameter is now INERT. It existed to route the child
        /// action to the right area; view components are resolved by name from the whole
        /// application and are not area-scoped. It is kept so no call site changes.
        /// </item>
        /// <item>
        /// View components do NOT run the action-filter pipeline, so none of the filters that used
        /// to execute around the child action run any more. Task 6.2 already relied on this when
        /// it removed the <c>IsChildAction</c> guards from 11 filters.
        /// </item>
        /// </list>
        /// The <c>Widget</c> view component itself is created by task 7.3.
        /// </remarks>
        /// <param name="helper">HTML helper</param>
        /// <param name="widgetZone">Widget zone name</param>
        /// <param name="additionalData">Additional data passed to the widgets</param>
        /// <param name="area">INERT — view components are not area-scoped</param>
        /// <returns>Widget markup</returns>
        public static IHtmlContent Widget(this IHtmlHelper helper, string widgetZone, object additionalData = null, string area = null)
        {
            var viewComponentHelper = helper.ViewContext.HttpContext.RequestServices
                .GetRequiredService<IViewComponentHelper>();

            //IViewComponentHelper is registered per-request but is not contextualized until it is
            //handed the ambient ViewContext; @Component.InvokeAsync in a view gets this for free
            (viewComponentHelper as IViewContextAware)?.Contextualize(helper.ViewContext);

            return viewComponentHelper
                .InvokeAsync("Widget", new { widgetZone = widgetZone, additionalData = additionalData })
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Renders the standard label with a specified suffix added to label text
        /// </summary>
        /// <typeparam name="TModel">Model</typeparam>
        /// <typeparam name="TValue">Value</typeparam>
        /// <param name="html">HTML helper</param>
        /// <param name="expression">Expression</param>
        /// <param name="htmlAttributes">HTML attributes</param>
        /// <param name="suffix">Suffix</param>
        /// <returns>Label</returns>
        public static IHtmlContent LabelFor<TModel, TValue>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, object htmlAttributes, string suffix)
        {
            var modelExpression = GetModelExpression(html, expression);
            var htmlFieldName = modelExpression.Name;
            var metadata = modelExpression.Metadata;
            string resolvedLabelText = metadata.DisplayName ?? (metadata.PropertyName ?? htmlFieldName.Split(new[] { '.' }).Last());
            if (string.IsNullOrEmpty(resolvedLabelText))
            {
                return HtmlString.Empty;
            }
            var tag = new TagBuilder("label");
            //3.90: TagBuilder.CreateSanitizedId(ViewData.TemplateInfo.GetFullHtmlFieldId(htmlFieldName)).
            //TagBuilder.CreateSanitizedId still exists on net10.0 but now REQUIRES the
            //invalid-character replacement argument, and GetFullHtmlFieldId is gone. IdFor performs
            //exactly this composition - CreateSanitizedId(NameFor(expression), IdAttributeDotReplacement).
            tag.Attributes.Add("for", html.IdFor(expression));
            if (!String.IsNullOrEmpty(suffix))
            {
                resolvedLabelText = String.Concat(resolvedLabelText, suffix);
            }
            tag.InnerHtml.SetContent(resolvedLabelText);

            var dictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            tag.MergeAttributes(dictionary, true);

            return new HtmlString(tag.ToHtmlString(TagRenderMode.Normal));
        }

        #endregion
    }
}
