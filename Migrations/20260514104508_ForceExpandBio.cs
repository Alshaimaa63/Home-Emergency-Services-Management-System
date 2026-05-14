using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Migrations
{
    /// <inheritdoc />
    public partial class ForceExpandBio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Bio",
                table: "AspNetUsers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "d26ba1aa-b5cf-4bcc-a596-ca2be8230d07", new DateTime(2026, 5, 14, 13, 45, 7, 327, DateTimeKind.Local).AddTicks(4446), "AQAAAAIAAYagAAAAEARFIMpZHE/pg5Vd53eDd2iBrsj0gI7a4L55CbEDLmj0Snv4fuJJj665AcEh5hyvUw==", "f10a81d2-9f46-42a8-a1a5-88c354463b4c" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Bio",
                table: "AspNetUsers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "59e124c3-1e91-426f-a1b5-4ed96510b803", new DateTime(2026, 5, 14, 13, 23, 39, 828, DateTimeKind.Local).AddTicks(7753), "AQAAAAIAAYagAAAAEJR7Kl0w2PTXoUaqH05MREDLhQQkCrlu7gGcmnqodHXDW4lUV7uCtNBA1fLH9BUIbA==", "87d1899a-1743-43fc-9e1d-42f52644f7d1" });
        }
    }
}
