using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Thinktecture.Database
{
   public class Order
   {
      public Guid Id { get; set; }
      public Guid CustomerId { get; set; }

      public Customer Customer { get; set; }

      private List<OrderItem> _orderItems;

      [NotNull]
      // ReSharper disable once UnusedMember.Global
      public List<OrderItem> OrderItems
      {
         get => _orderItems ?? (_orderItems = new List<OrderItem>());
         set => _orderItems = value;
      }
   }
}
