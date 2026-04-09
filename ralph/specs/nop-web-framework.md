# Nop.Web.Framework — Web Infrastructure

## Bounded Context
Shared web infrastructure — base controllers, MVC filters, HTML helpers, theme engine, security filters, localization helpers, validation, and UI components.

## Legacy Source
- `src/Presentation/Nop.Web.Framework/` — all subdirectories
- Subdirectories: Controllers, Events, Kendoui, Localization, Menu, Mvc, Security, Seo, Themes, UI, Validators, ViewEngines

## Key Entities
- `BasePublicController`, `BaseAdminController` (base controllers)
- Custom MVC filters and attributes (`AdminAuthorize`, anti-forgery, HTTPS, IP validation, etc.)
- HTML helpers for paging, localization, SEO
- Theme engine (`IThemeContext`, `IThemeProvider`, `ThemeableRazorViewEngine`)
- FluentValidation integration
- Kendo UI helpers (admin grid — `DataSourceRequest`, `DataSourceResult`, `Filter`, `Sort`)
- Admin menu system (`IAdminMenuPlugin`, `SiteMapNode`, `XmlSiteMap`)
- Page head builder (`IPageHeadBuilder` — CSS/JS/canonical URL management)
- Captcha integration (Google reCAPTCHA — `CaptchaValidatorAttribute`, `GReCaptchaValidator`)
- Honeypot anti-spam (`HoneypotValidatorAttribute`)

## External Dependencies
- ASP.NET MVC 5 → ASP.NET Core MVC
- FluentValidation 6.x → FluentValidation 11.x
- Kendo UI → consider replacement (Telerik UI for ASP.NET Core or alternative)

## Migration Notes
- **Decision**: Rewrite
- Base controllers → ASP.NET Core `Controller` with shared functionality
- Custom filters → ASP.NET Core `IActionFilter`, `IAuthorizationFilter`, `IExceptionFilter`
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
