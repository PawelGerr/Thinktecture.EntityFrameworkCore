using System;
using System.Collections.Generic;

namespace Thinktecture.Database;

public class Product
{
   public Guid Id { get; set; }

   private List<OrderItem>? _orderItems;

   // ReSharper disable once UnusedMember.Global
   public List<OrderItem> OrderItems
   {
      get => _orderItems ??= new List<OrderItem>();
      set => _orderItems = value;
   }
}