namespace Thinktecture.Database;

public class Order
{
   public Guid Id { get; set; }
   public DateTime Date { get; set; }
   public string? Text { get; set; }
   public Guid CustomerId { get; set; }

#nullable disable
   public Customer Customer { get; set; }
#nullable enable

   private List<OrderItem>? _orderItems;

   // ReSharper disable once UnusedMember.Global
   public List<OrderItem> OrderItems
   {
      get => _orderItems ??= new List<OrderItem>();
      set => _orderItems = value;
   }
}
