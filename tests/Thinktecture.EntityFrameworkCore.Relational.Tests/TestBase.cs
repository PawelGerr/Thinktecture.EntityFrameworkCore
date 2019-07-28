using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

[assembly: SuppressMessage("ReSharper", "CA1063")]
[assembly: SuppressMessage("ReSharper", "CA1816")]
[assembly: SuppressMessage("ReSharper", "CA1822")]

namespace Thinktecture
{
   public class TestBase : IDisposable
   {
      private readonly SqliteConnection _connection;

      protected DbContextOptionsBuilder<DbContextWithSchema> OptionBuilder { get; }
      private DbContextWithSchema _ctx;

      // use different schemas because EF Core uses static cache
      private string _schema;
      private bool _isSchemaSet;

      protected string Schema
      {
         get
         {
            if (!_isSchemaSet && _schema == null)
               _schema = Guid.NewGuid().ToString();

            return _schema;
         }
         set
         {
            _schema = value;
            _isSchemaSet = true;
         }
      }

      [NotNull]
      protected DbContextWithSchema DbContextWithSchema => _ctx ?? (_ctx = new DbContextWithSchema(OptionBuilder.Options, Schema));

      protected TestBase()
      {
         _connection = new SqliteConnection("DataSource=:memory:");
         _connection.Open();

         OptionBuilder = new DbContextOptionsBuilder<DbContextWithSchema>()
                         .UseSqlite(_connection)
                         .ReplaceService<IModelCacheKeyFactory, DbSchemaAwareModelCacheKeyFactory>();
      }

      [NotNull]
      protected static DbContextWithSchema CreateContextWithSchema(string schema)
      {
         var options = new DbContextOptionsBuilder<DbContextWithSchema>().Options;
         return new DbContextWithSchema(options, schema);
      }

      [NotNull]
      protected DbContextWithoutSchema CreateContextWithoutSchema()
      {
         var options = new DbContextOptionsBuilder<DbContextWithoutSchema>().Options;
         return new DbContextWithoutSchema(options);
      }

      public virtual void Dispose()
      {
         _ctx?.Dispose();
         _connection.Dispose();
      }
   }
}
