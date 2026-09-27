#pragma warning disable CA1861 // Avoid constant arrays as arguments
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Plugin.Pgsql.Migrations
{
    /// <inheritdoc />
    public partial class Jellyfin121 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserData_UserId",
                table: "UserData");

            migrationBuilder.DropIndex(
                name: "IX_Preferences_UserId_Kind",
                table: "Preferences");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_UserId_Kind",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_PeopleBaseItemMap_PeopleId",
                table: "PeopleBaseItemMap");

            migrationBuilder.DropIndex(
                name: "IX_MediaStreamInfos_StreamIndex",
                table: "MediaStreamInfos");

            migrationBuilder.DropIndex(
                name: "IX_MediaStreamInfos_StreamIndex_StreamType",
                table: "MediaStreamInfos");

            migrationBuilder.DropIndex(
                name: "IX_MediaStreamInfos_StreamIndex_StreamType_Language",
                table: "MediaStreamInfos");

            migrationBuilder.DropIndex(
                name: "IX_MediaStreamInfos_StreamType",
                table: "MediaStreamInfos");

            migrationBuilder.DropIndex(
                name: "IX_Devices_DeviceId",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_Id_Type_IsFolder_IsVirtualItem",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItemProviders_ProviderId_ProviderValue_ItemId",
                table: "BaseItemProviders");

            migrationBuilder.DropIndex(
                name: "IX_BaseItemImageInfos_ItemId",
                table: "BaseItemImageInfos");

            migrationBuilder.DropColumn(
                name: "Preference_Preferences_Guid",
                table: "Preferences");

            migrationBuilder.DropColumn(
                name: "Permission_Permissions_Guid",
                table: "Permissions");

            migrationBuilder.RenameColumn(
                name: "ExtraIds",
                table: "BaseItems",
                newName: "OriginalLanguage");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Preferences",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Permissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOriginal",
                table: "MediaStreamInfos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "PrimaryVersionId",
                table: "BaseItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "BaseItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            // Rows that predate the composite (ParentId, SortOrder) primary key stored a null SortOrder
            // (e.g. BoxSet and Collection children). Assign each such row a stable 0-based position within
            // its parent so the rows stay unique once SortOrder becomes part of the primary key; otherwise
            // they would all collapse to the column default (0) and collide during the table rebuild.
            migrationBuilder.Sql(
                @"UPDATE ""LinkedChildren""
                  SET ""SortOrder"" = (
                      SELECT COUNT(*)
                      FROM ""LinkedChildren"" AS lc2
                      WHERE lc2.""ParentId"" = ""LinkedChildren"".""ParentId""
                        AND lc2.""rowid"" < ""LinkedChildren"".""rowid""
                  )
                  WHERE ""SortOrder"" IS NULL;");

            migrationBuilder.CreateTable(
                name: "LinkedChildren",
                columns: table => new
                {
                    ParentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkedChildren", x => new { x.ParentId, x.SortOrder });
                    table.ForeignKey(
                        name: "FK_LinkedChildren_BaseItems_ChildId",
                        column: x => x.ChildId,
                        principalTable: "BaseItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LinkedChildren_BaseItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "BaseItems",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "BaseItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "Name", "OwnerId", "PrimaryVersionId" },
                values: new object[] { "This is a placeholder item for UserData that has been detached from its original item", null, null });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_UserId_IsFavorite_ItemId",
                table: "UserData",
                columns: new[] { "UserId", "IsFavorite", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_UserId_ItemId_LastPlayedDate",
                table: "UserData",
                columns: new[] { "UserId", "ItemId", "LastPlayedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_UserId_Played_ItemId",
                table: "UserData",
                columns: new[] { "UserId", "Played", "ItemId" });

            migrationBuilder.Sql("DELETE FROM Permissions WHERE UserId IS NULL OR UserId NOT IN (SELECT Id FROM Users);");
            migrationBuilder.Sql("DELETE FROM Preferences WHERE UserId IS NULL OR UserId NOT IN (SELECT Id FROM Users);");

            migrationBuilder.CreateIndex(
                name: "IX_Preferences_UserId_Kind",
                table: "Preferences",
                columns: new[] { "UserId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_UserId_Kind",
                table: "Permissions",
                columns: new[] { "UserId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeopleBaseItemMap_PeopleId_ItemId",
                table: "PeopleBaseItemMap",
                columns: new[] { "PeopleId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaStreamInfos_StreamType_ItemId_Language_IsExternal",
                table: "MediaStreamInfos",
                columns: new[] { "StreamType", "ItemId", "Language", "IsExternal" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_ExtraType_OwnerId",
                table: "BaseItems",
                columns: new[] { "ExtraType", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Name",
                table: "BaseItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_OwnerId",
                table: "BaseItems",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_PrimaryVersionId",
                table: "BaseItems",
                column: "PrimaryVersionId",
                filter: "\"PrimaryVersionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_SeasonId",
                table: "BaseItems",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_SeriesId",
                table: "BaseItems",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_SeriesName",
                table: "BaseItems",
                column: "SeriesName");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_IsFolder_IsVirtualItem_DateCreated",
                table: "BaseItems",
                columns: new[] { "TopParentId", "IsFolder", "IsVirtualItem", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_MediaType_IsVirtualItem_DateCreated",
                table: "BaseItems",
                columns: new[] { "TopParentId", "MediaType", "IsVirtualItem", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_Type_IsVirtualItem",
                table: "BaseItems",
                columns: new[] { "TopParentId", "Type", "IsVirtualItem" },
                filter: "\"PrimaryVersionId\" IS NULL AND (\"OwnerId\" IS NULL OR \"ExtraType\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_TopParentId_Type_IsVirtualItem_DateCreated",
                table: "BaseItems",
                columns: new[] { "TopParentId", "Type", "IsVirtualItem", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_CleanName",
                table: "BaseItems",
                columns: new[] { "Type", "CleanName" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_SeriesPresentationUniqueKey_ParentIndexNumbe~",
                table: "BaseItems",
                columns: new[] { "Type", "SeriesPresentationUniqueKey", "ParentIndexNumber", "IndexNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_SortName",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "SortName" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemProviders_ProviderId_ItemId_ProviderValue",
                table: "BaseItemProviders",
                columns: new[] { "ProviderId", "ItemId", "ProviderValue" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemImageInfos_ItemId_ImageType",
                table: "BaseItemImageInfos",
                columns: new[] { "ItemId", "ImageType" });

            migrationBuilder.CreateIndex(
                name: "IX_LinkedChildren_ChildId_ChildType",
                table: "LinkedChildren",
                columns: new[] { "ChildId", "ChildType" });

            migrationBuilder.CreateIndex(
                name: "IX_LinkedChildren_ParentId_ChildType",
                table: "LinkedChildren",
                columns: new[] { "ParentId", "ChildType" });

            migrationBuilder.AddForeignKey(
                name: "FK_BaseItems_BaseItems_OwnerId",
                table: "BaseItems",
                column: "OwnerId",
                principalTable: "BaseItems",
                principalColumn: "Id");

            // Expression index, so it cannot be declared on the entity type. /Persons collapses the
            // one-row-per-(Name, PersonType) table to one row per lowercased name; without this index
            // that dedup scans and groups the whole table on every request.
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Peoples_NameLower\" ON \"Peoples\" (lower(\"Name\"));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaseItems_BaseItems_OwnerId",
                table: "BaseItems");

            migrationBuilder.DropTable(
                name: "LinkedChildren");

            migrationBuilder.DropIndex(
                name: "IX_UserData_UserId_IsFavorite_ItemId",
                table: "UserData");

            migrationBuilder.DropIndex(
                name: "IX_UserData_UserId_ItemId_LastPlayedDate",
                table: "UserData");

            migrationBuilder.DropIndex(
                name: "IX_UserData_UserId_Played_ItemId",
                table: "UserData");

            migrationBuilder.DropIndex(
                name: "IX_Preferences_UserId_Kind",
                table: "Preferences");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_UserId_Kind",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_PeopleBaseItemMap_PeopleId_ItemId",
                table: "PeopleBaseItemMap");

            migrationBuilder.DropIndex(
                name: "IX_MediaStreamInfos_StreamType_ItemId_Language_IsExternal",
                table: "MediaStreamInfos");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_ExtraType_OwnerId",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_Name",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_OwnerId",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_PrimaryVersionId",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_SeasonId",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_SeriesId",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_SeriesName",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_TopParentId_IsFolder_IsVirtualItem_DateCreated",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_TopParentId_MediaType_IsVirtualItem_DateCreated",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_TopParentId_Type_IsVirtualItem",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_TopParentId_Type_IsVirtualItem_DateCreated",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_Type_CleanName",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_Type_SeriesPresentationUniqueKey_ParentIndexNumbe~",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_Type_TopParentId_SortName",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItemProviders_ProviderId_ItemId_ProviderValue",
                table: "BaseItemProviders");

            migrationBuilder.DropIndex(
                name: "IX_BaseItemImageInfos_ItemId_ImageType",
                table: "BaseItemImageInfos");

            migrationBuilder.DropColumn(
                name: "IsOriginal",
                table: "MediaStreamInfos");

            migrationBuilder.RenameColumn(
                name: "OriginalLanguage",
                table: "BaseItems",
                newName: "ExtraIds");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Preferences",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "Preference_Preferences_Guid",
                table: "Preferences",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Permissions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "Permission_Permissions_Guid",
                table: "Permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PrimaryVersionId",
                table: "BaseItems",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OwnerId",
                table: "BaseItems",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "BaseItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "Name", "OwnerId", "PrimaryVersionId" },
                values: new object[] { "This is a placeholder item for UserData that has been detacted from its original item", null, null });

            migrationBuilder.CreateIndex(
                name: "IX_UserData_UserId",
                table: "UserData",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Preferences_UserId_Kind",
                table: "Preferences",
                columns: new[] { "UserId", "Kind" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_UserId_Kind",
                table: "Permissions",
                columns: new[] { "UserId", "Kind" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PeopleBaseItemMap_PeopleId",
                table: "PeopleBaseItemMap",
                column: "PeopleId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaStreamInfos_StreamIndex",
                table: "MediaStreamInfos",
                column: "StreamIndex");

            migrationBuilder.CreateIndex(
                name: "IX_MediaStreamInfos_StreamIndex_StreamType",
                table: "MediaStreamInfos",
                columns: new[] { "StreamIndex", "StreamType" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaStreamInfos_StreamIndex_StreamType_Language",
                table: "MediaStreamInfos",
                columns: new[] { "StreamIndex", "StreamType", "Language" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaStreamInfos_StreamType",
                table: "MediaStreamInfos",
                column: "StreamType");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceId",
                table: "Devices",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Id_Type_IsFolder_IsVirtualItem",
                table: "BaseItems",
                columns: new[] { "Id", "Type", "IsFolder", "IsVirtualItem" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemProviders_ProviderId_ProviderValue_ItemId",
                table: "BaseItemProviders",
                columns: new[] { "ProviderId", "ProviderValue", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItemImageInfos_ItemId",
                table: "BaseItemImageInfos",
                column: "ItemId");
        }
    }
}
