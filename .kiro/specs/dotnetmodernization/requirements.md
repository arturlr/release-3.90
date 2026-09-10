# Requirements Document

## Introduction

This feature covers the complete port of the nopCommerce release-3.90 application from .NET Framework v4.5.1 (classic ASP.NET MVC 5, non-SDK MSBuild projects using `packages.config` and `app.config`) to .NET 10 (`net10.0`, SDK-style projects using `PackageReference`, ASP.NET Core MVC).

The migration MUST be exhaustive: every project, source file, project file, and configuration file in scope is to be updated with no shortcuts and no partial ports. The port MUST proceed in dependency order, beginning from the leaf project (`Nop.Core`) and moving outward to its dependents, so that each project is migrated only after all of its dependencies have been migrated.

Scope includes all Libraries (`Nop.Core`, `Nop.Data`, `Nop.Services`), all Presentation projects (`Nop.Web.Framework`, `Nop.Admin`, `Nop.Web`), all Plugins, and all Tests. The definition of done for each project is a clean compile as an SDK-style project targeting `net10.0`; passing tests at each stage is explicitly not the bar for stage completion.

## Glossary

- **Migration_System**: The overall process and tooling that ports projects from .NET Framework to .NET 10, as executed for this feature.
- **Source_Project**: Any project within `src/` that is part of the migration scope (`Nop.Core`, `Nop.Data`, `Nop.Services`, `Nop.Web.Framework`, `Nop.Admin`, `Nop.Web`, all Plugin projects, and all Test projects).
- **Leaf_Project**: The `Nop.Core` project, which has no dependencies on other Source_Projects.
- **Dependent_Project**: A Source_Project that references one or more other Source_Projects.
- **SDK_Style_Project**: A `.csproj` file using the .NET SDK project format (`<Project Sdk="...">`) with `<TargetFramework>net10.0</TargetFramework>` and NuGet dependencies expressed as `PackageReference` elements.
- **Classic_Project**: A non-SDK MSBuild `.csproj` file using `packages.config` for NuGet dependencies and `app.config`/`web.config` for configuration.
- **Migration_Order**: The dependency-ordered sequence: `Nop.Core` → `Nop.Data` → `Nop.Services` → `Nop.Web.Framework` → (`Nop.Admin`, `Nop.Web`) → Plugins → Tests. `Nop.Admin` and `Nop.Web` are sibling projects: neither references the other, both reference only `Nop.Web.Framework` and the Libraries, and therefore they may be migrated in either order once `Nop.Web.Framework` is migrated.
- **Clean_Compile**: A build of a Source_Project that completes with zero compiler errors.
- **System_Web_Dependency**: Any dependency on `System.Web`-based frameworks including `System.Web.Mvc`, `System.Web.Razor`, `System.Web.WebPages`, `System.Runtime.Caching`, `Autofac.Mvc5`, and related packages.
- **AspNetCore_Equivalent**: The ASP.NET Core replacement for a System_Web_Dependency (for example, ASP.NET Core MVC controllers, Razor views, middleware, and built-in dependency injection or Autofac's ASP.NET Core integration).
- **Legacy_Image_Dependency**: Any usage of the `ImageResizer` package (version 4.0.5, which has no `net10.0`-compatible release) or of `System.Drawing`/`System.Drawing.Common` types, which are Windows-only from .NET 6 onward and raise `PlatformNotSupportedException` on non-Windows platforms. Present in `Nop.Services/Media/PictureService.cs`, `Nop.Services/ExportImport/ExportManager.cs`, and `Nop.Web/Administration/Controllers/RoxyFilemanController.cs`.
- **Cross_Platform_Image_Library**: `SixLabors.ImageSharp`, the managed cross-platform image processing library selected to replace every Legacy_Image_Dependency.
- **Cross_Platform_Target_Framework**: The `net10.0` target framework moniker, which carries no operating-system suffix and therefore builds and runs on Windows, Linux, and macOS.

## Requirements

### Requirement 1: SDK-Style Conversion to .NET 10

**User Story:** As a maintainer, I want every in-scope project converted to the SDK-style project format targeting .NET 10, so that the entire solution builds on a modern, supported runtime.

#### Acceptance Criteria

1. THE Migration_System SHALL convert each Source_Project from a Classic_Project to an SDK_Style_Project.
2. THE Migration_System SHALL set the `TargetFramework` of each Source_Project to `net10.0`.
3. THE Migration_System SHALL replace each `packages.config` file with `PackageReference` elements in the corresponding SDK_Style_Project.
4. THE Migration_System SHALL migrate configuration from each `app.config` and `web.config` file to the .NET 10 configuration model.
5. WHERE a Source_Project contains files rendered obsolete by the SDK_Style_Project format, THE Migration_System SHALL remove those files.

### Requirement 2: Leaf-First Dependency-Ordered Migration

**User Story:** As a maintainer, I want the migration to follow strict dependency order starting from the leaf project, so that each project is ported against already-migrated dependencies.

#### Acceptance Criteria

1. THE Migration_System SHALL migrate the Leaf_Project before any Dependent_Project.
2. THE Migration_System SHALL follow the Migration_Order sequence: `Nop.Core`, then `Nop.Data`, then `Nop.Services`, then `Nop.Web.Framework`, then `Nop.Admin` and `Nop.Web` in either order, then Plugins, then Tests.
3. IF a Dependent_Project is scheduled for migration before all of its referenced Source_Projects are migrated, THEN THE Migration_System SHALL defer the Dependent_Project until its referenced Source_Projects are migrated.
4. WHEN a Source_Project is migrated, THE Migration_System SHALL reference its dependency Source_Projects as already-migrated SDK_Style_Projects.

### Requirement 3: Clean Compile Per Stage

**User Story:** As a maintainer, I want each migrated project to compile cleanly before its dependents are migrated, so that build integrity is preserved throughout the port.

#### Acceptance Criteria

1. WHEN a Source_Project migration is completed, THE Migration_System SHALL produce a Clean_Compile for that Source_Project.
2. IF a Source_Project does not produce a Clean_Compile, THEN THE Migration_System SHALL resolve the compiler errors before migrating any Dependent_Project that references it.
3. THE Migration_System SHALL treat Clean_Compile, not test execution results, as the completion criterion for each Source_Project stage.

### Requirement 4: System.Web to ASP.NET Core Port

**User Story:** As a maintainer, I want all System.Web-based dependencies replaced with ASP.NET Core equivalents, so that the presentation layer runs on ASP.NET Core.

#### Acceptance Criteria

1. THE Migration_System SHALL port `Nop.Web` and `Nop.Web.Framework` to ASP.NET Core MVC.
2. THE Migration_System SHALL replace each System_Web_Dependency with its AspNetCore_Equivalent.
3. THE Migration_System SHALL convert MVC controllers to ASP.NET Core MVC controllers.
4. THE Migration_System SHALL convert Razor views to the ASP.NET Core Razor view engine.
5. THE Migration_System SHALL map dependency injection registrations to the ASP.NET Core dependency injection model or Autofac's ASP.NET Core integration.
6. THE Migration_System SHALL migrate request-pipeline behavior previously provided by `System.Web` modules and handlers to ASP.NET Core middleware.
7. THE Migration_System SHALL port `Nop.Admin` to ASP.NET Core MVC.
8. THE Migration_System SHALL convert every controller in `Nop.Admin` to an ASP.NET Core MVC controller.
9. THE Migration_System SHALL convert every Razor view in `Nop.Admin` to the ASP.NET Core Razor view engine.
10. THE Migration_System SHALL replace the `System.Web.Mvc` area registration of `Nop.Admin` with ASP.NET Core area routing that preserves the existing `Admin` area route prefix.
11. WHEN `Nop.Admin` is ported to ASP.NET Core MVC, THE Migration_System SHALL reference `Nop.Web.Framework` as an already-migrated SDK_Style_Project.

### Requirement 5: Complete Coverage, No Shortcuts

**User Story:** As a maintainer, I want the migration applied to every file across all projects with no shortcuts, so that no legacy code or configuration is left unmigrated.

#### Acceptance Criteria

1. THE Migration_System SHALL update every source file in each Source_Project that references APIs unavailable in `net10.0`.
2. THE Migration_System SHALL include all Libraries, all Presentation projects, all Plugins, and all Tests within the migration scope.
3. IF a source file uses an API that has no direct `net10.0` counterpart, THEN THE Migration_System SHALL replace the usage with a supported `net10.0` API.
4. THE Migration_System SHALL update the `NopCommerce.sln` solution file to reference the migrated SDK_Style_Projects.
5. WHERE a Plugin project depends on migrated Source_Projects, THE Migration_System SHALL migrate the Plugin project to an SDK_Style_Project targeting `net10.0`.
6. WHERE a Test project depends on migrated Source_Projects, THE Migration_System SHALL migrate the Test project to an SDK_Style_Project targeting `net10.0`.
7. THE Migration_System SHALL migrate `Nop.Admin` to an SDK_Style_Project targeting `net10.0`.
8. THE Migration_System SHALL replace every Legacy_Image_Dependency with the Cross_Platform_Image_Library.
9. THE Migration_System SHALL replace the Legacy_Image_Dependency usages in `Nop.Services/Media/PictureService.cs`, `Nop.Services/ExportImport/ExportManager.cs`, and `Nop.Web/Administration/Controllers/RoxyFilemanController.cs` with the Cross_Platform_Image_Library.
10. THE Migration_System SHALL set the `TargetFramework` of every Source_Project that previously contained a Legacy_Image_Dependency to the Cross_Platform_Target_Framework.
11. IF a Source_Project requires image processing behavior, THEN THE Migration_System SHALL supply that behavior through the Cross_Platform_Image_Library.
