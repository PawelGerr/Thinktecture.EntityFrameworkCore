using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   // ReSharper disable once InconsistentNaming
   // ReSharper disable once UnusedMember.Global
   public partial class Initial_Migration : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.CreateTable("Customers",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>(nullable: false),
                                                  FirstName = table.Column<string>(maxLength: 100, nullable: false),
                                                  LastName = table.Column<string>(maxLength: 100, nullable: false)
                                               },
                                      constraints: table => table.PrimaryKey("PK_Customers", x => x.Id));

         migrationBuilder.CreateTable("Products",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>(nullable: false)
                                               },
                                      constraints: table => table.PrimaryKey("PK_Products", x => x.Id));

         migrationBuilder.CreateTable("Orders",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>(nullable: false),
                                                  Date = table.Column<DateTime>(nullable: false),
                                                  Text = table.Column<string>(nullable: true),
                                                  CustomerId = table.Column<Guid>(nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_Orders", x => x.Id);
                                                      table.ForeignKey(
                                                                       "FK_Orders_Customers_CustomerId",
                                                                       x => x.CustomerId,
                                                                       "Customers",
                                                                       "Id",
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });

         migrationBuilder.CreateTable("OrderItems",
                                      table => new
                                               {
                                                  OrderId = table.Column<Guid>(nullable: false),
                                                  ProductId = table.Column<Guid>(nullable: false),
                                                  Count = table.Column<int>(nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_OrderItems", x => new { x.OrderId, x.ProductId });
                                                      table.ForeignKey(
                                                                       "FK_OrderItems_Orders_OrderId",
                                                                       x => x.OrderId,
                                                                       "Orders",
                                                                       "Id",
                                                                       onDelete: ReferentialAction.Cascade);
                                                      table.ForeignKey(
                                                                       "FK_OrderItems_Products_ProductId",
                                                                       x => x.ProductId,
                                                                       "Products",
                                                                       "Id",
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });

         migrationBuilder.CreateIndex("IX_OrderItems_ProductId", "OrderItems", "ProductId");
         migrationBuilder.CreateIndex("IX_Orders_CustomerId", "Orders", "CustomerId");
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropTable("OrderItems");
         migrationBuilder.DropTable("Orders");
         migrationBuilder.DropTable("Products");
         migrationBuilder.DropTable("Customers");
      }
   }
}
