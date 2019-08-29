using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.ExtensionsTests.DbContextOptionsBuilderExtensionsTests
{
   public class AddExpressionFragmentTranslator : IntegrationTestsBase
   {
      public AddExpressionFragmentTranslator([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, MigrationExecutionStrategies.EnsureCreated)
      {
      }

      [Fact]
      public void Should_register_translator_with_EF()
      {
         ConfigureOptionsBuilder = builder => builder.AddExpressionFragmentTranslator<TestTranslator>();

         // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
         ActDbContext.TestEntities.Where(e => e.Id == Guid.Empty).ToList();

         var translatorPlugins = ActDbContext.GetService<IEnumerable<IExpressionFragmentTranslatorPlugin>>();
         var translators = translatorPlugins.Should().HaveCount(1).And
                                            .Subject.Single().Translators.ToList();
         var translatorMock = translators.Should().HaveCount(1).And
                                         .Subject.Single().Should().BeOfType<TestTranslator>()
                                         .Subject.TranslatorMock;

         // 3 calls:
         //   e.Id == Guid.Empty
         //   e.Id
         //   Guid.Empty
         translatorMock.Verify(t => t.Translate(It.IsAny<Expression>()), Times.Exactly(3));
      }
   }
}
