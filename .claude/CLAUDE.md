# CLAUDE.md

This file provides guidance to Claude Code when working with this repository.
For detailed instructions see the specialized files in this directory:

- [CLAUDE-CODE-STYLE.md](CLAUDE-CODE-STYLE.md) - Code conventions, naming, visibility, null handling
- [CLAUDE-TESTING.md](CLAUDE-TESTING.md) - Test infrastructure, patterns, fixtures, how to write tests
- [CLAUDE-ARCHITECTURE.md](CLAUDE-ARCHITECTURE.md) - EF Core integration, package structure, extension patterns

## Quick Reference

### Build & Test

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-build
```

Run a single test project:
```powershell
dotnet test tests/Thinktecture.EntityFrameworkCore.SqlServer.Tests/ -c Release
```

### Tech Stack

| Aspect | Value |
|--------|-------|
| Target framework | `net10.0` |
| Language | C# 14.0, nullable enabled, implicit usings |
| EF Core | 10.0.2 (`Microsoft.EntityFrameworkCore.*`) |
| Testing | xUnit 2.9.3 + AwesomeAssertions 9.3.0 + NSubstitute 5.3.0 |
| Logging | Serilog 10.0.0 + Serilog.Sinks.XUnit |
| SQL Server tests | Testcontainers.MsSql 4.10.0 (Docker) or configured connection string |
| Root namespace | `Thinktecture` (set in Directory.Build.props) |
| Package versions | Centralized in `Directory.Packages.props` |

### Project Layout

```
src/                                    # Runtime packages (8 projects)
  Thinktecture.EntityFrameworkCore.Relational/          # Foundation layer
  Thinktecture.EntityFrameworkCore.BulkOperations/      # Provider-agnostic bulk ops
  Thinktecture.EntityFrameworkCore.SqlServer/            # SQL Server implementation
  Thinktecture.EntityFrameworkCore.SqlServer.Testing/    # SQL Server test utilities
  Thinktecture.EntityFrameworkCore.Sqlite.Core/          # SQLite foundation
  Thinktecture.EntityFrameworkCore.Sqlite/               # SQLite full package
  Thinktecture.EntityFrameworkCore.Sqlite.Testing/       # SQLite test utilities
  Thinktecture.EntityFrameworkCore.Testing/              # Shared testing infrastructure
tests/                                  # Test projects (6 projects, mirror src/)
  Thinktecture.EntityFrameworkCore.TestHelpers/          # Shared test entities & DbContext
samples/                                # Sample apps and benchmarks
```

### Key Configuration Files

| File | Purpose |
|------|---------|
| `Thinktecture.EntityFrameworkCore.slnx` | Solution file (17 projects) |
| `Directory.Build.props` | Root MSBuild props (version, TFM, lang version) |
| `Directory.Packages.props` | Centralized NuGet versions |
| `src/Directory.Build.props` | Source Link, symbol packages |
| `tests/Directory.Build.props` | Test dependencies, warning suppressions, global usings |
| `global.json` | .NET SDK 10.0.0, rollForward: latestMajor |

### Package Dependency Graph

```
Relational (foundation)
  └─ BulkOperations (abstractions)
       ├─ SqlServer (SQL Server implementation)
       │    └─ SqlServer.Testing
       └─ Sqlite.Core (SQLite foundation)
            └─ Sqlite (full SQLite package)
                 └─ Sqlite.Testing
Testing (shared test infrastructure, used by *.Testing packages)
```

## Critical Rules

1. **Always async with CancellationToken** - Every I/O method must accept `CancellationToken cancellationToken = default`
2. **Server-evaluable queries** - Never force client evaluation; keep operations in `IQueryable<T>`
3. **No raw SQL concatenation** - Use `FromSqlInterpolated` or parameterized helpers
4. **Internal by default** - Only make types `public` when needed across package boundaries
5. **Null checks** - Use `ArgumentNullException.ThrowIfNull()` for parameters
6. **XML documentation** - All public APIs must have XML docs (`GenerateDocumentationFile` is enabled)
7. **No version numbers in PackageReference** - Versions are centralized in `Directory.Packages.props`
8. **File-scoped namespaces** - All files use `namespace Thinktecture...;` syntax

## CI/CD

- GitHub Actions on every push (`.github/workflows/main.yml`)
- Build/test on Ubuntu with `dotnet build -c Release` / `dotnet test -c Release --no-build`
- SQL Server tests use Testcontainers (env var `UseSqlServerContainer=true`)
- NuGet packages published to nuget.org on git tags
- Test results written to `test-results/<TargetFramework>/` as TRX files
- Claude Code integrated for PR reviews (`.github/workflows/claude-code-review.yml`)
