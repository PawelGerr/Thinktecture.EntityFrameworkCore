using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.Data.EntityDataReaderTests
{
   public class GetFinishedEntities : IntegrationTestsBase
   {
      private readonly List<PropertyWithNavigations> _propertiesToRead = new();

      public GetFinishedEntities(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
         _propertiesToRead.Add(new PropertyWithNavigations(ArrangeDbContext.GetEntityType<TestEntity>().GetProperty(nameof(TestEntity.Column1)), Array.Empty<INavigation>()));
      }

      private EntityDataReader<T> CreateReader<T>(IEnumerable<T> entities, bool ensureReadEntitiesCollection)
         where T : class
      {
         return new(ActDbContext, new PropertyGetterCache(LoggerFactory!), entities, _propertiesToRead, ensureReadEntitiesCollection);
      }

      [Fact]
      public void Should_throw_if_ensureEntitiesCollectionAfterFinish_is_false()
      {
         CreateReader(Array.Empty<TestEntity>(), false)
            .Invoking(sut => sut.GetReadEntities())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("'Read entities' were not requested previously.");
      }

      [Fact]
      public void Should_return_the_same_collection_if_entities_is_list()
      {
         var entities = new List<TestEntity> { new() };

         CreateReader(entities, true).GetReadEntities()
                                     .Should().BeSameAs(entities);
      }

      [Fact]
      public void Should_return_empty_collection_if_reader_is_not_read_to_end()
      {
         var entities = new HashSet<TestEntity> { new() };

         CreateReader(entities, true).GetReadEntities()
                                     .Should().HaveCount(0);
      }

      [Fact]
      public void Should_return_the_new_collection_if_entities_is_not_list()
      {
         var entities = new HashSet<TestEntity> { new() };

         var reader = CreateReader(entities, true);

         while (reader.Read())
         {
         }

         reader.GetReadEntities()
               .Should().HaveCount(1)
               .And.BeEquivalentTo(entities);
      }
   }
}
