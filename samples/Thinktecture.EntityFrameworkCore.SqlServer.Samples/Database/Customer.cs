using System;
using System.Collections.Generic;

namespace Thinktecture.Database
{
   public class Customer
   {
      public Guid Id { get; set; }
      public long RowVersion { get; set; }

      private List<Order>? _orders;

      // ReSharper disable once UnusedMember.Global
      public List<Order> Orders
      {
         get => _orders ??= new List<Order>();
         set => _orders = value;
      }
   }
}
