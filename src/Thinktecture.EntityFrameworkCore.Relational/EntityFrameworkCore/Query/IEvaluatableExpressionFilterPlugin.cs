using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Plugin for the <see cref="CompositeEvaluatableExpressionFilter{T}"/>.
   /// </summary>
   public interface IEvaluatableExpressionFilterPlugin
   {
      /// <summary>
      ///     Checks whether the given expression can be evaluated.
      /// </summary>
      /// <param name="expression"> The expression. </param>
      /// <param name="model"> The model. </param>
      /// <returns> True if the expression can be evaluated; false otherwise. </returns>
      bool? IsEvaluatableExpression(Expression expression, IModel model);
   }
}
