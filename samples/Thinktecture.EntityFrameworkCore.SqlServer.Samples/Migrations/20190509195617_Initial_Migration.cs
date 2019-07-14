using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   // ReSharper disable once InconsistentNaming
   // ReSharper disable once UnusedMember.Global
   public partial class Initial_Migration : DbSchemaAwareMigration
   {
      public Initial_Migration([CanBeNull] IDbContextSchema schema)
         : base(schema)
      {
      }

      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.CreateTable("Customers",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>(),
                                                  RowVersion = table.Column<byte[]>(rowVersion: true)
                                               },
                                      constraints: table => table.PrimaryKey("PK_Customers", x => x.Id));

         migrationBuilder.CreateTable("Products",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>()
                                               },
                                      constraints: table => table.PrimaryKey("PK_Products", x => x.Id));

         migrationBuilder.CreateTable("Orders",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>(),
                                                  CustomerId = table.Column<Guid>()
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
                                                  OrderId = table.Column<Guid>(),
                                                  ProductId = table.Column<Guid>(),
                                                  Count = table.Column<int>()
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

         migrationBuilder.CreateIndex("IX_OrderItems_ProductId", "OrderItems", "ProductId")
                         .IncludeColumns("OrderId", "Count");
         migrationBuilder.CreateIndex("IX_OrderItems_ProductId", "OrderItems", "ProductId").IfNotExists();
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
