using System;
using System.Linq.Expressions;
using Thinktecture.Linq.Expressions;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extension methods for expressions.
   /// </summary>
   public static class RelationalExpressionExtensions
   {
      /// <summary>
      /// This method is for use with <see cref="ExpressionBodyExtractingVisitor"/> only!
      /// The visitor extracts the body from <paramref name="lambda"/> and rebinds it using the <paramref name="parameter"/>.
      /// </summary>
      /// <param name="lambda">Lambda expression to extract the body from.</param>
      /// <param name="parameter">New parameter to rebind the one of the <paramref name="lambda"/> with.</param>
      /// <typeparam name="TIn">Type of the argument of the <paramref name="lambda"/>.</typeparam>
      /// <typeparam name="TOut">Return type of the <paramref name="lambda"/>.</typeparam>
      /// <exception cref="InvalidOperationException">The method is called directly instead being used with <see cref="ExpressionBodyExtractingVisitor"/>.</exception>
      // ReSharper disable UnusedParameter.Global
      public static TOut ExtractBody<TIn, TOut>(this Expression<Func<TIn, TOut>> lambda, TIn parameter)
      {
         throw new InvalidOperationException($"This method is not intended to be used directly but with the '{nameof(ExpressionBodyExtractingVisitor)}' only.");
      }
      // ReSharper restore UnusedParameter.Global
   }
}
