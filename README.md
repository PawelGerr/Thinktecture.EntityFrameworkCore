# Thinktecture.EntityFrameworkCore

![Build](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/workflows/CI/badge.svg)
![NuGet Downloads](https://img.shields.io/nuget/dt/Thinktecture.EntityFrameworkCore.Relational)
![TestResults](https://gist.githubusercontent.com/PawelGerr/2311eb236eb4a66e7c422005bd498060/raw/badge.svg)

[![Thinktecture.EntityFrameworkCore.Relational](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.Relational.svg?label=Thinktecture.EntityFrameworkCore.Relational&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.Relational/)  
[![Thinktecture.EntityFrameworkCore.SqlServer](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.SqlServer.svg?label=Thinktecture.EntityFrameworkCore.SqlServer&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.SqlServer/)  
[![Thinktecture.EntityFrameworkCore.PostgreSQL](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.PostgreSQL.svg?label=Thinktecture.EntityFrameworkCore.PostgreSQL&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.PostgreSQL/)  
[![Thinktecture.EntityFrameworkCore.MySql](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.MySql.svg?label=Thinktecture.EntityFrameworkCore.MySql&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.MySql/)  
[![Thinktecture.EntityFrameworkCore.Sqlite.Core](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.Sqlite.Core.svg?label=Thinktecture.EntityFrameworkCore.Sqlite.Core&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.Sqlite.Core/)  
[![Thinktecture.EntityFrameworkCore.Sqlite](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.Sqlite.svg?label=Thinktecture.EntityFrameworkCore.Sqlite&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.Sqlite/)  

These libraries extend [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) by a few features to make it easier to work with EF or to get more performance in some special cases.

## Supported Providers

| Provider | Package                                        |
|----------|------------------------------------------------|
| SQL Server | `Thinktecture.EntityFrameworkCore.SqlServer`   |
| PostgreSQL (Npgsql) | `Thinktecture.EntityFrameworkCore.PostgreSQL` |
| MySQL (Microting; MariaDB is not tested) | `Thinktecture.EntityFrameworkCore.MySql` |
| SQLite | `Thinktecture.EntityFrameworkCore.Sqlite`      |

## Performance
* [Temp-Tables](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Temp-Tables) (SQL Server, PostgreSQL, MySQL, SQLite)
* [Bulk-Insert](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Bulk-Insert) (SQL Server, PostgreSQL, MySQL, SQLite)
* [Bulk-Update](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Bulk-Update) (SQL Server, PostgreSQL, MySQL, SQLite)
* [Bulk-Upsert (Insert-or-Update)](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Bulk-Upsert-(Insert-or-Update)) (SQL Server, PostgreSQL, MySQL, SQLite)
* [Truncate Tables](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Truncate-Tables) (SQL Server, PostgreSQL, MySQL, SQLite)

## Features
* [Collection Parameters (temp-tables *light*)](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Collection-Parameters-(temp-tables-light)) (SQL Server, PostgreSQL, MySQL)
* [Window Functions Support (RowNumber, Sum, Average, Min, Max)](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Window-Functions-Support-(RowNumber,-Sum,-Average,-Min,-Max)) (SQL Server, PostgreSQL, MySQL, SQLite*)
* [Bitwise Aggregates (BitOr, BitAnd, BitXor)](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Bitwise-Aggregates-(PostgreSQL)) (PostgreSQL, MySQL)
* [Nested (virtual) Transactions](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Nested-(virtual)-Transactions)
* [Table Hints](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Table-Hints-(SQL-Server)) (SQL Server)
* [Queries across multiple databases](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Queries-accross-multiple-databases-(SQL-Server)) (SQL Server)
* [Changing default schema at runtime](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Changing-default-schema-at-runtime)
* [If-Exists / If-Not-Exists checks in migrations](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/If-(Not-)Exists-checks-in-migrations) (SQL Server, PostgreSQL)

> \* SQLite supports RowNumber and NTile; Sum, Average, Min, Max are available on SQL Server, PostgreSQL and MySQL.

MySQL bulk operations insert rows with multi-row `INSERT` statements by default. The registration `AddBulkOperationSupport(useBulkCopy: true)` switches every insert of the bulk operations to `MySqlBulkCopy` (`LOAD DATA LOCAL INFILE`). Bulk copy needs `AllowLoadLocalInfile=true` in the connection string and `local_infile=ON` on the server.
A MySQL bulk upsert returns the number of matched rows of its UPDATE plus the number of inserted rows.

## Convenience
* [Migrations: include-columns](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Migrations-Include-columns) (SQL Server, PostgreSQL)
* [Migrations: identity column](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Migrations-Identity-column) (SQL Server, PostgreSQL)
* [Migrations: (non-)clustered PK](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Migrations-(Non-)Clustered-PK-(SQL-Server)) (SQL Server)

## Best Practices
* [DbContext Lifetime and Change-Tracking](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/DbContext-Lifetime-and-Change-Tracking)

## Extensibility
* [Adding custom IRelationalTypeMappingSourcePlugin](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Adding-custom-IRelationalTypeMappingSourcePlugin)
* [Adding custom IEvaluatableExpressionFilter](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Adding-custom-IEvaluatableExpressionFilter)

## AI Coding Assistants

An AI agent skill teaches AI coding agents how to consume these packages. The skill lives in the repository [PawelGerr/agent-skills](https://github.com/PawelGerr/agent-skills) under `skills/thinktecture-entityframeworkcore`. Install it with the [`skills`](https://github.com/vercel-labs/skills) CLI:

``` bash
npx skills@latest add PawelGerr/agent-skills --skill thinktecture-entityframeworkcore
```

In Claude Code, you can install the skill as a plugin instead:

```
/plugin marketplace add PawelGerr/agent-skills
/plugin install thinktecture-entityframeworkcore@pawelgerr
```

The library is also indexed by Context7 (MCP) under the id `/pawelgerr/thinktecture.entityframeworkcore`. See [AI Coding Assistants](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/AI-Coding-Assistants) for details.
