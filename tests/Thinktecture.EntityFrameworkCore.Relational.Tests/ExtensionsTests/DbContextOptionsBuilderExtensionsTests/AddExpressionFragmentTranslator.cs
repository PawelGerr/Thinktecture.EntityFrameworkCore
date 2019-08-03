using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;
using Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.ExtensionsTests.DbContextOptionsBuilderExtensionsTests
{
   public class AddExpressionFragmentTranslator : TestBase
   {
      public AddExpressionFragmentTranslator([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
      }

      [Fact]
      public void Should_register_translator_with_EF()
      {
         OptionBuilder.AddExpressionFragmentTranslator<TestTranslator>();
         DbContextWithSchema.Database.OpenConnection();
         DbContextWithSchema.Database.EnsureCreated();

         // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
         DbContextWithSchema.TestEntities.Where(e => e.Id == Guid.Empty).ToList();

         var translatorPlugins = DbContextWithSchema.GetService<IEnumerable<IExpressionFragmentTranslatorPlugin>>();
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
