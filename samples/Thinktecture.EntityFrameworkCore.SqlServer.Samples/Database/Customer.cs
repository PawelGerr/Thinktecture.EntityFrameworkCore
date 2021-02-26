using System;
using System.Collections.Generic;

namespace Thinktecture.Database
{
   public class Customer
   {
      public Guid Id { get; private set; }
      public string FirstName { get; private set; }
      public string LastName { get; private set; }
      public long RowVersion { get; private set; }

      private List<Order>? _orders;

      // ReSharper disable once UnusedMember.Global
      public List<Order> Orders
      {
         get => _orders ??= new List<Order>();
         set => _orders = value;
      }

#nullable disable
      private Customer()
      {
      }
#nullable enable

      public Customer(Guid id, string firstName, string lastName)
      {
         Id = id;
         FirstName = firstName;
         LastName = lastName;
      }

      public override string ToString()
      {
         return $"{{ CustomerId='{Id}', FirstName='{FirstName}', LastName='{LastName}', RowVersion={RowVersion} }}";
      }
   }
}
