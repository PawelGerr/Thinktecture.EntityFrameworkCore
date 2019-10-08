using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Query;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators
{
   /// <summary>
   /// Plugin registering method translators.
   /// </summary>
   public class SqlServerMethodCallTranslatorPlugin : IMethodCallTranslatorPlugin
   {
      /// <inheritdoc />
      public IEnumerable<IMethodCallTranslator> Translators { get; }

      /// <summary>
      /// Initializes new instance of <see cref="SqlServerMethodCallTranslatorPlugin"/>.
      /// </summary>
      public SqlServerMethodCallTranslatorPlugin(SqlServerDbContextOptionsExtension extension)
      {
         if (extension == null)
            throw new ArgumentNullException(nameof(extension));

         var translators = new List<IMethodCallTranslator>();

         if (extension.AddRowNumberSupport)
            translators.Add(new SqlServerRowNumberTranslator());

         Translators = translators;
      }
   }
}
