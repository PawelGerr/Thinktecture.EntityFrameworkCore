using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   internal class CompositeTempTableEntityPropertiesProvider : IEntityPropertiesProvider
   {
      private readonly IEntityPropertiesProvider? _mainPropertiesProvider;
      private readonly IEntityPropertiesProvider? _keyPropertiesProvider;

      public CompositeTempTableEntityPropertiesProvider(
         IEntityPropertiesProvider? mainPropertiesProvider,
         IEntityPropertiesProvider? keyPropertiesProvider)
      {
         _mainPropertiesProvider = mainPropertiesProvider;
         _keyPropertiesProvider = keyPropertiesProvider;
      }

      public IReadOnlyList<IProperty> GetProperties(IEntityType entityType)
      {
         IReadOnlyList<IProperty> properties;

         if (_mainPropertiesProvider is null || (properties = _mainPropertiesProvider.GetProperties(entityType)).Count == 0)
            return Array.Empty<IProperty>();

         return properties.Union(_keyPropertiesProvider.GetKeyProperties(entityType)).ToList();
      }
   }
}
