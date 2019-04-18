using System;
using System.Collections.Generic;

namespace Thinktecture.Database
{
   public class Order
   {
      public Guid Id { get; set; }
      public Guid CustomerId { get; set; }

      public Customer Customer { get; set; }

      private List<OrderItem> _orderItems;

      public List<OrderItem> OrderItems
      {
         get => _orderItems ?? (_orderItems = new List<OrderItem>());
         set => _orderItems = value;
      }
   }
}