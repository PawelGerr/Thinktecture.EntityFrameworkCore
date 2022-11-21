using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using Thinktecture.EntityFrameworkCore.Testing;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.SqlServerDbFunctionsExtensionsTests;

// ReSharper disable once InconsistentNaming
public class RowNumber : IntegrationTestsBase
{
   public RowNumber(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper, ITestIsolationOptions.SharedTablesAmbientTransaction)
   {
   }

   [Fact]
   public void Generates_RowNumber_with_orderby_and_one_column()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "2", RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(e.Name))
                                            })
                               .ToList();

      result.First(t => t.Name == "1").RowNumber.Should().Be(1);
      result.First(t => t.Name == "2").RowNumber.Should().Be(2);
   }

   [Fact]
   public void Generates_RowNumber_with_orderby_and_one_column_generic_approach()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "2", RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var propertyName = nameof(TestEntity.Name);

      var query = ActDbContext.TestEntities;
      var result = AppendSelect(query, propertyName, new { Name = String.Empty, RowNumber = 0L }).ToList();

      result.First(t => t.Name == "1").RowNumber.Should().Be(1);
      result.First(t => t.Name == "2").RowNumber.Should().Be(2);
   }

   private static IQueryable<TResult> AppendSelect<T, TResult>(IQueryable<T> query, string propertyName, TResult anonymousTypeSample)
   {
      var testEntityType = typeof(T);
      var propertyInfo = testEntityType.GetProperty(propertyName) ?? throw new Exception($"Property '{propertyName}' not found.");

      var returnType = new { Name = String.Empty, RowNumber = 0L }.GetType();
      var returnTypeCtor = returnType.GetConstructor(new[] { typeof(string), typeof(long) })
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
   public void Generates_RowNumber_with_orderby_and_one_struct_column()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18CF65B3-F53D-4F45-8DF5-DD62DCC8B2EB"), RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("28CF65B3-F53D-4F45-8DF5-DD62DCC8B2EB"), RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Id,
                                               RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(e.Id))
                                            })
                               .ToList();

      result.First(t => t.Id == new Guid("18CF65B3-F53D-4F45-8DF5-DD62DCC8B2EB")).RowNumber.Should().Be(1);
      result.First(t => t.Id == new Guid("28CF65B3-F53D-4F45-8DF5-DD62DCC8B2EB")).RowNumber.Should().Be(2);
   }

   [Fact]
   public void Generates_RowNumber_with_orderby_desc_and_one_column()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "2", RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               RowNumber = EF.Functions.RowNumber(EF.Functions.OrderByDescending(e.Name))
                                            })
                               .ToList();

      result.First(t => t.Name == "1").RowNumber.Should().Be(2);
      result.First(t => t.Name == "2").RowNumber.Should().Be(1);
   }

   [Fact]
   public void Generates_RowNumber_with_orderby_and_two_columns()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", Count = 1, RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "1", Count = 2, RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Count,
                                               RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(e.Name).ThenBy(e.Count))
                                            })
                               .ToList();

      result.First(t => t.Count == 1).RowNumber.Should().Be(1);
      result.First(t => t.Count == 2).RowNumber.Should().Be(2);
   }

   [Fact]
   public void Generates_RowNumber_with_orderby_desc_and_two_columns()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", Count = 1, RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "1", Count = 2, RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Count,
                                               RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(e.Name).ThenByDescending(e.Count))
                                            })
                               .ToList();

      result.First(t => t.Count == 1).RowNumber.Should().Be(2);
      result.First(t => t.Count == 2).RowNumber.Should().Be(1);
   }

   [Fact]
   public void Generates_RowNumber_with_partitionby_and_orderby_and_one_column()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "2", RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               RowNumber = EF.Functions.RowNumber(e.Name, EF.Functions.OrderBy(e.Name))
                                            })
                               .ToList();

      result.First(t => t.Name == "1").RowNumber.Should().Be(1);
      result.First(t => t.Name == "2").RowNumber.Should().Be(1);
   }

   [Fact]
   public void Generates_RowNumber_with_partitionby_and_orderby_and_two_columns()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", Count = 1, RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "1", Count = 2, RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Count,
                                               RowNumber = EF.Functions.RowNumber(e.Name, e.Count,
                                                                                  EF.Functions.OrderBy(e.Name).ThenBy(e.Count))
                                            })
                               .ToList();

      result.First(t => t.Count == 1).RowNumber.Should().Be(1);
      result.First(t => t.Count == 2).RowNumber.Should().Be(1);
   }

   [Fact]
   public void Throws_if_RowNumber_contains_NewExpression()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", Count = 1, RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "1", Count = 2, RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var query = ActDbContext.TestEntities
                              .Select(e => new
                                           {
                                              e.Count,
                                              RowNumber = EF.Functions.RowNumber(new { e.Name, e.Count },
                                                                                 EF.Functions.OrderBy(e.Name).ThenBy(e.Count))
                                           });
      query.Invoking(q => q.ToList()).Should().Throw<NotSupportedException>()
           .WithMessage("The EF function 'RowNumber' contains some expressions not supported by the Entity Framework. One of the reason is the creation of new objects like: 'new { e.MyProperty, e.MyOtherProperty }'.");
   }

   [Fact]
   public void Should_throw_if_accessing_RowNumber_not_within_subquery()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var query = ActDbContext.TestEntities
                              .Select(e => new
                                           {
                                              e.Name,
                                              RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(e.Name))
                                           })
                              .Where(i => i.RowNumber == 1);

      query.Invoking(q => q.ToList())
           .Should()
           .Throw<SqlException>();
   }

   [Fact]
   public void Should_be_able_to_fetch_whole_entity()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var query = ActDbContext.TestEntities
                              .Select(e => new
                                           {
                                              e,
                                              RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(e.Name))
                                           });

      var entities = query.ToList();
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
   public void Should_filter_for_RowNumber_if_accessing_within_subquery()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "2", RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(e.Name))
                                            })
                               .AsSubQuery()
                               .Where(i => i.RowNumber == 1)
                               .ToList();

      result.Should().HaveCount(1);
      result[0].Name.Should().Be("1");
   }

   [Fact]
   public void Should_support_columns_with_converters()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", ConvertibleClass = new ConvertibleClass(1), RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "1", ConvertibleClass = new ConvertibleClass(2), RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.ConvertibleClass,
                                               RowNumber = EF.Functions.RowNumber(e.Name,
                                                                                  EF.Functions.OrderBy(e.Name).ThenBy(e.ConvertibleClass))
                                            })
                               .ToList();

      result.Should().HaveCount(2);
      result.Select(e => e.ConvertibleClass?.Key).Should().BeEquivalentTo(new[] { 1, 2 });
   }

   [Fact]
   public void Should_support_conversion_to_underlying_column_type()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", ConvertibleClass = new ConvertibleClass(1), RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "1", ConvertibleClass = new ConvertibleClass(2), RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               ConvertibleClass = (int)e.ConvertibleClass!,
#pragma warning disable CS8604
                                               RowNumber = EF.Functions.RowNumber(e.Name, (int)e.ConvertibleClass, (int)e.Count,
                                                                                  EF.Functions.OrderBy(e.Name).ThenBy((int)e.ConvertibleClass).ThenBy((int)e.Count))
#pragma warning restore CS8604
                                            })
                               .ToList();

      result.Should().HaveCount(2);
      result.Select(e => e.ConvertibleClass).Should().AllBeOfType<int>().And.BeEquivalentTo(new[] { 1, 2 });
   }

   [Fact]
   public void Should_support_conversion_if_column_type_is_convertable_on_database()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", ConvertibleClass = new ConvertibleClass(1), RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "1", ConvertibleClass = new ConvertibleClass(2), RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               ConvertibleClass = (long)e.ConvertibleClass!,
#pragma warning disable CS8604
                                               RowNumber = EF.Functions.RowNumber(e.Name, (long)e.ConvertibleClass, (long)e.Count,
                                                                                  EF.Functions.OrderBy(e.Name).ThenBy((long)e.ConvertibleClass).ThenBy((long)e.Count))
#pragma warning restore CS8604
                                            })
                               .ToList();

      result.Should().HaveCount(2);
      result.Select(e => e.ConvertibleClass).Should().AllBeOfType<long>().And.BeEquivalentTo(new long[] { 1, 2 });
   }

   [Fact]
   public void Should_support_conversion_for_non_trivial_expressions()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BF"), Name = "1", ConvertibleClass = new ConvertibleClass(1), RequiredName = "RequiredName" });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70A"), Name = "1", ConvertibleClass = new ConvertibleClass(2), RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               ConvertibleClass = (long)e.ConvertibleClass!,
#pragma warning disable CS8604
                                               RowNumber = EF.Functions.RowNumber(e.Name, (long)e.ConvertibleClass + 1, (long)e.Count + 1,
                                                                                  EF.Functions.OrderBy(e.Name).ThenBy((long)e.ConvertibleClass + 1).ThenBy((long)e.Count + 1))
#pragma warning restore CS8604
                                            })
                               .ToList();

      result.Should().HaveCount(2);
      result.Select(e => e.ConvertibleClass).Should().AllBeOfType<long>().And.BeEquivalentTo(new long[] { 1, 2 });
   }
}
