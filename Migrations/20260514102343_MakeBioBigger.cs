using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Migrations
{
    /// <inheritdoc />
    public partial class MakeBioBigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "59e124c3-1e91-426f-a1b5-4ed96510b803", new DateTime(2026, 5, 14, 13, 23, 39, 828, DateTimeKind.Local).AddTicks(7753), "AQAAAAIAAYagAAAAEJR7Kl0w2PTXoUaqH05MREDLhQQkCrlu7gGcmnqodHXDW4lUV7uCtNBA1fLH9BUIbA==", "87d1899a-1743-43fc-9e1d-42f52644f7d1" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "bbd9455f-3271-444d-ac8c-665ef301e4d5", new DateTime(2026, 5, 14, 12, 36, 5, 992, DateTimeKind.Local).AddTicks(1044), "AQAAAAIAAYagAAAAEB8+/255qaMjKLrGohgLTqYklBFZqRz8A52b09fvJYP21LBp0u22LyFICunxoeu1dw==", "6a972e0d-2e04-4501-9696-4f5a2c664bf3" });
        }
    }
}
