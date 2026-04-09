# Nop.Web.Framework — Web Infrastructure

## Bounded Context
Shared web infrastructure — base controllers, MVC filters, HTML helpers, theme engine, security filters, localization helpers, validation, and UI components.

## Legacy Source
- `src/Presentation/Nop.Web.Framework/` — all subdirectories
- Subdirectories: Controllers, Events, Kendoui, Localization, Menu, Mvc, Security, Seo, Themes, UI, Validators, ViewEngines

## Key Entities
- `BasePublicController`, `BaseAdminController` (base controllers)
- Custom MVC filters and attributes: `AdminAuthorizeAttribute`, `AdminAntiForgeryAttribute`, `AdminValidateIpAddressAttribute`, `PublicAntiForgeryAttribute`, `NopHttpsRequirementAttribute`, `WwwRequirementAttribute`, `FormValueRequiredAttribute`, `ParameterBasedOnFormNameAttribute`, `ParameterBasedOnFormNameAndValueAttribute`, `NoTrimAttribute`, `CaptchaValidatorAttribute`, `HoneypotValidatorAttribute`
- Root-level action filter attributes: `CheckAffiliateAttribute`, `CustomerLastActivityAttribute`, `LanguageSeoCodeAttribute`, `PublicStoreAllowNavigationAttribute`, `StoreClosedAttribute`, `StoreIpAddressAttribute`, `StoreLastVisitedPageAttribute`, `ValidatePasswordAttribute`
- `WebWorkContext` (IWorkContext implementation), `WebStoreContext` (IStoreContext implementation)
- `RemotePost` — helper for payment gateway form POST redirects
- `NopResourceDisplayName` — localized display name attribute for model properties
- Routing infrastructure: `IRouteProvider`, `IRoutePublisher`, `GenericPathRoute` (SEO-friendly URL routing), `GuidConstraint` (custom route constraint for GUID parameters)
- Custom model binders: `NopModelBinder`, `CommaSeparatedModelBinder`
- Custom action results: `RssActionResult`, `NullJsonResult`, `XmlDownloadResult`, `ConverterJsonResult`
- Base models: `BaseNopModel`, `BasePageableModel`, `ActionConfirmationModel`, `DeleteConfirmationModel`
- HTML helpers for paging, localization, SEO: `HtmlExtensions` (697 LOC), `LayoutExtensions` (366 LOC), `DataListExtensions`, `UrlHelperExtensions`, `LocalizedRouteExtensions`
- Theme engine (`IThemeContext`, `IThemeProvider`, `ThemeableRazorViewEngine`)
- FluentValidation integration (`BaseNopValidator<T>`, `CreditCardPropertyValidator`, `DecimalPropertyValidator`)
- Kendo UI helpers (admin grid — `DataSourceRequest`, `DataSourceResult`, `Filter`, `Sort`, `ModelStateExtensions`, `QueryableExtensions`)
- `FilePermissionHelper` (186 LOC) — checks file system write permissions for installation
- Admin menu system (`IAdminMenuPlugin`, `SiteMapNode`, `XmlSiteMap`)
- Page head builder (`IPageHeadBuilder` — CSS/JS/canonical URL management)
- Captcha integration (Google reCAPTCHA — `CaptchaValidatorAttribute`, `GReCaptchaValidator`)
- Honeypot anti-spam (`HoneypotValidatorAttribute`)
- Localization helpers: `ILocalizedModel`, `LocalizedRoute`, `LocalizedUrlExtensions`, `Localizer`
- Admin events: `AdminTabStripCreated`, `ProductSearchEvent`

## External Dependencies
- ASP.NET MVC 5 → ASP.NET Core MVC
- FluentValidation 6.x → FluentValidation 11.x
- Kendo UI → consider replacement (Telerik UI for ASP.NET Core or alternative)

## Migration Notes
- **Decision**: Rewrite
- Base controllers → ASP.NET Core `Controller` with shared functionality
- Custom filters → ASP.NET Core `IActionFilter`, `IAuthorizationFilter`, `IExceptionFilter`
- Root-level action filters → ASP.NET Core middleware or global filters (e.g., `CustomerLastActivityAttribute` → middleware, `StoreClosedAttribute` → middleware, `LanguageSeoCodeAttribute` → route constraint or middleware)
- `WebWorkContext` / `WebStoreContext` → scoped services using `IHttpContextAccessor`
- `RemotePost` → replace with `HttpClient`-based approach or JavaScript form submission
- `GenericPathRoute` / `IRouteProvider` → ASP.NET Core endpoint routing with custom route constraints
- Custom model binders → ASP.NET Core `IModelBinder` implementations
- Custom action results → ASP.NET Core `IActionResult` implementations
- HTML helpers → Tag Helpers and View Components
- Theme engine → ASP.NET Core view location expanders
- Kendo UI grid → evaluate Telerik UI for ASP.NET Core or open-source alternative
- FluentValidation → update to latest, register via DI
- Localized routes → ASP.NET Core route constraints

## Acceptance Criteria
- [ ] Base controllers provide shared functionality (notifications, localization, work context access)
- [ ] Theme engine resolves views from active theme directory with fallback to default
- [ ] FluentValidation validators registered and executed for all model binding
- [ ] Admin area protected by authorization filter requiring admin role
- [ ] Paging, localization, and SEO tag helpers produce correct HTML
- [ ] All 8 root-level action filter attributes ported as ASP.NET Core middleware or global filters with equivalent behavior
- [ ] `WebWorkContext` and `WebStoreContext` resolve current customer and store from HTTP context
- [ ] `GenericPathRoute` SEO-friendly URL routing works via ASP.NET Core endpoint routing
