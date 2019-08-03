using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Moq;

namespace Thinktecture.ExtensionsTests.DbContextOptionsBuilderExtensionsTests
{
   // ReSharper disable once ClassNeverInstantiated.Global
   public class TestTranslator : IExpressionFragmentTranslator
   {
      public Mock<IExpressionFragmentTranslator> TranslatorMock { get; } = new Mock<IExpressionFragmentTranslator>();

      /// <inheritdoc />
      public Expression Translate(Expression expression)
      {
         return TranslatorMock.Object.Translate(expression);
      }
   }
}
