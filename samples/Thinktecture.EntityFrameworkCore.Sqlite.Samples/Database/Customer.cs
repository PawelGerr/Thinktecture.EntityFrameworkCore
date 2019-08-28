using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Thinktecture.Database
{
   public class Customer
   {
      public Guid Id { get; set; }

      private List<Order> _orders;

      [NotNull]
      // ReSharper disable once UnusedMember.Global
      public List<Order> Orders
      {
         get => _orders ?? (_orders = new List<Order>());
         set => _orders = value;
      }
   }
}
