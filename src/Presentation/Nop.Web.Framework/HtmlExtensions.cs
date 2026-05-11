using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;
using Nop.Services.Stores;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.DependencyInjection;


namespace Nop.Web.Framework
{
    public static class HtmlExtensions
    {
        #region Html.Action compatibility shim

        /// <summary>
        /// Compatibility shim for Html.Action() which was removed in ASP.NET Core.
        /// Invokes a controller action and returns its rendered HTML.
        /// </summary>
        public static IHtmlContent Action(this IHtmlHelper helper, string action)
        {
            return Action(helper, action, null, null);
        }

        public static IHtmlContent Action(this IHtmlHelper helper, string action, object routeValues)
        {
            return Action(helper, action, null, routeValues);
        }

        public static IHtmlContent Action(this IHtmlHelper helper, string action, string controller, object routeValues = null)
        {
            var httpContext = helper.ViewContext.HttpContext;

            // Prevent infinite recursion
            const string depthKey = "HtmlAction_Depth";
            var depth = httpContext.Items.ContainsKey(depthKey) ? (int)httpContext.Items[depthKey] : 0;
            if (depth > 3)
                return HtmlString.Empty;
            httpContext.Items[depthKey] = depth + 1;

            try
            {
                controller = controller ?? helper.ViewContext.RouteData.Values["controller"]?.ToString();
                var routeDict = routeValues != null ? new RouteValueDictionary(routeValues) : new RouteValueDictionary();
                var actionDescriptorProvider = httpContext.RequestServices.GetRequiredService<IActionDescriptorCollectionProvider>();
                var descriptor = actionDescriptorProvider.ActionDescriptors.Items
                    .OfType<ControllerActionDescriptor>()
                    .FirstOrDefault(d => string.Equals(d.RouteValues["action"], action, StringComparison.OrdinalIgnoreCase)
                                      && string.Equals(d.RouteValues["controller"], controller, StringComparison.OrdinalIgnoreCase));

                if (descriptor == null)
                    return HtmlString.Empty;

                // Create route data for the child action
                var routeData = new RouteData();
                routeData.Values["action"] = action;
                routeData.Values["controller"] = controller;
                foreach (var kvp in routeDict)
                    routeData.Values[kvp.Key] = kvp.Value;

                // Create controller instance via DI (Autofac)
                var controllerType = descriptor.ControllerTypeInfo.AsType();
                var controller_instance = EngineContext.Current.Resolve(controllerType);

                if (controller_instance is Controller mvcController)
                {
                    var actionContext = new ActionContext(httpContext, routeData, descriptor);
                    mvcController.ControllerContext = new ControllerContext(actionContext);
                    // Set up TempData
                    var tempDataProvider = httpContext.RequestServices.GetRequiredService<ITempDataProvider>();
                    mvcController.TempData = new TempDataDictionary(httpContext, tempDataProvider);
                }

                // Invoke the action method
                var methodInfo = descriptor.MethodInfo;
                var parameters = methodInfo.GetParameters();
                var args = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (routeDict.TryGetValue(parameters[i].Name, out var val) && val != null)
                    {
                        if (parameters[i].ParameterType.IsAssignableFrom(val.GetType()))
                            args[i] = val;
                        else
                            args[i] = Convert.ChangeType(val, parameters[i].ParameterType);
                    }
                    else if (parameters[i].HasDefaultValue)
                        args[i] = parameters[i].DefaultValue;
                    else
                        args[i] = parameters[i].ParameterType.IsValueType ? Activator.CreateInstance(parameters[i].ParameterType) : null;
                }

                var result = methodInfo.Invoke(controller_instance, args);

                // Handle async results
                if (result is Task task)
                {
                    task.GetAwaiter().GetResult();
                    var taskType = task.GetType();
                    if (taskType.IsGenericType)
                        result = taskType.GetProperty("Result")?.GetValue(task);
                    else
                        return HtmlString.Empty;
                }

                if (result is ViewResult viewResult)
                {
                    return RenderViewResultToHtml(httpContext, helper.ViewContext.RouteData, descriptor, viewResult, controller_instance as Controller);
                }
                else if (result is PartialViewResult partialViewResult)
                {
                    return RenderPartialViewResultToHtml(httpContext, helper.ViewContext.RouteData, descriptor, partialViewResult, controller_instance as Controller);
                }
                else if (result is ContentResult contentResult)
                {
                    return new HtmlString(contentResult.Content ?? "");
                }
            }
            catch
            {
                // Swallow errors in child actions
            }
            finally
            {
                httpContext.Items[depthKey] = depth;
            }

            return HtmlString.Empty;
        }

        private static IHtmlContent RenderViewResultToHtml(Microsoft.AspNetCore.Http.HttpContext httpContext, RouteData parentRouteData, ControllerActionDescriptor descriptor, ViewResult viewResult, Controller mvcController)
        {
            var viewEngine = httpContext.RequestServices.GetRequiredService<IRazorViewEngine>();
            var tempDataProvider = httpContext.RequestServices.GetRequiredService<ITempDataProvider>();

            var routeData = new RouteData(parentRouteData);
            routeData.Values["action"] = descriptor.RouteValues["action"];
            routeData.Values["controller"] = descriptor.RouteValues["controller"];
            var actionContext = new ActionContext(httpContext, routeData, descriptor);

            var viewName = viewResult.ViewName ?? descriptor.ActionName;
            var viewEngineResult = viewEngine.FindView(actionContext, viewName, false);
            if (!viewEngineResult.Success)
            {
                // Try as main page to find in controller folder
                viewEngineResult = viewEngine.FindView(actionContext, viewName, true);
            }
            if (!viewEngineResult.Success)
                return HtmlString.Empty;

            using (var sw = new StringWriter())
            {
                var viewData = mvcController?.ViewData ?? new ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
                if (viewResult.Model != null)
                    viewData.Model = viewResult.Model;
                // Suppress layout for child actions
                viewData["__ChildAction"] = true;
                var tempData = new TempDataDictionary(httpContext, tempDataProvider);
                var viewContext = new ViewContext(actionContext, viewEngineResult.View, viewData, tempData, sw, new HtmlHelperOptions());
                viewEngineResult.View.RenderAsync(viewContext).GetAwaiter().GetResult();
                return new HtmlString(sw.ToString());
            }
        }

        private static IHtmlContent RenderPartialViewResultToHtml(Microsoft.AspNetCore.Http.HttpContext httpContext, RouteData parentRouteData, ControllerActionDescriptor descriptor, PartialViewResult partialViewResult, Controller mvcController)
        {
            var viewEngine = httpContext.RequestServices.GetRequiredService<IRazorViewEngine>();
            var tempDataProvider = httpContext.RequestServices.GetRequiredService<ITempDataProvider>();

            var routeData = new RouteData(parentRouteData);
            routeData.Values["action"] = descriptor.RouteValues["action"];
            routeData.Values["controller"] = descriptor.RouteValues["controller"];
            var actionContext = new ActionContext(httpContext, routeData, descriptor);

            var viewName = partialViewResult.ViewName ?? descriptor.ActionName;
            var viewEngineResult = viewEngine.FindView(actionContext, viewName, false);
            if (!viewEngineResult.Success)
            {
                viewEngineResult = viewEngine.FindView(actionContext, viewName, true);
            }
            if (!viewEngineResult.Success)
                return HtmlString.Empty;

            using (var sw = new StringWriter())
            {
                var viewData = mvcController?.ViewData ?? new ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
                if (partialViewResult.Model != null)
                    viewData.Model = partialViewResult.Model;
                // Suppress layout for child actions
                viewData["__ChildAction"] = true;
                var tempData = new TempDataDictionary(httpContext, tempDataProvider);
                var viewContext = new ViewContext(actionContext, viewEngineResult.View, viewData, tempData, sw, new HtmlHelperOptions());
                viewEngineResult.View.RenderAsync(viewContext).GetAwaiter().GetResult();
                return new HtmlString(sw.ToString());
            }
        }

        #endregion

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
                foreach (var locale in helper.ViewData.Model.Locales)
                {
                    //languages
                    var language = languageService.GetLanguageById(locale.LanguageId);
                    if (language == null)
                        throw new Exception("Language cannot be loaded");

                    tabStrip.AppendLine("<li>");
                    var iconUrl = "/Content/images/flags/" + language.FlagImageFileName;
                    tabStrip.AppendLine(string.Format("<a data-tab-name=\"{0}-{1}-tab\" href=\"#{0}-{1}-tab\" data-toggle=\"tab\"><img alt='' src='{2}'>{3}</a>",
                            name, 
                            language.Id,
                            iconUrl,
                            WebUtility.HtmlEncode(language.Name)));

                    tabStrip.AppendLine("</li>");
                }
                tabStrip.AppendLine("</ul>");
                
                //default tab
                tabStrip.AppendLine("<div class=\"tab-content\">");
                tabStrip.AppendLine(string.Format("<div class=\"tab-pane active\" id=\"{0}-{1}-tab\">", name, "standard"));
                tabStrip.AppendLine(GetHtmlContentString(standardTemplate(helper.ViewData.Model)));
                tabStrip.AppendLine("</div>");

                for (int i = 0; i < helper.ViewData.Model.Locales.Count; i++)
                {
                    //languages
                    var language = languageService.GetLanguageById(helper.ViewData.Model.Locales[i].LanguageId);

                    tabStrip.AppendLine(string.Format("<div class=\"tab-pane\" id=\"{0}-{1}-tab\">",
                        name,
                        language.Id));
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
            if (String.IsNullOrEmpty(actionName))
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
            window.AppendLine(string.Format("<div id='{0}' class=\"modal fade\"  tabindex=\"-1\" role=\"dialog\" aria-labelledby=\"{0}-title\">", modalId));
            window.AppendLine(GetHtmlContentString(helper.Partial("Delete", deleteConfirmationModel)));
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
                actionName = helper.ViewContext.RouteData.Values["action"]?.ToString();

            var modalId = buttonId + "-action-confirmation";

            var actionConfirmationModel = new ActionConfirmationModel()
            {
                ControllerName = helper.ViewContext.RouteData.Values["controller"]?.ToString(),
                ActionName = actionName,
                WindowId = modalId
            };

            var window = new StringBuilder();
            window.AppendLine(string.Format("<div id='{0}' class=\"modal fade\"  tabindex=\"-1\" role=\"dialog\" aria-labelledby=\"{0}-title\">", modalId));
            window.AppendLine(GetHtmlContentString(helper.Partial("Confirm", actionConfirmationModel)));
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
                result.Append(GetHtmlContentString(helper.CheckBoxFor(expression, new Dictionary<string, object>
                {
                    { "class", cssClass },
                    { "onclick", onClick },
                    { "data-for-input-selector", dataInputSelector },
                })));
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
        /// <returns>IHtmlContent</returns>
        public static IHtmlContent RenderBootstrapTabContent(this IHtmlHelper helper, string currentTabName,
            IHtmlContent content, bool isDefaultTab = false, string tabNameToSelect = "")
        {
            if (helper == null)
                throw new ArgumentNullException("helper");

            if (string.IsNullOrEmpty(tabNameToSelect))
                tabNameToSelect = helper.GetSelectedTabName();

            if (string.IsNullOrEmpty(tabNameToSelect) && isDefaultTab)
                tabNameToSelect = currentTabName;

            var tag = new TagBuilder("div");
            tag.InnerHtml.AppendHtml(content);
            tag.AddCssClass(string.Format("tab-pane{0}", tabNameToSelect == currentTabName ? " active" : ""));
            tag.Attributes["id"] = currentTabName;

            return tag;
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
        /// <returns>IHtmlContent</returns>
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
            a.Attributes["data-tab-name"] = currentTabName;
            a.Attributes["href"] = string.Format("#{0}", currentTabName);
            a.Attributes["data-toggle"] = "tab";
            a.InnerHtml.Append(title.Text);

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
            if (!string.IsNullOrEmpty(liClassValue))
                li.AddCssClass(liClassValue);
            li.InnerHtml.AppendHtml(a);

            return li;
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

            if (helper.ViewContext.TempData.ContainsKey(dataKey))
                tabName = helper.ViewContext.TempData[dataKey].ToString();

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
            return builder;
        }

        public static IHtmlContent NopLabelFor<TModel, TValue>(this IHtmlHelper<TModel> helper,
                Expression<Func<TModel, TValue>> expression, bool displayHint = true)
        {
            var result = new StringBuilder();
            var hintResource = string.Empty;

            result.Append(GetHtmlContentString(helper.LabelFor(expression, new { title = hintResource, @class = "control-label" })));

            var metadata = helper.MetadataProvider.GetMetadataForType(typeof(TModel));
            var expressionText = GetExpressionText(expression);
            var propertyMetadata = metadata.Properties.FirstOrDefault(p => p.PropertyName == expressionText);
            
            if (propertyMetadata != null && displayHint)
            {
                // Check for NopResourceDisplayName attribute
                var property = typeof(TModel).GetProperty(expressionText);
                if (property != null)
                {
                    var resourceDisplayName = property.GetCustomAttributes(typeof(NopResourceDisplayName), true)
                        .FirstOrDefault() as NopResourceDisplayName;
                    if (resourceDisplayName != null)
                    {
                        var langId = EngineContext.Current.Resolve<IWorkContext>().WorkingLanguage.Id;
                        hintResource = EngineContext.Current.Resolve<ILocalizationService>()
                            .GetResource(resourceDisplayName.ResourceKey + ".Hint", langId, returnEmptyIfNotFound: true, logIfNotFound: false);
                        if (!String.IsNullOrEmpty(hintResource))
                        {
                            result.Append(GetHtmlContentString(helper.Hint(hintResource)));
                        }
                    }
                }
            }

            var labelWrapper = new TagBuilder("div");
            labelWrapper.AddCssClass("label-wrapper");
            labelWrapper.InnerHtml.AppendHtml(result.ToString());

            return labelWrapper;
        }

        public static IHtmlContent NopEditorFor<TModel, TValue>(this IHtmlHelper<TModel> helper,
            Expression<Func<TModel, TValue>> expression, string postfix = "",
            bool? renderFormControlClass = null, bool required = false)
        {
            var result = new StringBuilder();

            object htmlAttributes = null;
            var expressionText = GetExpressionText(expression);
            var metadata = helper.MetadataProvider.GetMetadataForType(typeof(TModel));
            var propertyMetadata = metadata.Properties.FirstOrDefault(p => p.PropertyName == expressionText);
            if ((!renderFormControlClass.HasValue && propertyMetadata?.ModelType?.Name.Equals("String") == true) ||
                (renderFormControlClass.HasValue && renderFormControlClass.Value))
                htmlAttributes = new {@class = "form-control"};

            if (required)
                result.AppendFormat(
                    "<div class=\"input-group input-group-required\">{0}<div class=\"input-group-btn\"><span class=\"required\">*</span></div></div>",
                    GetHtmlContentString(helper.EditorFor(expression, new {htmlAttributes, postfix})));
            else
                result.Append(GetHtmlContentString(helper.EditorFor(expression, new {htmlAttributes, postfix})));

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
                    GetHtmlContentString(helper.DropDownList(name, itemList, attrs)));
            else
                result.Append(GetHtmlContentString(helper.DropDownList(name, itemList, attrs)));

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
                    GetHtmlContentString(helper.DropDownListFor(expression, itemList, attrs)));
            else
                result.Append(GetHtmlContentString(helper.DropDownListFor(expression, itemList, attrs)));

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
                    GetHtmlContentString(helper.TextAreaFor(expression, rows, columns, attrs)));
            else
                result.Append(GetHtmlContentString(helper.TextAreaFor(expression, rows, columns, attrs)));

            return new HtmlString(result.ToString());
        }


        public static IHtmlContent NopDisplayFor<TModel, TValue>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TValue>> expression)
        {
            var result = new TagBuilder("div");
            result.AddCssClass("form-text-row");
            result.InnerHtml.AppendHtml(helper.DisplayFor(expression));

            return result;
        }

        public static IHtmlContent NopDisplay<TModel>(this IHtmlHelper<TModel> helper, string expression)
        {
            var result = new TagBuilder("div");
            result.AddCssClass("form-text-row");
            result.InnerHtml.AppendHtml(expression);

            return result;
        }

        public static IDictionary<string, object> AddFormControlClassToHtmlAttributes(IDictionary<string, object> htmlAttributes)
        {
            if (!htmlAttributes.ContainsKey("class") || htmlAttributes["class"] == null || string.IsNullOrEmpty(htmlAttributes["class"].ToString()))
                htmlAttributes["class"] = "form-control";
            else
                if (!htmlAttributes["class"].ToString().Contains("form-control"))
                htmlAttributes["class"] += " form-control";

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
            builder.InnerHtml.Append(innerText);
            // Render tag
            return builder;
        }

        public static string FieldNameFor<T, TResult>(this IHtmlHelper<T> html, Expression<Func<T, TResult>> expression)
        {
            return html.ViewData.TemplateInfo.GetFullHtmlFieldName(GetExpressionText(expression));
        }
        public static string FieldIdFor<T, TResult>(this IHtmlHelper<T> html, Expression<Func<T, TResult>> expression)
        {
            var id = html.ViewData.TemplateInfo.GetFullHtmlFieldName(GetExpressionText(expression)).Replace(".", "_");
            // replace "[" and "]" with "_" for valid HTML IDs
            return id.Replace('[', '_').Replace(']', '_');
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

            if (wrapTags) 
            {
                string wrapDaysList = "<span class=\"days-list select-wrapper\">" + GetHtmlContentString(daysList) + "</span>";
                string wrapMonthsList = "<span class=\"months-list select-wrapper\">" + GetHtmlContentString(monthsList) + "</span>";
                string wrapYearsList = "<span class=\"years-list select-wrapper\">" + GetHtmlContentString(yearsList) + "</span>";

                return new HtmlString(string.Concat(wrapDaysList, wrapMonthsList, wrapYearsList));
            }
            else
            {
                return new HtmlString(string.Concat(GetHtmlContentString(daysList), GetHtmlContentString(monthsList), GetHtmlContentString(yearsList)));
            }

        }

        public static IHtmlContent Widget(this IHtmlHelper helper, string widgetZone, object additionalData = null, string area = null)
        {
            // In ASP.NET Core, Html.Action is replaced by ViewComponents.
            // This will need to be invoked as a ViewComponent in the view.
            return new HtmlString($"<!-- Widget zone: {widgetZone} -->");
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
            string htmlFieldName = GetExpressionText(expression);
            var metadata = html.MetadataProvider.GetMetadataForType(typeof(TModel));
            var propertyMetadata = metadata.Properties.FirstOrDefault(p => p.PropertyName == htmlFieldName);
            string resolvedLabelText = propertyMetadata?.DisplayName ?? (propertyMetadata?.PropertyName ?? htmlFieldName.Split(new[] { '.' }).Last());
            if (string.IsNullOrEmpty(resolvedLabelText))
            {
                return HtmlString.Empty;
            }
            var tag = new TagBuilder("label");
            tag.Attributes.Add("for", html.ViewData.TemplateInfo.GetFullHtmlFieldName(htmlFieldName).Replace(".", "_"));
            if (!String.IsNullOrEmpty(suffix))
            {
                resolvedLabelText = String.Concat(resolvedLabelText, suffix);
            }
            tag.InnerHtml.Append(resolvedLabelText);

            var dictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            tag.MergeAttributes(dictionary, true);

            return tag;
        }

        #endregion

        #region Helpers

        private static string GetExpressionText<TModel, TValue>(Expression<Func<TModel, TValue>> expression)
        {
            // Extract property name from expression
            if (expression.Body is MemberExpression memberExpression)
                return memberExpression.Member.Name;
            if (expression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression operand)
                return operand.Member.Name;
            return string.Empty;
        }

        private static string GetHtmlContentString(IHtmlContent content)
        {
            using (var writer = new StringWriter())
            {
                content.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
                return writer.ToString();
            }
        }

        #endregion
    }
}
