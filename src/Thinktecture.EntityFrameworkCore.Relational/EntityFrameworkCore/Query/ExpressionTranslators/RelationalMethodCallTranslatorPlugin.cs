using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Query;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

/// <summary>
/// Plugin registering method translators.
/// </summary>
public sealed class RelationalMethodCallTranslatorPlugin : IMethodCallTranslatorPlugin
{
   /// <inheritdoc />
   public IEnumerable<IMethodCallTranslator> Translators { get; }

   /// <summary>
   /// Initializes new instance of <see cref="RelationalMethodCallTranslatorPlugin"/>.
   /// </summary>
   public RelationalMethodCallTranslatorPlugin(RelationalDbContextOptionsExtension extension)
   {
      if (extension == null)
         throw new ArgumentNullException(nameof(extension));

      var translators = new List<IMethodCallTranslator>();

      if (extension.AddRowNumberSupport)
         translators.Add(new RowNumberTranslator());

      Translators = translators;
   }
}