using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseBioLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "7284fa88-88f5-47a1-bec9-a11c81265017", new DateTime(2026, 5, 14, 12, 19, 5, 930, DateTimeKind.Local).AddTicks(2882), "AQAAAAIAAYagAAAAEDbNPtmDbm2fTolxz0hwUjAHoVgG8HnIEMAAnZjmmsgNRbkN2RVjANhtEf5GowXbvQ==", "1ec87923-0ae1-4475-9294-b5196b214d9e" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "2fc0fea1-4e51-4d12-8e2d-3fc90973f4d2", new DateTime(2026, 5, 13, 1, 44, 45, 168, DateTimeKind.Local).AddTicks(34), "AQAAAAIAAYagAAAAEOfS7+vqnUy22utn4RC9BGLGAq1VG/QDtvZVlj29R8bbBJMBExYhakWaAVPnZzjDHg==", "2a1419ea-c0e8-4786-84e2-4a123762a309" });
        }
    }
}
