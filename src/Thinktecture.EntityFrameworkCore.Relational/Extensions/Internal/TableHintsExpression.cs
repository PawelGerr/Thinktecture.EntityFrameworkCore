using System;
using System.Collections.Generic;
using System.Linq;
using Thinktecture.EntityFrameworkCore;

namespace Thinktecture.Internal
{
   /// <summary>
   /// Represents the table hints.
   /// </summary>
   public class TableHintsExpression : NonEvaluatableConstantExpression<IReadOnlyList<ITableHint>>
   {
      /// <inheritdoc />
      public TableHintsExpression(IReadOnlyList<ITableHint> value)
         : base(value)
      {
      }

      /// <inheritdoc />
      public override bool Equals(IReadOnlyList<ITableHint>? otherTableHints)
      {
         if (otherTableHints is null)
            return false;

         if (Value.Count != otherTableHints.Count)
            return false;

         foreach (var tableHint in Value)
         {
            if (!otherTableHints.Contains(tableHint))
               return false;
         }

         return true;
      }

      /// <inheritdoc />
      public override int GetHashCode()
      {
         var hashCode = new HashCode();

         foreach (var tableHint in Value)
         {
            hashCode.Add(tableHint);
         }

         return hashCode.ToHashCode();
      }
   }
}
