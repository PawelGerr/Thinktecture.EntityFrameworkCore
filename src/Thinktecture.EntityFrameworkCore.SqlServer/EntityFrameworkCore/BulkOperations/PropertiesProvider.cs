using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Gets properties provided via the constructor.
   /// </summary>
   public class PropertiesProvider : IPropertiesProvider
   {
      private readonly IReadOnlyList<PropertyInfo> _properties;

      /// <summary>
      /// Initializes new instance of <see cref="PropertiesProvider"/>.
      /// </summary>
      /// <param name="properties">Properties to return by the method <see cref="GetProperties"/>.</param>
      public PropertiesProvider([NotNull] IReadOnlyList<PropertyInfo> properties)
      {
         _properties = properties ?? throw new ArgumentNullException(nameof(properties));
      }

      /// <inheritdoc />
      public IReadOnlyList<PropertyInfo> GetProperties()
      {
         return _properties;
      }

      /// <summary>
      /// Extracts properties from the provided <paramref name="projection"/>.
      /// </summary>
      /// <param name="projection">Projection to extract properties from.</param>
      /// <typeparam name="T">Type of the entity.</typeparam>
      /// <returns>An instance of <see cref="IPropertiesProvider"/> containing properties extracted from <paramref name="projection"/>.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="projection"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentException">No properties couldn't be extracted.</exception>
      /// <exception cref="NotSupportedException">The <paramref name="projection"/> contains unsupported expressions.</exception>
      [NotNull]
      public static IPropertiesProvider From<T>([NotNull] Expression<Func<T, object>> projection)
      {
         if (projection == null)
            throw new ArgumentNullException(nameof(projection));

         var properties = new List<PropertyInfo>();
         ExtractProperties(properties, projection.Parameters[0], projection.Body);

         if (properties.Count == 0)
            throw new ArgumentException("The provided projection contains no properties.");

         return new PropertiesProvider(properties);
      }

      private static void ExtractProperties([NotNull] List<PropertyInfo> properties, [NotNull] ParameterExpression paramExpression, [NotNull] Expression body)
      {
         switch (body.NodeType)
         {
            case ExpressionType.Convert:
            case ExpressionType.Quote:
               ExtractProperties(properties, paramExpression, ((UnaryExpression)body).Operand);
               break;

            // entity => entity.Property;
            case ExpressionType.MemberAccess:
               properties.Add(ExtractProperty(paramExpression, (MemberExpression)body));
               break;

            // entity => new { Prop = entity.Property }
            case ExpressionType.New:
               ExtractProperties(properties, paramExpression, (NewExpression)body);
               break;

            default:
               throw new NotSupportedException($"The expression of type '{body.NodeType}' is not supported. Expression: {body}.");
         }
      }

      private static void ExtractProperties([NotNull] List<PropertyInfo> properties, [NotNull] ParameterExpression paramExpression, [NotNull] NewExpression newExpression)
      {
         foreach (var argument in newExpression.Arguments)
         {
            ExtractProperties(properties, paramExpression, argument);
         }
      }

      [NotNull]
      // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
      private static PropertyInfo ExtractProperty([NotNull] ParameterExpression paramExpression, [NotNull] MemberExpression memberAccess)
      {
         if (memberAccess.Expression != paramExpression)
            throw new NotSupportedException($"Complex projections are not supported. Current expression: {memberAccess}");

         if (memberAccess.Member is PropertyInfo propertyInfo)
            return propertyInfo;

         throw new NotSupportedException($"The projection must have properties only. Current expression: {memberAccess}");
      }
   }
}
