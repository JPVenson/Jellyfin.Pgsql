using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Plugin.Pgsql.Migrations
{
    /// <summary>
    /// Stage 3: Create unique index after removing duplicate People records.
    /// </summary>
    public partial class RemoveDuplicatePeopleStage3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the temporary index
            migrationBuilder.DropIndex(
                name: "IX_Peoples_Name_PersonType_Temp",
                table: "Peoples");

            // Step 2: Make PersonType column NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "PersonType",
                table: "Peoples",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            // Step 3: Create unique index on Name, PersonType (replaces the old IX_Peoples_Name)
            #pragma warning disable CA1861
            migrationBuilder.CreateIndex(
                name: "IX_Peoples_Name_PersonType",
                table: "Peoples",
                columns: new[] { "Name", "PersonType" },
                unique: true);
            #pragma warning restore CA1861
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the unique index
            migrationBuilder.DropIndex(
                name: "IX_Peoples_Name_PersonType",
                table: "Peoples");

            // Make PersonType column nullable again
            migrationBuilder.AlterColumn<string>(
                name: "PersonType",
                table: "Peoples",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: false);
        }
    }
}
