using System.Linq.Expressions;
using MySqlConnector;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.MySqlDbFunctionsExtensionsTests;

// MySQL specifics:
// - ROW_NUMBER() is BIGINT UNSIGNED, the provider casts it to a signed value so that it can be read as long.
// - A window function can't be used in WHERE; it has to be evaluated in a sub query (AsSubQuery), error 3593.
// - NULL is the smallest value, i.e. NULLs come first in an ascending order.
// ReSharper disable once InconsistentNaming
[Collection(MySqlTestCollection.NAME)]
public sealed class RowNumber : MySqlIntegrationTestsBase
{
   public RowNumber(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_generate_RowNumber_with_orderby_and_one_column()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "2", RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(e.Name))
                                                  })
                                     .ToListAsync();

      result.First(t => t.Name == "1").RowNumber.Should().Be(1);
      result.First(t => t.Name == "2").RowNumber.Should().Be(2);
   }

   [Fact]
   public async Task Should_generate_RowNumber_with_orderby_and_one_column_generic_approach()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "2", RequiredName = "RequiredName" });

      var propertyName = nameof(TestEntity.Name);

      var query = ActDbContext.TestEntities;
      var result = await AppendSelect(query, propertyName, new { Name = String.Empty, RowNumber = 0L }).ToListAsync();

      result.First(t => t.Name == "1").RowNumber.Should().Be(1);
      result.First(t => t.Name == "2").RowNumber.Should().Be(2);
   }

   private static IQueryable<TResult> AppendSelect<T, TResult>(IQueryable<T> query, string propertyName, TResult anonymousTypeSample)
   {
      var testEntityType = typeof(T);
      var propertyInfo = testEntityType.GetProperty(propertyName) ?? throw new Exception($"Property '{propertyName}' not found.");

      var returnType = anonymousTypeSample!.GetType();
      var returnTypeCtor = returnType.GetConstructor([typeof(string), typeof(long)])
                           ?? throw new Exception("Constructor not found.");

      var efFunctions = Expression.Constant(EF.Functions); // EF.Functions
      var extensionsType = typeof(RelationalDbFunctionsExtensions);
      var orderByMethod = extensionsType.GetMethod(nameof(RelationalDbFunctionsExtensions.OrderBy)) // EF.Functions.OrderBy<T>
                                        ?.MakeGenericMethod(propertyInfo.PropertyType)              // EF.Functions.OrderBy<string>
                          ?? throw new Exception("Method 'OrderBy' not found.");
      var rowNumberMethod = extensionsType.GetMethods()
                                          .Single(m => m.Name == nameof(RelationalDbFunctionsExtensions.RowNumber) && !m.IsGenericMethod); // EF.Functions.RowNumber

      var param = Expression.Parameter(testEntityType);                                     // e
      var nameAccessor = Expression.MakeMemberAccess(param, propertyInfo);                  // e.Name
      var orderByCall = Expression.Call(null, orderByMethod, efFunctions, nameAccessor);    // EF.Functions.OrderBy(e.Name)
      var rowNumberCall = Expression.Call(null, rowNumberMethod, efFunctions, orderByCall); // EF.Functions.RowNumber(EF.Functions.OrderBy(e.Name))
      var returnTypeCtorCall = Expression.New(returnTypeCtor, nameAccessor, rowNumberCall); // new { ... }

      var projection = Expression.Lambda<Func<T, TResult>>(returnTypeCtorCall, param);

      return query.Select(projection);
   }

   [Fact]
   public async Task Should_generate_RowNumber_with_orderby_and_one_struct_column()
   {
      await SeedAsync(new TestEntity { Id = new Guid("18CF65B3-F53D-4F45-8DF5-DD62DCC8B2EB"), RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("28CF65B3-F53D-4F45-8DF5-DD62DCC8B2EB"), RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(e.Id))
                                                  })
                                     .ToListAsync();

      result.First(t => t.Id == new Guid("18CF65B3-F53D-4F45-8DF5-DD62DCC8B2EB")).RowNumber.Should().Be(1);
      result.First(t => t.Id == new Guid("28CF65B3-F53D-4F45-8DF5-DD62DCC8B2EB")).RowNumber.Should().Be(2);
   }

   [Fact]
   public async Task Should_generate_RowNumber_with_orderby_desc_and_one_column()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "2", RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     RowNumber = EF.Functions.RowNumber(EF.Functions.OrderByDescending(e.Name))
                                                  })
                                     .ToListAsync();

      result.First(t => t.Name == "1").RowNumber.Should().Be(2);
      result.First(t => t.Name == "2").RowNumber.Should().Be(1);
   }

   [Fact]
   public async Task Should_generate_RowNumber_with_orderby_and_two_columns()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", Count = 1, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "1", Count = 2, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Count,
                                                     RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(e.Name).ThenBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.First(t => t.Count == 1).RowNumber.Should().Be(1);
      result.First(t => t.Count == 2).RowNumber.Should().Be(2);
   }

   [Fact]
   public async Task Should_generate_RowNumber_with_partitionby_and_orderby_and_one_column()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "2", RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     RowNumber = EF.Functions.RowNumber(e.Name, EF.Functions.OrderBy(e.Name))
                                                  })
                                     .ToListAsync();

      result.First(t => t.Name == "1").RowNumber.Should().Be(1);
      result.First(t => t.Name == "2").RowNumber.Should().Be(1);
   }

   [Fact]
   public async Task Should_filter_for_RowNumber_if_accessing_within_subquery()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "2", RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(e.Name))
                                                  })
                                     .AsSubQuery()
                                     .Where(i => i.RowNumber == 1)
                                     .ToListAsync();

      result.Should().HaveCount(1);
      result[0].Name.Should().Be("1");
   }

   [Fact]
   public async Task Should_throw_if_accessing_RowNumber_not_within_subquery()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" });

      var query = ActDbContext.TestEntities
                              .Select(e => new
                                           {
                                              e.Name,
                                              RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(e.Name))
                                           })
                              .Where(i => i.RowNumber == 1);

      // 3593: You cannot use the window function 'row_number' in this context
      var exception = await query.Awaiting(q => q.ToListAsync()).Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(3593);
   }

   [Fact]
   public async Task Should_be_able_to_fetch_whole_entity()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" });

      var query = ActDbContext.TestEntities
                              .Select(e => new
                                           {
                                              e,
                                              RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(e.Name))
                                           });

      var entities = await query.ToListAsync();
      entities.Should().HaveCount(1);
      entities[0].Should().BeEquivalentTo(new
                                          {
                                             e = new TestEntity
                                                 {
                                                    Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"),
                                                    Name = "1",
                                                    RequiredName = "RequiredName"
                                                 },
                                             RowNumber = 1
                                          });
   }

   [Fact]
   public async Task Should_support_columns_with_converters()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", ConvertibleClass = new ConvertibleClass(1), RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "1", ConvertibleClass = new ConvertibleClass(2), RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.ConvertibleClass,
                                                     RowNumber = EF.Functions.RowNumber(e.Name,
                                                                                        EF.Functions.OrderBy(e.Name).ThenBy(e.ConvertibleClass))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(2);
      result.Select(e => e.ConvertibleClass?.Key).Should().BeEquivalentTo([1, 2]);
      result.Single(e => e.ConvertibleClass!.Key == 1).RowNumber.Should().Be(1);
      result.Single(e => e.ConvertibleClass!.Key == 2).RowNumber.Should().Be(2);
   }

   [Fact]
   public async Task Should_support_conversion_to_underlying_column_type()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", ConvertibleClass = new ConvertibleClass(1), RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "1", ConvertibleClass = new ConvertibleClass(2), RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     ConvertibleClass = (int)e.ConvertibleClass!,
#pragma warning disable CS8604
                                                     RowNumber = EF.Functions.RowNumber(e.Name, (int)e.ConvertibleClass, e.Count,
                                                                                        EF.Functions.OrderBy(e.Name).ThenBy((int)e.ConvertibleClass).ThenBy(e.Count))
#pragma warning restore CS8604
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(2);
      result.Select(e => e.ConvertibleClass).Should().AllBeOfType<int>().And.BeEquivalentTo([1, 2]);
      result.Should().OnlyContain(e => e.RowNumber == 1);
   }

   [Fact]
   public async Task Should_support_conversion_if_column_type_is_convertable_on_database()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", ConvertibleClass = new ConvertibleClass(1), RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "1", ConvertibleClass = new ConvertibleClass(2), RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     ConvertibleClass = (long)e.ConvertibleClass!,
#pragma warning disable CS8604
                                                     RowNumber = EF.Functions.RowNumber(e.Name, (long)e.ConvertibleClass, (long)e.Count,
                                                                                        EF.Functions.OrderBy(e.Name).ThenBy((long)e.ConvertibleClass).ThenBy((long)e.Count))
#pragma warning restore CS8604
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(2);
      result.Select(e => e.ConvertibleClass).Should().AllBeOfType<long>().And.BeEquivalentTo(new long[] { 1, 2 });
      result.Should().OnlyContain(e => e.RowNumber == 1);
   }

   [Fact]
   public async Task Should_support_conversion_for_non_trivial_expressions()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", ConvertibleClass = new ConvertibleClass(1), RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "1", ConvertibleClass = new ConvertibleClass(2), RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     ConvertibleClass = (long)e.ConvertibleClass!,
#pragma warning disable CS8604
                                                     RowNumber = EF.Functions.RowNumber(e.Name, (long)e.ConvertibleClass + 1, (long)e.Count + 1,
                                                                                        EF.Functions.OrderBy(e.Name).ThenBy((long)e.ConvertibleClass + 1).ThenBy((long)e.Count + 1))
#pragma warning restore CS8604
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(2);
      result.Select(e => e.ConvertibleClass).Should().AllBeOfType<long>().And.BeEquivalentTo(new long[] { 1, 2 });
      result.Should().OnlyContain(e => e.RowNumber == 1);
   }

   [Fact]
   public async Task Should_generate_RowNumber_with_orderby_desc_and_two_columns()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", Count = 1, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "1", Count = 2, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Count,
                                                     RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(e.Name).ThenByDescending(e.Count))
                                                  })
                                     .ToListAsync();

      result.First(t => t.Count == 1).RowNumber.Should().Be(2);
      result.First(t => t.Count == 2).RowNumber.Should().Be(1);
   }

   [Fact]
   public async Task Should_generate_RowNumber_with_partitionby_and_orderby_and_two_columns()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", Count = 1, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "1", Count = 2, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Count,
                                                     RowNumber = EF.Functions.RowNumber(e.Name, e.Count,
                                                                                        EF.Functions.OrderBy(e.Name).ThenBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.First(t => t.Count == 1).RowNumber.Should().Be(1);
      result.First(t => t.Count == 2).RowNumber.Should().Be(1);
   }

   [Fact]
   public async Task Should_throw_if_RowNumber_contains_NewExpression()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", Count = 1, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "1", Count = 2, RequiredName = "RequiredName" });

      var query = ActDbContext.TestEntities
                              .Select(e => new
                                           {
                                              e.Count,
                                              RowNumber = EF.Functions.RowNumber(new { e.Name, e.Count },
                                                                                 EF.Functions.OrderBy(e.Name).ThenBy(e.Count))
                                           });
      await query.Awaiting(q => q.ToListAsync()).Should().ThrowAsync<NotSupportedException>()
                 .WithMessage("The window function contains some expressions not supported by the Entity Framework. One of the reason is the creation of new objects like: 'new { e.MyProperty, e.MyOtherProperty }'.");
   }

   [Fact]
   public async Task Should_generate_RowNumber_with_three_partitions_and_orderby_in_every_partition()
   {
      await SeedAsync(new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "a", RequiredName = "x", Count = 1 },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "a", RequiredName = "x", Count = 2 },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "a", RequiredName = "y", Count = 1 },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000004"), Name = "b", RequiredName = "x", Count = 1 },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000005"), Name = "b", RequiredName = "x", Count = 1 });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     RowNumber = EF.Functions.RowNumber(e.Name, e.RequiredName, e.Count, EF.Functions.OrderBy(e.Id))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      // Only the last two rows share a partition (b, x, 1).
      result.Select(e => e.RowNumber).Should().Equal(1, 1, 1, 1, 2);
   }

   [Fact]
   public async Task Should_generate_RowNumber_with_five_partitions()
   {
      await SeedAsync(new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "a", RequiredName = "x", Count = 1, NullableCount = 1, PropertyWithBackingField = 1 },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "a", RequiredName = "x", Count = 1, NullableCount = 1, PropertyWithBackingField = 1 },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "a", RequiredName = "x", Count = 1, NullableCount = 1, PropertyWithBackingField = 2 },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000004"), Name = "a", RequiredName = "x", Count = 1, NullableCount = null, PropertyWithBackingField = 2 },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000005"), Name = "a", RequiredName = "x", Count = 1, NullableCount = null, PropertyWithBackingField = 2 });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     RowNumber = EF.Functions.RowNumber(e.Name, e.RequiredName, e.Count, e.NullableCount, e.PropertyWithBackingField, EF.Functions.OrderBy(e.Id))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      // NULLs form one partition.
      result.Select(e => e.RowNumber).Should().Equal(1, 2, 1, 1, 2);
   }

   [Fact]
   public async Task Should_generate_RowNumber_with_nulls_first_in_ascending_order()
   {
      await SeedAsync(new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "a", RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = null, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Ascending = EF.Functions.RowNumber(EF.Functions.OrderBy(e.Name)),
                                                     Descending = EF.Functions.RowNumber(EF.Functions.OrderByDescending(e.Name))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Ascending).Should().Equal(2, 1);
      result.Select(e => e.Descending).Should().Equal(1, 2);
   }

   [Fact]
   public async Task Should_return_top_entries_per_group_using_a_subquery()
   {
      await SeedAsync(new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "a", RequiredName = "a1", Count = 10 },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "a", RequiredName = "a2", Count = 30 },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "a", RequiredName = "a3", Count = 20 },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000004"), Name = "b", RequiredName = "b1", Count = 5 },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000005"), Name = "b", RequiredName = "b2", Count = 15 });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.RequiredName,
                                                     RowNumber = EF.Functions.RowNumber(e.Name, EF.Functions.OrderByDescending(e.Count))
                                                  })
                                     .AsSubQuery()
                                     .Where(i => i.RowNumber <= 2)
                                     .OrderBy(i => i.RequiredName)
                                     .ToListAsync();

      result.Select(e => e.RequiredName).Should().Equal("a2", "a3", "b1", "b2");
      result.Select(e => e.RowNumber).Should().Equal(1, 2, 2, 1);
   }

   [Fact]
   public async Task Should_cast_RowNumber_to_signed()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" });

      await ActDbContext.TestEntities
                        .Select(e => EF.Functions.RowNumber(e.Name, EF.Functions.OrderBy(e.Id)))
                        .ToListAsync();

      ExecutedCommands.Should().Contain(c => c.Contains("CAST(ROW_NUMBER() OVER(PARTITION BY", StringComparison.Ordinal) && c.Contains("AS signed)", StringComparison.Ordinal));
   }

   private async Task SeedAsync(params TestEntity[] entities)
   {
      ArrangeDbContext.TestEntities.AddRange(entities);
      await ArrangeDbContext.SaveChangesAsync();
   }
}
