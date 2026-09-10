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
