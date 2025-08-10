# Project Purpose
Thinktecture.EntityFrameworkCore is a set of libraries that extend Entity Framework Core with additional features for performance, convenience, and integration testing. It provides enhancements for bulk operations, temp-tables, window functions, table hints, and more, targeting SQL Server and SQLite. The project is maintained by Pawel Gerr and is available on both Azure DevOps and GitHub.

# Tech Stack
- .NET 8.0 and 9.0 (C#)
- Entity Framework Core (EF Core)
- SQL Server, SQLite
- Azure Pipelines for CI/CD
- PowerShell for CI scripting
- NuGet for package management
- xUnit for testing
- BenchmarkDotNet for performance benchmarks

# Codebase Structure
- `src/`: Main library source code, organized by feature and database provider (BulkOperations, Relational, Sqlite, SqlServer, Testing)
- `tests/`: Unit and integration tests for each main library
- `samples/`: Sample and benchmark projects for usage demonstration
- `ci/`: CI scripts (PowerShell)
- Solution and build configuration files at the root

# Key Components
- Bulk operations and performance features (BulkOperations)
- Relational and provider-specific extensions (Relational, Sqlite, SqlServer)
- Testing utilities and helpers
- Sample and benchmark projects for both SQL Server and SQLite

# Entry Points
- Libraries are consumed as NuGet packages
- Sample and benchmark projects have `Program.cs` as entry points

# Notable Guidelines
- Follows .NET and C# conventions (nullable enabled, implicit usings, LangVersion 13.0)
- Centralized package management
- Documentation and samples provided for all major features

See additional memories for commands, style, and task completion guidelines.