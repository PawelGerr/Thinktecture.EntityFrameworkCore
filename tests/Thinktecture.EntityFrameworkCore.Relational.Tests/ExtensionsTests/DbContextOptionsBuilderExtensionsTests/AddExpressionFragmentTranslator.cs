using System;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Moq;
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
      public void Should_throw_if_translator_is_null()
      {
         // ReSharper disable once AssignNullToNotNullAttribute
         OptionBuilder.Invoking(b => b.AddExpressionFragmentTranslator(null))
                      .Should().Throw<ArgumentNullException>();
      }

      [Fact]
      public void Should_register_translator_with_EF()
      {
         var translatorMock = new Mock<IExpressionFragmentTranslator>();
         OptionBuilder.AddExpressionFragmentTranslator(translatorMock.Object);
         DbContextWithSchema.Database.EnsureCreated();

         // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
         DbContextWithSchema.TestEntities.Where(e => e.Id == Guid.Empty).ToList();

         // 3 calls:
         //   e.Id == Guid.Empty
         //   e.Id
         //   Guid.Empty
         translatorMock.Verify(t => t.Translate(It.IsAny<Expression>()), Times.Exactly(3));
      }
   }
}
