# Thinktecture.EntityFrameworkCore

[![Build Status](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_apis/build/status/Thinktecture.EntityFrameworkCore/Thinktecture.EntityFrameworkCore%20CI?branchName=master)](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_build/latest?definitionId=4&branchName=master)
![NuGet Downloads](https://img.shields.io/nuget/dt/Thinktecture.EntityFrameworkCore.Relational)


[![Thinktecture.EntityFrameworkCore.Relational](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.Relational.svg?label=Thinktecture.EntityFrameworkCore.Relational&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.Relational/)  
[![Thinktecture.EntityFrameworkCore.SqlServer](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.SqlServer.svg?label=Thinktecture.EntityFrameworkCore.SqlServer&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.SqlServer/)  
[![Thinktecture.EntityFrameworkCore.SqlServer.Testing](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.SqlServer.Testing.svg?label=Thinktecture.EntityFrameworkCore.SqlServer.Testing&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.SqlServer.Testing/)  
[![Thinktecture.EntityFrameworkCore.Sqlite](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.Sqlite.svg?label=Thinktecture.EntityFrameworkCore.Sqlite&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.Sqlite/)  
[![Thinktecture.EntityFrameworkCore.Sqlite.Testing](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.Sqlite.Testing.svg?label=Thinktecture.EntityFrameworkCore.Sqlite.Testing&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.Sqlite.Testing/)

These libraries extend [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) by a few features to make it easier to work with EF and for easier integration testing or to get more performance in some special cases.

The code and the documentation can be found on [Thinktecture.EntityFrameworkCore](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore)

Use the [repo on GitHub](https://github.com/PawelGerr/Thinktecture.EntityFrameworkCore) to create issues and feature requests.

## Performance
* [Temp-Tables](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/2/Temp-Tables)
* [Bulk-Insert](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/65/Bulk-Insert)
* [Bulk-Update](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/67/Bulk-Update)
* [Bulk-Upsert (Insert-or-Update)](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/69/Bulk-Upsert-(Insert-or-Update))
* [Truncate Tables](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/64/Truncate-Tables)

## Features
* [Collection Parameters (temp-tables *light*)](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/109/Collection-Parameters-(temp-tables-light)) (SQL Server)
* [Window Functions Support (RowNumber, Sum, Average, Min, Max)](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki?wikiVersion=GBwikiMaster&pagePath=/Features/Window%20Functions%20Support%20(RowNumber%2C%20Sum%2C%20Average%2C%20Min%2C%20Max)&pageId=14&_a=edit)
* [Nested (virtual) Transactions](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/40/Nested-(virtual)-Transactions)
* [Table Hints](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/71/Table-Hints-(SQL-Server)) (SQL Server)
* [Queries accross multiple databases](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/43/Queries-accross-multiple-databases) (SQL Server)
* [Changing default schema at runtime](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/6/Changing-default-schema-at-runtime)
* [If-Exists / If-Not-Exists checks in migrations](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/7/If-(Not-)Exists-checks-in-migrations) (SQL Server)

## Convenience
* [Extension method LeftJoin](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/4/Extension-method-LeftJoin)
* [Migrations: include-columns](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/9/Migrations-Include-columns) (SQL Server)
* [Migrations: identity column](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/28/Migrations-Identity-column) (SQL Server)
* [Migrations: (non-)clustered PK](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/29/Migrations-(Non-)Clustered-PK) (SQL Server)

## Integration Testing
* [Isolation of tests](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/12/Isolation-of-tests) (SQL Server, SQLite)

## Extensibility
* [Adding custom IRelationalTypeMappingSourcePlugin](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/26/Adding-custom-IRelationalTypeMappingSourcePlugin)
* [Adding custom IEvaluatableExpressionFilter](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/31/Adding-custom-IEvaluatableExpressionFilter)
