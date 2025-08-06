using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Plugin.Pgsql.Migrations
{
    /// <summary>
    /// Clean up duplicate People records and enforce unique constraints.
    /// </summary>
    public partial class FixPeoples : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Migrate Conductor and Composer names to Artists column before deletion
            migrationBuilder.Sql(@"
                UPDATE ""BaseItems""
                SET ""Artists"" = (
                    CASE
                        WHEN ""Artists"" IS NULL OR ""Artists"" = '' THEN
                            -- No existing artists, just add the new ones
                            (SELECT string_agg(p.""Name"", '|' ORDER BY p.sort_order)
                             FROM (SELECT DISTINCT p.""Name"",
                                          CASE p.""PersonType"" WHEN 'Conductor' THEN 1 WHEN 'Composer' THEN 2 END as sort_order
                                   FROM ""PeopleBaseItemMap"" pbim
                                   JOIN ""Peoples"" p ON pbim.""PeopleId"" = p.""Id""
                                   WHERE pbim.""ItemId"" = ""BaseItems"".""Id""
                                     AND p.""PersonType"" IN ('Conductor', 'Composer')) p)
                        ELSE
                            -- Merge with existing artists, avoiding duplicates
                            (SELECT CASE
                                WHEN new_names.names IS NULL THEN ""BaseItems"".""Artists""
                                ELSE ""BaseItems"".""Artists"" || '|' || new_names.names
                            END
                            FROM (
                                SELECT string_agg(p.""Name"", '|' ORDER BY p.sort_order) as names
                                FROM (SELECT DISTINCT p.""Name"",
                                             CASE p.""PersonType"" WHEN 'Conductor' THEN 1 WHEN 'Composer' THEN 2 END as sort_order
                                      FROM ""PeopleBaseItemMap"" pbim
                                      JOIN ""Peoples"" p ON pbim.""PeopleId"" = p.""Id""
                                      WHERE pbim.""ItemId"" = ""BaseItems"".""Id""
                                        AND p.""PersonType"" IN ('Conductor', 'Composer')
                                        AND ('|' || ""BaseItems"".""Artists"" || '|') NOT LIKE ('%|' || p.""Name"" || '|%')) p
                            ) new_names)
                    END
                )
                WHERE ""Id"" IN (
                    SELECT DISTINCT pbim.""ItemId""
                    FROM ""PeopleBaseItemMap"" pbim
                    JOIN ""Peoples"" p ON pbim.""PeopleId"" = p.""Id""
                    WHERE p.""PersonType"" IN ('Conductor', 'Composer')
                );
            ");

            // Step 2: Delete People with removed PersonType values (Artist, AlbumArtist, Composer, Conductor)
            // These are no longer valid PersonKind enum values as of commit 62ff759f
            migrationBuilder.Sql(@"
                DELETE FROM ""Peoples""
                WHERE ""PersonType"" IN ('Artist', 'AlbumArtist', 'Composer', 'Conductor');
            ");

            // Step 3: Update any NULL PersonType values to "Unknown" (default from PersonKind enum)
            migrationBuilder.Sql(@"
                UPDATE ""Peoples""
                SET ""PersonType"" = 'Unknown'
                WHERE ""PersonType"" IS NULL;
            ");

            // Step 4: Remove PeopleBaseItemMap records that reference deleted People records
            migrationBuilder.Sql(@"
                DELETE FROM ""PeopleBaseItemMap""
                WHERE ""PeopleId"" NOT IN (SELECT ""Id"" FROM ""Peoples"");
            ");

            // Step 4b: Consolidate remaining PeopleBaseItemMap to point to canonical People records
            // For each (Name, PersonType) combination, use the People record with MIN(Id)
            migrationBuilder.Sql(@"
                UPDATE ""PeopleBaseItemMap""
                SET ""PeopleId"" = (
                    SELECT p.""Id""
                    FROM ""Peoples"" p
                    WHERE p.""Name"" = (SELECT ""Name"" FROM ""Peoples"" WHERE ""Id"" = ""PeopleBaseItemMap"".""PeopleId"")
                      AND p.""PersonType"" = (SELECT ""PersonType"" FROM ""Peoples"" WHERE ""Id"" =
""PeopleBaseItemMap"".""PeopleId"")
                    ORDER BY p.""Id""::text
                    LIMIT 1
                )
                WHERE ""PeopleId"" NOT IN (
                    SELECT p2.""Id""
                    FROM ""Peoples"" p2
                    WHERE p2.""Id"" = (
                        SELECT p3.""Id""
                        FROM ""Peoples"" p3
                        WHERE p3.""Name"" = p2.""Name"" AND p3.""PersonType"" = p2.""PersonType""
                        ORDER BY p3.""Id""::text
                        LIMIT 1
                    )
                );
            ");

            // Step 5: Clean up duplicate PeopleBaseItemMap records after consolidation
            migrationBuilder.Sql(@"
                DELETE FROM ""PeopleBaseItemMap""
                WHERE ctid NOT IN (
                    SELECT MIN(ctid)
                    FROM ""PeopleBaseItemMap""
                    GROUP BY ""ItemId"", ""PeopleId""
                );
            ");

            // Step 6: Delete orphaned People records (CASCADE will handle any remaining mappings)
            migrationBuilder.Sql(@"
                DELETE FROM ""Peoples""
                WHERE ""Id"" NOT IN (
                    SELECT DISTINCT ""PeopleId""
                    FROM ""PeopleBaseItemMap""
                );
            ");

            // Step 7: Drop the old IX_Peoples_Name index (will be superseded by unique index)
            migrationBuilder.DropIndex(
                name: "IX_Peoples_Name",
                table: "Peoples");

            // Step 8: Make PersonType column NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "PersonType",
                table: "Peoples",
                type: "text",
                nullable: false,
                defaultValue: "Unknown",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            // Step 9: Delete orphaned Person BaseItems that don't have corresponding Peoples records
            migrationBuilder.Sql(@"
                DELETE FROM ""BaseItems""
                WHERE ""Type"" = 'MediaBrowser.Controller.Entities.Person'
                  AND ""Name"" NOT IN (SELECT DISTINCT ""Name"" FROM ""Peoples"");
            ");

            // Step 10: Create unique index on Name, PersonType (replaces the old IX_Peoples_Name)
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

            // Recreate the old IX_Peoples_Name index
            migrationBuilder.CreateIndex(
                name: "IX_Peoples_Name",
                table: "Peoples",
                column: "Name");

            // Make PersonType column nullable again
            migrationBuilder.AlterColumn<string>(
                name: "PersonType",
                table: "Peoples",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: false,
                oldDefaultValue: "Unknown");

            // Note: We cannot easily reverse the data cleanup. This is a destructive migration.
        }
    }
}
