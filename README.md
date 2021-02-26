# Thinktecture.EntityFrameworkCore

[![Build Status](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_apis/build/status/Thinktecture.EntityFrameworkCore/Thinktecture.EntityFrameworkCore%20CI?branchName=master)](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_build/latest?definitionId=4&branchName=master)

[![Thinktecture.EntityFrameworkCore.Relational](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.Relational.svg?label=Thinktecture.EntityFrameworkCore.Relational&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.Relational/)  
[![Thinktecture.EntityFrameworkCore.SqlServer](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.SqlServer.svg?label=Thinktecture.EntityFrameworkCore.SqlServer&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.SqlServer/)  
[![Thinktecture.EntityFrameworkCore.SqlServer.Testing](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.SqlServer.Testing.svg?label=Thinktecture.EntityFrameworkCore.SqlServer.Testing&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.SqlServer.Testing/)  
[![Thinktecture.EntityFrameworkCore.Sqlite](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.Sqlite.svg?label=Thinktecture.EntityFrameworkCore.Sqlite&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.Sqlite/)  
[![Thinktecture.EntityFrameworkCore.Sqlite.Testing](https://img.shields.io/nuget/vpre/Thinktecture.EntityFrameworkCore.Sqlite.Testing.svg?label=Thinktecture.EntityFrameworkCore.Sqlite.Testing&maxAge=3600)](https://www.nuget.org/packages/Thinktecture.EntityFrameworkCore.Sqlite.Testing/)


Thies library extends Entity Framework Core by a few features to make it easier to work with EF and for easier integration testing or to get more performance in some special cases.

The code and the documentation can be found on [Thinktecture.EntityFrameworkCore](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore)

Use this repo to create issues and feature requests.


## Performance
* [Temp-Tables](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/2/Temp-Tables)
* [Bulk-Delete](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/63/Bulk-Delete)
* [Truncate Tables](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/64/Truncate-Tables)

## Features
* [RowNumber Support](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/14/RowNumber-Support)
* [Nested (virtual) Transactions](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/40/Nested-(virtual)-Transactions)
* [Queries accross multiple databases](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/43/Queries-accross-multiple-databases) (SQL Server)
* [Changing default schema at runtime](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/6/Changing-default-schema-at-runtime)
* [If-Exists / If-Not-Exists checks in migrations](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/7/If-(Not-)Exists-checks-in-migrations) (SQL Server)

## Convenience
* [Extension method LeftJoin](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/4/Extension-method-LeftJoin)
* [Migrations: include-columns](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/9/Migrations-Include-columns) (SQL Server)
* [Migrations: identity column](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/28/Migrations-Identity-column) (SQL Server)
* [Migrations: (non-)clustered PK](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/29/Migrations-(Non-)Clustered-PK) (SQL Server)

## Integration Testing
* [Base class for isolation of tests](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/12/Base-class-for-isolation-of-tests) (SQL Server, SQLite)

## Extensibility
* [Adding custom IRelationalTypeMappingSourcePlugin](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/26/Adding-custom-IRelationalTypeMappingSourcePlugin)
* [Adding custom IEvaluatableExpressionFilter](https://dev.azure.com/pawelgerr/Thinktecture.EntityFrameworkCore/_wiki/wikis/Thinktecture.EntityFrameworkCore.wiki/31/Adding-custom-IEvaluatableExpressionFilter)
