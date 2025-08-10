# What to Do When a Task is Completed

1. Ensure all code builds and passes tests on both `net8.0` and `net9.0`.
2. Check for and resolve any analyzer warnings or code style violations.
3. Ensure new/changed code follows repository conventions (see style_and_conventions.md).
4. Add or update XML documentation for any public APIs.
5. If the change is performance-sensitive, add or update benchmarks in `samples/Thinktecture.EntityFrameworkCore.Benchmarks`.
6. If the change is user-facing, update samples and documentation as needed.
7. For new features or bug fixes:
   - Add/extend tests in `tests/**` (unit first, integration if needed)
   - Use `CollectExecutedCommands()` in tests to verify SQL if relevant
   - Ensure provider-specific code is handled for both SQL Server and SQLite
8. If dependencies were added, update `Directory.Packages.props` and reference in `.csproj` (no inline versions).
9. If the change affects CI/CD, update `azure-pipelines.yml` or scripts in `ci/`.
10. Commit and push changes, then create a pull request with a clear description and scope.
11. Ensure PR passes all CI checks before merging.
