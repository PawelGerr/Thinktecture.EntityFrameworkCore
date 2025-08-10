# Suggested Commands for Development

## Build
- `dotnet build` — Builds the solution (Thinktecture.EntityFrameworkCore.slnx)

## Test
- `dotnet test tests/**/*.csproj` — Runs all tests

## Pack
- `dotnet pack -c Release` — Creates NuGet packages

## Run Samples/Benchmarks
- `dotnet run --project samples/Thinktecture.EntityFrameworkCore.SqlServer.Samples`
- `dotnet run --project samples/Thinktecture.EntityFrameworkCore.Sqlite.Samples`
- `dotnet run --project samples/Thinktecture.EntityFrameworkCore.Benchmarks`

## CI/CD
- Azure Pipelines: defined in `azure-pipelines.yml`
- PowerShell CI script: `ci/ci.ps1`

## Version Suffix (for releases)
- Run PowerShell: `. ./ci/ci.ps1; Set-VersionSuffixOnTag <dir> <branch>`

## Utilities (Windows)
- Use PowerShell for scripting
- Standard git commands for version control

See Readme.md and azure-pipelines.yml for more details.