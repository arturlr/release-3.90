# Build Environment (Verified)

Reference for all `net10.0` build and clean-compile work in this migration.

## Problem: the host cannot run .NET 10

- Host OS: Amazon Linux 2, **glibc 2.26**.
- .NET 10 requires **glibc 2.27+**. Microsoft dropped Amazon Linux 2 support after .NET 8.
- Available SDKs on the host:
  - .NET 8 SDK **8.0.413** at `~/.dotnet` — works.
  - .NET 10 SDK **10.0.202** at `/home/artrodri/.local/share/mise/installs/dotnet/10.0.202` (via mise) — **does not run**:

    ```
    Failed to load .../libcoreclr.so, error: /lib64/libm.so.6: version `GLIBC_2.27' not found
    ```

**Do not attempt `net10.0` builds with the host `dotnet` CLI.** The mise-installed SDK 10.0.202 is unusable here regardless of PATH ordering.

## Solution: containerized builds

Docker **25.0.16** is available and working. Use the official .NET 10 SDK image.

```bash
docker run --rm \
  -u "$(id -u):$(id -g)" \
  -e DOTNET_CLI_HOME=/tmp \
  -e HOME=/tmp \
  -v /home/artrodri/release-3.90:/workspace \
  -w /workspace \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet build src/Libraries/Nop.Core/Nop.Core.csproj -c Debug --nologo
```

Verified details:

| Detail | Note |
| --- | --- |
| `mcr.microsoft.com/dotnet/sdk:10.0` | Provides SDK **10.0.401** |
| `-u "$(id -u):$(id -g)"` | **Required** (currently `1000:1001`) so build outputs are owned by the user, not root |
| `-e DOTNET_CLI_HOME=/tmp -e HOME=/tmp` | **Required** — the mapped user has no home directory inside the container |
| Result | A `net10.0` project built with **0 warnings / 0 errors**; outputs confirmed owned by uid 1000 |
| `An issue was encountered verifying workloads` | Benign stderr noise — ignore |

Paths inside the container are rooted at `/workspace`, which maps to `/home/artrodri/release-3.90`.

## Guidance for clean-compile gates

- Every clean-compile gate task in `tasks.md` — **2.5, 3.3, 4.3, 6.6, 7.6, 8.8, 18.2**, and each plugin/test compile check — MUST use the containerized command above, substituting the target project or solution path.
  - 2.5 Nop.Core · 3.3 Nop.Data · 4.3 Nop.Services · 6.6 Nop.Web.Framework · 7.6 Nop.Web · 8.8 Nop.Admin · 18.2 full-solution build
- Task groups **7 (Nop.Web)** and **8 (Nop.Admin)** are sibling groups. Once the 6.6 gate passes, they may be built in either order or in parallel. Plugin migration requires **both** the 7.6 and 8.8 gates to report zero errors.
- Gate criterion is unchanged: **zero compiler ERRORS**. Warnings are non-blocking (requirement 3.3).
- When substituting paths, use the `/workspace`-relative form, not host absolute paths:
  - `src/Presentation/Nop.Web/Nop.Web.csproj`
  - `src/Presentation/Nop.Web/Administration/Nop.Admin.csproj` (nested deeper than the other presentation projects)


## Running the Nop.Web smoke check (task 7.7)

`src/Tests/Nop.Web.SmokeTests` boots the real `Nop.Web` host in-process through
`WebApplicationFactory<Nop.Web.Program>`. **It is deliberately NOT part of any clean-compile gate** —
task 7.7 is non-gating and 13 of its 161 tests need a database. (Counts as of task 8.8, which added
the `AdminUiRenderTests` and `PluginDiscoveryTests` fixtures plus the admin cases on the existing
`Deferral_7_3_4_*` invariants.)

**As of task 8.8 this project also builds `Nop.Admin` and `Nop.Plugin.SmokeProbe`.** Both are
**build-order-only** `ProjectReference`s (`ReferenceOutputAssembly="false"`), for reasons documented
at length in the csproj: `Nop.Admin.dll` must be in the test host's `BaseDirectory` and **absent from
`deps.json`** so the base-directory path load is the mechanism under test (runtime deferral 8.4-1),
and `Nop.Plugin.SmokeProbe.dll` must **not** reach the output directory at all or `WebAppTypeFinder`
would load it directly and bypass `PluginManager`'s shadow copy (deferral 8.2-1). The practical
consequence: `dotnet test` on this project now fails if `Nop.Admin` fails to compile.

Without a database (74 pass, 87 skip — the storefront and admin-render fixtures plus the canaries):

```bash
docker run --rm -u "$(id -u):$(id -g)" -e DOTNET_CLI_HOME=/tmp -e HOME=/tmp \
  -v /home/artrodri/release-3.90:/workspace -w /workspace \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test src/Tests/Nop.Web.SmokeTests/Nop.Web.SmokeTests.csproj -c Debug --nologo
```

Tests that need an installed store `Assert.Ignore` with `"NOT EXERCISED: no database is
installed …"`, so the run is green and the gap is visible rather than silently passed.

### With a database (full storefront AND admin coverage — 148 pass / 0 fail / 13 skip)

```bash
docker network create nopnet
docker run -d --name nopsql --network nopnet \
  -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<password>' -e MSSQL_PID=Developer \
  mcr.microsoft.com/mssql/server:2022-latest
```

Then install a store by POSTing the real installer form (this is what surfaced task 7.7's three
blockers and task 8.8's two, so it is worth doing rather than seeding the database directly). Run
`Nop.Web` inside a container **on `nopnet`**, and POST to `/install` with
`DataProvider=sqlserver`, `SqlConnectionInfo=sqlconnectioninfo_raw`,
`DatabaseConnectionString=Data Source=nopsql;Initial Catalog=<db>;User ID=sa;Password=<password>;TrustServerCertificate=True`,
`SqlServerCreateDatabase=true`, `InstallSampleData=true` and admin credentials. Sample data is needed
for the product-slug test. The installer calls `IWebHelper.RestartAppDomain()` on success, which stops
the host — that is expected.

**The admin credentials matter.** `AdminUiRenderTests` signs in as
`admin@gate88.local` / `Gate88Pass!` (constants on that fixture) and every test in it `Assert.Ignore`s
with an explicit message if it cannot. Install with those values, or update the two constants. Note
the skip condition is deliberately **"not authorised"** and not **"the page did not render"**: a
broken admin page is reported as a **failure**, because an earlier version keyed off
`GET /Admin/ == 200` and silently skipped the whole fixture when a revert experiment broke rendering.

Finally run `dotnet test` as above but add `--network nopnet` to the `docker run`, so the test
container can reach `nopsql`.

Two artifacts are left under `src/Presentation/Nop.Web/App_Data/` and are **gitignored**:
`Settings.txt` (the connection string the installer wrote) and `browscap.crawlersonly.xml`
(regenerated on demand). Delete `Settings.txt` to return to install mode.

### Note on files the fixtures plant (added at 8.5, extended at 8.8)

`AdminStaticAssetTests` plants a `.bak` under `Administration/db_backups/` and a `.zip` under
`Administration/Content/Roxy_Fileman/tmp/`, so its refusal assertions are made against files that
really exist rather than against trivially-true absences. Both carry the `8_5_smoke_planted` marker.

`AdminUiRenderTests` plants `Content/Images/uploaded/8_8_smoke_planted.png` (a real PNG copied from
the repository) so Roxy Fileman has something to make a thumbnail of.

`PluginDiscoveryTests` plants a whole plugin — `Plugins/Nop.Plugin.SmokeProbe/` with the built
assembly and a generated `Description.txt` — **before** the host starts, because
`PluginManager.Initialize()` runs once from `Program.Main`. `src/Presentation/Nop.Web/Plugins/*` is
already gitignored, so nothing there can be committed by accident.

All of them are removed in `OneTimeTearDown`. A crashed run leaves an obviously-named file that shows
up in `git status` (or, for the plugin, under the gitignored `Plugins/`) rather than silently; the
shadow copy at `Plugins/bin/Nop.Plugin.SmokeProbe.dll` may survive because the assembly is loaded
into the test process, and is safe to delete. None of these fixtures needs `Nop.Admin.dll` to be
*routable* — the static-file provider resolves against the filesystem — but the admin-render and
plugin fixtures do need the copies described above.

### Proving the harness can fail

The suite is only evidence if it has been shown to go red. `HarnessCanaryTests` is `[Explicit]` and
contains one deliberately-false assertion per mechanism the suite relies on:

```bash
dotnet test src/Tests/Nop.Web.SmokeTests/Nop.Web.SmokeTests.csproj \
  --filter "FullyQualifiedName~HarnessCanaryTests"
```

**All eight must report Failed.** If any passes, the corresponding group of real assertions cannot be
trusted. (Task 7.7 shipped three; the deferral 7.3-4 / 7.7-1 fix added a fourth for the
`/__smoke/action` probe; task 8.2 added a fifth for the view-location expander; task 8.5 added a
sixth for the admin static-asset assertions; task 8.8 added a seventh for the `/__smoke/adminarea`
probe and an eighth for admin-authenticated page fetches.)


## Running the Nop.Admin imaging tests (task 8.6)

`src/Tests/Nop.Admin.Tests` drives the real `Nop.Admin.Controllers.RoxyFilemanController` through a
test subclass — real files on disk, a real `DefaultHttpContext`, real encoded bytes read back — so
the SixLabors.ImageSharp port that replaced `System.Drawing` is verified by execution rather than by
compilation. It needs **no database and no host**: it does not use `WebApplicationFactory` and never
boots `Nop.Web`.

```bash
docker run --rm -u "$(id -u):$(id -g)" -e DOTNET_CLI_HOME=/tmp -e HOME=/tmp \
  -v /home/artrodri/release-3.90:/workspace -w /workspace \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test src/Tests/Nop.Admin.Tests/Nop.Admin.Tests.csproj -c Debug --nologo
```

**53 passed / 0 failed / 0 skipped.** Nothing is mocked and nothing skips.

Running it on Linux is the point, not an accident: the `Bitmap`/`Graphics` pipeline these tests
replaced throws `TypeInitializationException` → `DllNotFoundException: Unable to load shared library
'libgdiplus'` on this platform, so a green run *is* the cross-platform assertion.
`Task_8_6_these_tests_are_running_on_a_platform_where_the_System_Drawing_pipeline_could_not` reports
the platform explicitly and `Assert.Ignore`s on Windows rather than passing vacuously.

Like `Nop.Web.SmokeTests`, this project is **not** in `NopCommerce.sln` (task 18.1 owns the solution
file — deferral 7.7-2) and must **not** become part of a clean-compile gate. The same applies to
`src/Tests/Nop.Plugin.SmokeProbe`, added at task 8.8 (deferral 8.8-2) — though that one is always
built anyway, via a build-order `ProjectReference` from `Nop.Web.SmokeTests`.

### Proving the harness can fail

```bash
dotnet test src/Tests/Nop.Admin.Tests/Nop.Admin.Tests.csproj \
  --filter "FullyQualifiedName~HarnessCanaryTests"
```

**All four must report Failed.** They guard the four mechanisms the suite rests on: that the
controller harness really captures response bytes (a `DefaultHttpContext` whose `Response.Body` is
left as `Stream.Null` discards writes silently, which would make every imaging assertion vacuous),
that the `MapPath` assertions compare against a real resolution, that the colour-parity assertions
discriminate, and that the assembly-reference scan really reads the built assembly.
