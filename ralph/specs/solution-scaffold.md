# Solution Scaffold

## Bounded Context
.NET 10 solution structure — solution file, project files, Directory.Build.props, global.json, CI/CD configuration.

## Legacy Source
- `src/NopCommerce.sln` — 31 projects
- Individual `.csproj` files (old-style MSBuild format targeting .NET Framework 4.5.1)

## Key Entities
- Solution file (.sln)
- SDK-style project files (.csproj)
- `Directory.Build.props` — shared build properties
- `global.json` — SDK version pinning
- `nuget.config` — package sources

## External Dependencies
- .NET 10 SDK
- All NuGet packages for target stack

## Migration Notes
- **Decision**: Create from scratch
- New SDK-style .csproj files (not converted from old format)
- Project structure: `src/Core/`, `src/Data/`, `src/Services/`, `src/Web/`, `src/Plugins/`, `tests/`
- `Directory.Build.props`: nullable enable, implicit usings, treat warnings as errors, common package versions
- `global.json`: pin to .NET 10 SDK version
- CI/CD: GitHub Actions or Azure DevOps pipeline for build/test/publish

## Acceptance Criteria
- [ ] Solution builds successfully with `dotnet build` on .NET 10 SDK
- [ ] `Directory.Build.props` enables nullable reference types and implicit usings for all projects
- [ ] `global.json` pins SDK version to .NET 10
- [ ] All projects use SDK-style .csproj format
- [ ] CI pipeline runs build and test on every commit
