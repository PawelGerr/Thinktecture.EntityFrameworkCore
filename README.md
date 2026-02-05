# Thinktecture.EntityFrameworkCore

![Build](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/workflows/CI/badge.svg)
![NuGet Downloads](https://img.shields.io/nuget/dt/Thinktecture.EntityFrameworkCore.Relational)
![TestResults](https://gist.githubusercontent.com/PawelGerr/2311eb236eb4a66e7c422005bd498060/raw/badge.svg)

[![Thinktecture.EntityFrameworkCore.Relational](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.Relational.svg?label=Thinktecture.EntityFrameworkCore.Relational&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.Relational/)  
[![Thinktecture.EntityFrameworkCore.SqlServer](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.SqlServer.svg?label=Thinktecture.EntityFrameworkCore.SqlServer&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.SqlServer/)  
[![Thinktecture.EntityFrameworkCore.SqlServer.Testing](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.SqlServer.Testing.svg?label=Thinktecture.EntityFrameworkCore.SqlServer.Testing&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.SqlServer.Testing/)  
[![Thinktecture.EntityFrameworkCore.Sqlite](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.Sqlite.svg?label=Thinktecture.EntityFrameworkCore.Sqlite&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.Sqlite/)  
[![Thinktecture.EntityFrameworkCore.Sqlite.Testing](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.Sqlite.Testing.svg?label=Thinktecture.EntityFrameworkCore.Sqlite.Testing&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.Sqlite.Testing/)

These libraries extend [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) by a few features to make it easier to work with EF and for easier integration testing or to get more performance in some special cases.

## Performance
* [Temp-Tables](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Temp-Tables)
* [Bulk-Insert](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Bulk-Insert)
* [Bulk-Update](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Bulk-Update)
* [Bulk-Upsert (Insert-or-Update)](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Bulk-Upsert-(Insert-or-Update))
* [Truncate Tables](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Truncate-Tables)

## Features
* [Collection Parameters (temp-tables *light*)](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Collection-Parameters-(temp-tables-light)) (SQL Server)
* [Window Functions Support (RowNumber, Sum, Average, Min, Max)](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Window-Functions-Support-(RowNumber,-Sum,-Average,-Min,-Max))
* [Nested (virtual) Transactions](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Nested-(virtual)-Transactions)
* [Table Hints](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Table-Hints-(SQL-Server)) (SQL Server)
* [Queries accross multiple databases](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Queries-accross-multiple-databases) (SQL Server)
* [Changing default schema at runtime](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Changing-default-schema-at-runtime)
* [If-Exists / If-Not-Exists checks in migrations](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/If-(Not-)Exists-checks-in-migrations-(SQL-Server)) (SQL Server)

## Convenience
* [Migrations: include-columns](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Migrations-Include-columns-(SQL-Server)) (SQL Server)
* [Migrations: identity column](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Migrations-Identity-column-(SQL-Server)) (SQL Server)
* [Migrations: (non-)clustered PK](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Migrations-(Non-)Clustered-PK-(SQL-Server)) (SQL Server)

## Integration Testing
* [Isolation of tests](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Isolation-of-tests) (SQL Server, SQLite)

## Extensibility
* [Adding custom IRelationalTypeMappingSourcePlugin](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Adding-custom-IRelationalTypeMappingSourcePlugin)
* [Adding custom IEvaluatableExpressionFilter](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore/wiki/Adding-custom-IEvaluatableExpressionFilter)
