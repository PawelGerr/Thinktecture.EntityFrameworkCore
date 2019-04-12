using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture
{
   public class TestBase
   {
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
         OptionBuilder = new DbContextOptionsBuilder<DbContextWithSchema>()
                         .UseInMemoryDatabase("TestDatabase")
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
   }
}
