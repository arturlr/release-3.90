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
task 7.7 is non-gating and 14 of its 66 tests need a database.

Without a database (48 pass, 18 skip — install-mode coverage, the host/container group, plus the
canaries):

```bash
docker run --rm -u "$(id -u):$(id -g)" -e DOTNET_CLI_HOME=/tmp -e HOME=/tmp \
  -v /home/artrodri/release-3.90:/workspace -w /workspace \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test src/Tests/Nop.Web.SmokeTests/Nop.Web.SmokeTests.csproj -c Debug --nologo
```

Tests that need an installed store `Assert.Ignore` with `"NOT EXERCISED: no database is
installed …"`, so the run is green and the gap is visible rather than silently passed.

### With a database (full storefront coverage — the `InstalledStoreTests` group stops skipping)

```bash
docker network create nopnet
docker run -d --name nopsql --network nopnet \
  -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<password>' -e MSSQL_PID=Developer \
  mcr.microsoft.com/mssql/server:2022-latest
```

Then install a store by POSTing the real installer form (this is what surfaced task 7.7's three
blockers, so it is worth doing rather than seeding the database directly). Run `Nop.Web` inside a
container **on `nopnet`**, and POST to `/install` with
`DataProvider=sqlserver`, `SqlConnectionInfo=sqlconnectioninfo_raw`,
`DatabaseConnectionString=Data Source=nopsql;Initial Catalog=<db>;User ID=sa;Password=<password>;TrustServerCertificate=True`,
`SqlServerCreateDatabase=true`, `InstallSampleData=true` and admin credentials. Sample data is needed
for the product-slug test. The installer calls `IWebHelper.RestartAppDomain()` on success, which stops
the host — that is expected.

Finally run `dotnet test` as above but add `--network nopnet` to the `docker run`, so the test
container can reach `nopsql`.

Two artifacts are left under `src/Presentation/Nop.Web/App_Data/` and are **gitignored**:
`Settings.txt` (the connection string the installer wrote) and `browscap.crawlersonly.xml`
(regenerated on demand). Delete `Settings.txt` to return to install mode.

### Proving the harness can fail

The suite is only evidence if it has been shown to go red. `HarnessCanaryTests` is `[Explicit]` and
contains one deliberately-false assertion per mechanism the suite relies on:

```bash
dotnet test src/Tests/Nop.Web.SmokeTests/Nop.Web.SmokeTests.csproj \
  --filter "FullyQualifiedName~HarnessCanaryTests"
```

**All four must report Failed.** If any passes, the corresponding group of real assertions cannot be
trusted. (Task 7.7 shipped three; the deferral 7.3-4 / 7.7-1 fix added a fourth for the
`/__smoke/action` probe.)
