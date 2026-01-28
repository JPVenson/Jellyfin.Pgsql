using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Plugin.Pgsql.Migrations
{
    /// <inheritdoc />
    public partial class Jellyfin101161 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PeopleBaseItemMap",
                table: "PeopleBaseItemMap");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "PeopleBaseItemMap",
                type: "text",
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PeopleBaseItemMap",
                table: "PeopleBaseItemMap",
                columns: ["ItemId", "PeopleId", "Role"]);

            migrationBuilder.AddForeignKey(
                name: "FK_BaseItems_BaseItems_ParentId",
                table: "BaseItems",
                column: "ParentId",
                principalTable: "BaseItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaseItems_BaseItems_ParentId",
                table: "BaseItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PeopleBaseItemMap",
                table: "PeopleBaseItemMap");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "PeopleBaseItemMap",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PeopleBaseItemMap",
                table: "PeopleBaseItemMap",
                columns: ["ItemId", "PeopleId"]);
        }
    }
}
