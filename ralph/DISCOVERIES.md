# Ralph Discovery Log

Append-only log of cross-iteration findings. Never edit or remove previous entries.


## 2026-04-09 — Scaffold Iteration

### ATXDocumentation Gaps
- `ATXDocumentation/architecture/components.md` referenced in README but does not exist
- `ATXDocumentation/architecture/patterns.md` referenced in README but does not exist
- `ATXDocumentation/migration/component-order.md` referenced in README but does not exist
- `ATXDocumentation/technical-debt/remediation-plan.md` referenced in README but does not exist
- `ATXDocumentation/technical-debt/outdated-components.md` referenced but does not exist
- `ATXDocumentation/technical-debt/maintenance-burden.md` referenced but does not exist
- Available ATXDocumentation: 39 files across architecture (2), behavior (4), reference (3), analysis (4), migration (2), specialized (3), technical-debt (2), diagrams (4), plus README and project-overview and technical-debt-report

### Codebase Facts
- All 31 projects target .NET Framework 4.5.1 (old-style MSBuild ToolsVersion 12.0)
- 1731 C# files, ~252K LOC total
- 207 domain entity files in `Nop.Core/Domain/` across 26 subdirectories
- 117 service interfaces in `Nop.Services/`
- 27 public controllers (excluding BasePublicController)
- 54 admin controllers (excluding BaseAdminController)
- 20 plugin projects
- 5 test projects

### God Classes (>1000 LOC)
- FedEx SOAP Reference.cs: 16721 LOC (generated, will be replaced by REST API)
- CodeFirstInstallationService.cs: 12269 LOC (seed data — split into domain-specific seeders)
- Admin ProductController: 4857 LOC
- FedEx TrackService.cs: 4850 LOC (generated SOAP)
- Admin OrderController: 4379 LOC
- OrderProcessingService: 3167 LOC
- UPS SOAP Reference.cs: 2787 LOC (generated, will be replaced)
- Admin CustomerController: 2396 LOC
- Admin SettingController: 2339 LOC
- ProductService: 2142 LOC
- WorkflowMessageService: 1920 LOC

### Pattern Decisions
- Replace Autofac with built-in DI (Microsoft.Extensions.DependencyInjection)
- Replace Newtonsoft.Json with System.Text.Json
- Replace custom ICacheManager with IMemoryCache + IDistributedCache
- Replace Forms Authentication with ASP.NET Core Cookie Authentication
- Replace SOAP web references (FedEx, UPS) with REST API clients
- Replace SQL CE support with SQL Server only
- Keep IRepository<T> pattern, implement with EF Core
- Keep domain event pattern (EntityInserted/Updated/Deleted)
- Keep model factory pattern for view model construction
- Keep GenericAttribute extension property system

### Cross-Cutting Findings
- No GDPR service directory exists in legacy — GDPR features may be spread across CustomerService
- No dedicated observability — only ILogger (DB) and KeepAliveTask
- Authentication is custom FormsAuthenticationService, not ASP.NET Identity
- Plugin system uses shadow copying and dynamic assembly loading
- Kendo UI used extensively in admin grids — needs replacement evaluation

## 2026-04-09 — Refine Iteration 2

### Gap Analysis: 25 Missing Service Interfaces
Compared all 117 service interfaces in `src/Libraries/Nop.Services/` against spec files. Found 25 interfaces not explicitly listed in any spec. All were sub-services belonging to existing spec domains:

- **svc-common.md**: Added `IAddressAttributeFormatter`, `IAddressAttributeParser`
- **svc-catalog.md**: Added `IProductAttributeParser`, `IProductAttributeFormatter`, `ICompareProductsService`, `ICategoryTemplateService`, `IManufacturerTemplateService`, `IProductTemplateService`
- **svc-orders.md**: Added `ICheckoutAttributeParser`, `ICheckoutAttributeFormatter`, `IOrderReportService`, `IRewardPointService`, `ICustomNumberFormatter`
- **svc-customers.md**: Added `ICustomerAttributeParser`, `ICustomerAttributeFormatter`, `ICustomerReportService`
- **svc-directory.md**: Added `IGeoLookupService`
- **svc-tax.md**: Added `ITaxCategoryService`
- **svc-seo.md**: Added `ISitemapGenerator`
- **svc-messages.md**: Added `ITokenizer`, `IEmailSender`
- **svc-helpers.md**: Added `IUserAgentHelper`
- **svc-events.md**: Added `ISubscriptionService`
- **xcut-authentication.md**: Added `IOpenAuthenticationService`, `IClaimsTranslator`, `IExternalAuthorizer`, `IExternalProviderAuthorizer`
- **nop-core-infrastructure.md**: Added `IMachineNameProvider`

### Gap Analysis: 5 Missing Controllers
- **Admin**: `JbimagesController` (79 LOC) + `RoxyFilemanController` (765 LOC) — TinyMCE file managers. Added plan item [5.82] to replace with modern file manager.
- **Public**: `BackwardCompatibility1XController` (271 LOC) + `BackwardCompatibility2XController` (113 LOC) — legacy URL redirects. Added plan item [5.28] for redirect middleware.
- **Public**: `KeepAliveController` (14 LOC) — simple health check. Added plan item [5.27] mapping to observability spec.

### Plan Item Growth
- Plan items: 165 → 168 (+3 new items)
- All 25 service interfaces now explicitly listed in their parent specs
- All plan items enriched with complete sub-service lists
