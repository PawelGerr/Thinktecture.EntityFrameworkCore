using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Thinktecture.TestDatabaseContext
{
   public class DbContextWithoutSchema : DbContext
   {
      public Action<ModelBuilder> ConfigureModel { get; set; }

      public DbContextWithoutSchema([NotNull] DbContextOptions<DbContextWithoutSchema> options)
         : base(options)
      {
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         base.OnModelCreating(modelBuilder);

         ConfigureModel?.Invoke(modelBuilder);
      }
   }
}
