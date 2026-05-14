using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Migrations
{
    /// <inheritdoc />
    public partial class ExpandBioColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "bbd9455f-3271-444d-ac8c-665ef301e4d5", new DateTime(2026, 5, 14, 12, 36, 5, 992, DateTimeKind.Local).AddTicks(1044), "AQAAAAIAAYagAAAAEB8+/255qaMjKLrGohgLTqYklBFZqRz8A52b09fvJYP21LBp0u22LyFICunxoeu1dw==", "6a972e0d-2e04-4501-9696-4f5a2c664bf3" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "7284fa88-88f5-47a1-bec9-a11c81265017", new DateTime(2026, 5, 14, 12, 19, 5, 930, DateTimeKind.Local).AddTicks(2882), "AQAAAAIAAYagAAAAEDbNPtmDbm2fTolxz0hwUjAHoVgG8HnIEMAAnZjmmsgNRbkN2RVjANhtEf5GowXbvQ==", "1ec87923-0ae1-4475-9294-b5196b214d9e" });
        }
    }
}
