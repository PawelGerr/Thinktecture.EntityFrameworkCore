using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Thinktecture.Database
{
   public class Product
   {
      public Guid Id { get; set; }

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
