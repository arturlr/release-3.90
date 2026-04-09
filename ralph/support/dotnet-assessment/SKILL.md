---
name: dotnet-assessment
description: >
  Use this skill whenever the user wants to assess, analyze, audit, or scan a .NET codebase for migration readiness or modernization planning. Trigger on requests like "assess this codebase", "run a migration assessment", "analyze our .NET app", "generate a scale report", "find legacy API usage", or "prepare for .NET 8 migration"

---

# Objective

Produces a complete migration readiness assessment for a .NET Framework codebase
targeting .NET 8. The deliverables are files on disk — not a chat summary.

---

## Pre-Requisites

1. Codebase has at least one .sln file with one or more .csproj or .vbproj projects
2. Projects target .NET Framework (v2.0-v4.8), .NET Core (1.0-3.1), or .NET 5-9 (skip .NET 10 -- no migration needed)
3. Project files are valid XML (MSBuild or SDK-style)
4. NuGet references exist via PackageReference or packages.config
5. Solution structure is readable for automated analysi

## Steps

Parse the .sln file to inventory projects. Assess each project individually and one at a time -- parsing, LOC analysis, complexity scoring, NuGet compatibility checking, and recommendation assignment -- then persist each project's full assessment to a dedicated JSON file in a `ATXASSESSMENT/` directory alongside the solution. This incremental, file-based approach prevents information loss for large solutions (e.g., 10+ projects) by ensuring intermediate results survive timeouts or context limits. After all projects are assessed and persisted, load the per-project JSON files to perform cross-project analysis (version conflicts, complexity adjustments). Finally, assemble the Markdown report from the persisted files rather than in-memory state. Validate the report against exit criteria.

### Phase 1: Build Assessment Plan

1. Invoke `parse_solution` with the .sln file path. Extract all project names, paths, and GUIDs. Record total project count.

2. Resolve each project's relative path to an absolute path. Produce a numbered list of every project in the solution. Print the full list using the format below so that no project is missed during assessment:

    | No. | Project Name | Relative Path |
    |-----|--------------|---------------|
    | 1   | ProjectA     | src/ProjectA/ProjectA.csproj |
    | 2   | ProjectB     | src/ProjectB/ProjectB.csproj |
    | ... | ...          | ... |

    Every project found in the .sln MUST appear in this table with a unique sequential number. The total row count must equal the total project count recorded in step 1. This numbered list is the authoritative assessment plan -- Phase 3 must process every entry in this table by its No.

### Phase 2: Prepare Assessment Infrastructure

3. Invoke the MCP server tool `get_loc_script` to retrieve the lines-of-code counting script content. Write the script content to a local temporary file (e.g., `/tmp/count_loc.sh`). Make the script executable: `chmod +x /tmp/count_loc.sh`. This script is reused across all projects.

4. Create an output directory for per-project assessment files alongside the solution file (e.g., `<solution_root>/ATXASSESSMENT/`). This directory will hold one JSON file per project, enabling incremental progress and fault tolerance.

### Phase 3: Assess Each Project Individually (One at a Time)

Process projects one at a time in the order determined by the numbered list from Phase 1 (No. 1, No. 2, No. 3, ...). For each project, announce which No. is being assessed (e.g., "Assessing project No. 3 of 12: ProjectName"), complete steps 5 through 13, then persist the result to disk before moving on to the next project. This ensures that if the solution has many projects (e.g., more than 10), no project-level information is lost due to timeouts or context limits. Every No. in the Phase 1 table must be processed -- do not skip any.

5. Invoke `parse_project_file` for the current project to extract framework version, NuGet references, and project-to-project references.

6. Run the LOC script against the current project folder: `/tmp/count_loc.sh <project_folder_path>`. Parse the script output to extract source code types (e.g., C#, XML, JSON, XAML, SQL, etc.) and their corresponding line counts. Store the per-project LOC summary (language breakdown and total lines).

7. Invoke `assess_project_complexity` with the parsed metadata JSON. The tool scores complexity based on:
   - 2 pts per incompatible package (prefixes: System.Web, Microsoft.Owin, Microsoft.AspNet, System.ServiceModel, System.Runtime.Remoting, System.EnterpriseServices, System.Configuration, System.Drawing, System.Windows.Forms, Microsoft.VisualBasic.Compatibility)
   - 2 pts if NuGet count > 20; 1 pt if > 10
   - 2 pts if project refs > 10; 1 pt if > 5
   - 2 pts if framework is v2.0-v4.0

   Rating: >= 6 CRITICAL, >= 4 HIGH, >= 2 MEDIUM, < 2 LOW.

8. Set target framework for the current project. Target must always exceed current version. Rules:
   - .NET 10: skip (no migration needed)
   - .NET Framework (v2.0-v4.8): target .NET 10. Floor: CRITICAL for v2.0-v3.5, HIGH for v4.0-v4.8
   - .NET Core (1.x-3.1): target .NET 10. Floor: MEDIUM for 1.x-2.x, LOW for 3.1
   - .NET 5-9: target .NET 10. Floor: LOW

   Keep the higher of the tool rating or the floor. Validate target > current.

9. Invoke `check_nuget_compatibility` for every NuGet package in the current project against its target framework. The tool returns: `is_compatible` (boolean), `compatible_versions` (array or null), `strategy` (one of "replace", "upgrade", or "rewrite"), and `target_packages` (array of objects each with `packageName` and `packageVersion`). Capture all returned fields for each package. Determine the Target Version and Target Package(s) from the response:
    - Strategy "upgrade": `target_packages` contains the same package with the highest compatible version. Use that version as Target Version. Target Package(s) is the original package name.
    - Strategy "replace": `target_packages` contains one or more replacement packages (different package names and versions). Target Version is taken from the replacement entries. Target Package(s) lists the replacement package name(s).
    - Strategy "rewrite": `target_packages` is empty. No compatible version or known replacement exists. Target Version is null. Target Package(s) is empty.

10. For each package in the current project, assign Complexity and Priority informed by the strategy:
    - Strategy "upgrade": Complexity is Low (minor/patch bump) or Medium (major version bump). Priority is Low or Medium accordingly.
    - Strategy "replace": Complexity is High (deprecated, known replacement available). Priority is High (deprecated with known replacement).
    - Strategy "rewrite": Complexity is Critical (fundamentally incompatible, no known replacement). Priority is Critical (blocking).

11. Build the Package Compatibility Table for the current project, sorted by Priority then Complexity (Critical first):

    | Package Name | Current Version | Target Package(s) | Target Version | Strategy | Complexity | Priority |

    - Target Package(s): For "upgrade", the original package name. For "replace", the comma-separated replacement package name(s). For "rewrite", "(none)".
    - Target Version: For "upgrade", the highest compatible version. For "replace", the version(s) from target_packages. For "rewrite", "(none)".

12. Set Next Step Agent Recommendation for the current project based on the strategies across all packages:
    - All packages have strategy "upgrade": "ATX.NET agent"
    - Mix of strategies (some "upgrade", some "replace" or "rewrite"): "ATX.NET agent, then ATX custom agent"
    - All packages have strategy "rewrite": "Kiro"

13. Persist the current project's full assessment to a JSON file at `<solution_root>/ATXASSESSMENT/<project_name>.json`. The JSON file must contain all collected data for this project:
    - `name`, `relative_path`, `current_framework`, `target_framework`
    - `project_dependencies` (array of referenced project names)
    - `nuget_dependencies` (array of NuGet dependency objects with `package_name`, `current_version`, `recommended_version`, `is_compatible`, `strategy`, `target_packages`)
    - `package_compatibility_table` (array of objects with `package_name`, `current_version`, `target_packages`, `target_version`, `strategy`, `complexity`, `priority`)
    - `migration_risks` (array of strings)
dependency count, Package Compatibility Table (correct columns including Strategy and Target Package(s), sorted), package metrics, Next Step Agent Recommendation, dependency table with target versions, blocking issues
    - Target > current for all assessed projects
    - Complexity floors match version rules
    - Solution-level complexity equals the highest project rating
    - All incompatible packages are documented with blocking issues
    - CRITICAL/HIGH projects have full blocking issue details
    - Cross-project conflicts are annotated and summarized
    - Solution package summary is present
    - Cross-project dependencies are documented
    - Actionable Next Steps has no manual estimates or instructions

## Validation

1. The final assessment report includes every project found in the solution -- none are missing
2. The solution has an overall complexity rating (the highest among all projects)
3. Each project has its own complexity rating
4. Each project has a recommended agent (ATX.NET agent, ATX.NET agent then ATX custom agent, or Kiro)
5. Each project lists its packages with compatibility details (current version, target version, strategy)
6. Each project lists its project-to-project dependencies
7. The report includes a migration ordering based on cross-project dependencies
