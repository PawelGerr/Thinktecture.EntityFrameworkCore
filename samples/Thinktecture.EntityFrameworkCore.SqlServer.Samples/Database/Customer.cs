using System;
using System.Collections.Generic;

namespace Thinktecture.Database
{
   public class Customer
   {
      public Guid Id { get; set; }
      public long RowVersion { get; set; }

      private List<Order> _orders;

      public List<Order> Orders
      {
         get => _orders ?? (_orders = new List<Order>());
         set => _orders = value;
      }
   }
}
