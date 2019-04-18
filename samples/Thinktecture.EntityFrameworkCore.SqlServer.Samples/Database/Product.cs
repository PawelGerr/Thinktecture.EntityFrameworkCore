using System;
using System.Collections.Generic;

namespace Thinktecture.Database
{
   public class Product
   {
      public Guid Id { get; set; }

      private List<OrderItem> _orderItems;

      public List<OrderItem> OrderItems
      {
         get => _orderItems ?? (_orderItems = new List<OrderItem>());
         set => _orderItems = value;
      }
   }
}