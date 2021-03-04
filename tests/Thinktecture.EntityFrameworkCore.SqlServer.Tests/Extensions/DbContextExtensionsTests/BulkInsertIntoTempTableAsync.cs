using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.DbContextExtensionsTests
{
   // ReSharper disable InconsistentNaming
   [Collection("BulkInsertTempTableAsync")]
   public class BulkInsertIntoTempTableAsync : IntegrationTestsBase
   {
      public BulkInsertIntoTempTableAsync(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, true)
      {
      }

      [Fact]
      public async Task Should_insert_keyless_type()
      {
         ConfigureModel = builder => builder.ConfigureTempTableEntity<CustomTempTable>().Property(t => t.Column2).HasMaxLength(100).IsRequired();

         var entities = new List<CustomTempTable> { new(1, "value") };
         await using var query = await ActDbContext.BulkInsertIntoTempTableAsync(entities);

         var tempTable = await query.Query.ToListAsync();
         tempTable.Should()
                  .HaveCount(1).And
                  .BeEquivalentTo(new CustomTempTable(1, "value"));
      }

      [Fact]
      public async Task Should_insert_entityType_without_touching_real_table()
      {
         var entity = new TestEntity
                      {
                         Id = new Guid("577BFD36-21BC-4F9E-97B4-367B8F29B730"),
                         Name = "Name",
                         Count = 42,
                         ConvertibleClass = new ConvertibleClass(43)
                      };
         ArrangeDbContext.TestEntities.Add(entity);
         await ArrangeDbContext.SaveChangesAsync();

         var entities = new List<TestEntity> { entity };
         await using var query = await ActDbContext.BulkInsertIntoTempTableAsync(entities);

         var tempTable = await query.Query.ToListAsync();
         tempTable.Should()
                  .HaveCount(1).And
                  .BeEquivalentTo(new TestEntity
                                  {
                                     Id = new Guid("577BFD36-21BC-4F9E-97B4-367B8F29B730"),
                                     Name = "Name",
                                     Count = 42,
                                     ConvertibleClass = new ConvertibleClass(43)
                                  });
      }

      [Fact]
      public async Task Should_return_disposable_query()
      {
         await using var tempTableQuery = await ActDbContext.BulkInsertIntoTempTableAsync(Array.Empty<TestEntity>());
         tempTableQuery.Dispose();

         tempTableQuery.Awaiting(t => t.Query.ToListAsync())
                       .Should().Throw<SqlException>().WithMessage("Invalid object name '#TestEntities_1'.");
      }

      [Fact]
      public async Task Should_work_with_inlined_owned_type()
      {
         var testEntity = new TestEntityOwningInlineEntity
                          {
                             Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                             InlineEntity = new OwnedInlineEntity
                                            {
                                               IntColumn = 42,
                                               StringColumn = "value"
                                            }
                          };

         await using var tempTable = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity });

         var entities = await tempTable.Query.ToListAsync();

         entities.Should().HaveCount(1)
                 .And.BeEquivalentTo(new TestEntityOwningInlineEntity
                                     {
                                        Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                                        InlineEntity = new OwnedInlineEntity
                                                       {
                                                          IntColumn = 42,
                                                          StringColumn = "value"
                                                       }
                                     });
      }

      [Fact]
      public async Task Should_return_detached_entities_for_entities_with_a_primary_key()
      {
         var testEntity = new TestEntityOwningInlineEntity
                          {
                             Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                             InlineEntity = new OwnedInlineEntity
                                            {
                                               IntColumn = 42,
                                               StringColumn = "value"
                                            }
                          };

         await using var tempTable = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity });

         var entities = await tempTable.Query.ToListAsync();

         ActDbContext.Entry(entities[0]).State.Should().Be(EntityState.Detached);
      }

      [Fact]
      public async Task Should_properly_join_2_temp_table_having_inlined_owned_type()
      {
         var testEntity1 = new TestEntityOwningInlineEntity
                           {
                              Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                              InlineEntity = new OwnedInlineEntity
                                             {
                                                IntColumn = 42,
                                                StringColumn = "value"
                                             }
                           };
         var testEntity2 = new TestEntityOwningInlineEntity
                           {
                              Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                              InlineEntity = new OwnedInlineEntity
                                             {
                                                IntColumn = 43,
                                                StringColumn = "other"
                                             }
                           };

         await using var tempTable1 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity1 });
         await using var tempTable2 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity2 });

         var entities = await tempTable1.Query
                                        .Join(tempTable2.Query, e => e.Id, e => e.Id, (temp1, temp2) => new { temp1, temp2 })
                                        .ToListAsync();

         entities.Should().HaveCount(1).And.BeEquivalentTo(new { temp1 = testEntity1, temp2 = testEntity2 });

         entities = await tempTable1.Query
                                    .Join(tempTable2.Query, e => e.Id, e => e.Id, (temp1, temp2) => new { temp1, temp2 })
                                    .ToListAsync();

         entities.Should().HaveCount(1).And.BeEquivalentTo(new { temp1 = testEntity1, temp2 = testEntity2 });
      }

      [Fact]
      public async Task Should_not_mess_up_temp_tables_with_alternating_requests_without_disposing_previous_one()
      {
         var testEntity1 = new TestEntityOwningInlineEntity
                           {
                              Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                              InlineEntity = new OwnedInlineEntity
                                             {
                                                IntColumn = 42,
                                                StringColumn = "value"
                                             }
                           };
         var testEntity2 = new TestEntityOwningInlineEntity
                           {
                              Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                              InlineEntity = new OwnedInlineEntity
                                             {
                                                IntColumn = 43,
                                                StringColumn = "other"
                                             }
                           };

         await using var tempTable1_1 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity1 });
         await using var tempTable2_1 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity2 });
         await using var tempTable1_2 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity1 });
         await using var tempTable2_2 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity2 });

         tempTable1_1.Query.ToList().Should().HaveCount(1).And.BeEquivalentTo(testEntity1);
         tempTable1_2.Query.ToList().Should().HaveCount(1).And.BeEquivalentTo(testEntity1);
         tempTable2_1.Query.ToList().Should().HaveCount(1).And.BeEquivalentTo(testEntity2);
         tempTable2_2.Query.ToList().Should().HaveCount(1).And.BeEquivalentTo(testEntity2);
      }

      [Fact]
      public async Task Should_not_mess_up_temp_tables_with_alternating_requests_with_disposing_previous_one()
      {
         var testEntity1 = new TestEntityOwningInlineEntity
                           {
                              Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                              InlineEntity = new OwnedInlineEntity
                                             {
                                                IntColumn = 42,
                                                StringColumn = "value"
                                             }
                           };
         var testEntity2 = new TestEntityOwningInlineEntity
                           {
                              Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                              InlineEntity = new OwnedInlineEntity
                                             {
                                                IntColumn = 43,
                                                StringColumn = "other"
                                             }
                           };

         await using (var tempTable1 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity1 }))
         await using (var tempTable2 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity2 }))
         {
            tempTable1.Query.ToList().Should().HaveCount(1).And.BeEquivalentTo(testEntity1);
            tempTable2.Query.ToList().Should().HaveCount(1).And.BeEquivalentTo(testEntity2);
         }

         await using (var tempTable1 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity1 }))
         await using (var tempTable2 = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity2 }))
         {
            tempTable1.Query.ToList().Should().HaveCount(1).And.BeEquivalentTo(testEntity1);
            tempTable2.Query.ToList().Should().HaveCount(1).And.BeEquivalentTo(testEntity2);
         }
      }

      [Fact]
      public async Task Should_properly_join_real_table_with_temp_table_having_inlined_owned_type()
      {
         var realEntity = new TestEntityOwningInlineEntity
                          {
                             Id = new Guid("C0A98E8F-2715-4764-A02E-033FF5278B9B"),
                             InlineEntity = new OwnedInlineEntity
                                            {
                                               IntColumn = 42,
                                               StringColumn = "real"
                                            }
                          };
         ArrangeDbContext.Add(realEntity);
         await ArrangeDbContext.SaveChangesAsync();

         var tempEntity = new TestEntityOwningInlineEntity
                          {
                             Id = new Guid("C0A98E8F-2715-4764-A02E-033FF5278B9B"),
                             InlineEntity = new OwnedInlineEntity
                                            {
                                               IntColumn = 100,
                                               StringColumn = "other"
                                            }
                          };

         await using var tempTable = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { tempEntity });

         var entities = await tempTable.Query
                                       .Join(ActDbContext.TestEntitiesOwningInlineEntity, e => e.Id, e => e.Id, (temp, real) => new { temp, real })
                                       .ToListAsync();

         entities.Should().HaveCount(1).And.BeEquivalentTo(new { temp = tempEntity, real = realEntity });

         entities = await tempTable.Query
                                   .Join(ActDbContext.TestEntitiesOwningInlineEntity, e => e.Id, e => e.Id, (temp, real) => new { temp, real })
                                   .ToListAsync();

         entities.Should().HaveCount(1).And.BeEquivalentTo(new { temp = tempEntity, real = realEntity });
      }

      [Fact]
      public void Should_throw_if_required_inlined_owned_type_is_null()
      {
         var testEntity = new TestEntityOwningInlineEntity
                          {
                             Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                             InlineEntity = null!
                          };
         var testEntities = new[] { testEntity };

         ActDbContext.Awaiting(ctx => ctx.BulkInsertIntoTempTableAsync(testEntities))
                     .Should().Throw<InvalidOperationException>().WithMessage("Column 'InlineEntity_IntColumn' does not allow DBNull.Value.");
      }

      [Fact]
      public void Should_throw_for_entities_with_separated_owned_type()
      {
         var testEntity = new TestEntityOwningOneSeparateEntity
                          {
                             Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                             SeparateEntity = new OwnedSeparateEntity
                                              {
                                                 IntColumn = 42,
                                                 StringColumn = "value"
                                              }
                          };

         ActDbContext.Awaiting(sut => sut.BulkInsertIntoTempTableAsync(new[] { testEntity }))
                     .Should().Throw<NotSupportedException>()
                     .WithMessage("Bulk insert of separate owned types into temp tables is not supported. Properties of separate owned types: SeparateEntity.IntColumn, SeparateEntity.StringColumn");
      }

      [Fact]
      public async Task Should_work_for_entities_if_separated_owned_type_is_excluded()
      {
         var testEntity = new TestEntityOwningOneSeparateEntity
                          {
                             Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                             SeparateEntity = new OwnedSeparateEntity
                                              {
                                                 IntColumn = 42,
                                                 StringColumn = "value"
                                              }
                          };

         await using var tempTable = await ActDbContext.BulkInsertIntoTempTableAsync(new[] { testEntity }, e => e.Id);

         var entities = await tempTable.Query.ToListAsync();

         entities.Should().HaveCount(1)
                 .And.BeEquivalentTo(new TestEntityOwningOneSeparateEntity
                                     {
                                        Id = new Guid("3A1B2FFF-8E11-44E5-80E5-8C7FEEDACEB3"),
                                        SeparateEntity = null!
                                     });
      }
   }
}
