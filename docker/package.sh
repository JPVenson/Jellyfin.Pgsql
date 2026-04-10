#!/bin/bash

# Package Jellyfin PostgreSQL Plugin for Docker build
# This script creates a tar.gz with all files needed to build the Docker image

set -e

PACKAGE_NAME="jellyfin-pgsql-plugin.tar.gz"

echo "Creating package: $PACKAGE_NAME"

# Remove existing package if it exists
if [ -f "$PACKAGE_NAME" ]; then
    rm "$PACKAGE_NAME"
    echo "Removed existing $PACKAGE_NAME"
fi

# Create the tar.gz with all required files
# Copy files to avoid relative paths and set proper permissions
cp ../jellyfin.ruleset ./
cp ../Jellyfin.Plugin.Pgsql.sln ./
cp ../build.yaml ./

# Copy only the source code files needed for the build
mkdir -p Jellyfin.Plugin.Pgsql/Configuration
mkdir -p Jellyfin.Plugin.Pgsql/Database
mkdir -p Jellyfin.Plugin.Pgsql/Migrations

cp ../Jellyfin.Plugin.Pgsql/Jellyfin.Plugin.Pgsql.csproj ./Jellyfin.Plugin.Pgsql/
cp ../Jellyfin.Plugin.Pgsql/Plugin.cs ./Jellyfin.Plugin.Pgsql/
cp ../Jellyfin.Plugin.Pgsql/Configuration/PluginConfiguration.cs ./Jellyfin.Plugin.Pgsql/Configuration/
cp ../Jellyfin.Plugin.Pgsql/Configuration/configPage.html ./Jellyfin.Plugin.Pgsql/Configuration/
cp ../Jellyfin.Plugin.Pgsql/Database/PgSqlDatabaseProvider.cs ./Jellyfin.Plugin.Pgsql/Database/
cp ../Jellyfin.Plugin.Pgsql/Migrations/20250618214615_PgSQL_Init.cs ./Jellyfin.Plugin.Pgsql/Migrations/
cp ../Jellyfin.Plugin.Pgsql/Migrations/20250618214615_PgSQL_Init.Designer.cs ./Jellyfin.Plugin.Pgsql/Migrations/
cp ../Jellyfin.Plugin.Pgsql/Migrations/20250913211637_AddProperParentChildRelationBaseItemWithCascade.cs ./Jellyfin.Plugin.Pgsql/Migrations/
cp ../Jellyfin.Plugin.Pgsql/Migrations/20250913211637_AddProperParentChildRelationBaseItemWithCascade.Designer.cs ./Jellyfin.Plugin.Pgsql/Migrations/
cp ../Jellyfin.Plugin.Pgsql/Migrations/20250929202529_Update_10.11-RC8.cs ./Jellyfin.Plugin.Pgsql/Migrations/
cp ../Jellyfin.Plugin.Pgsql/Migrations/20250929202529_Update_10.11-RC8.Designer.cs ./Jellyfin.Plugin.Pgsql/Migrations/
cp ../Jellyfin.Plugin.Pgsql/Migrations/20260128200059_10.11.6-1.cs ./Jellyfin.Plugin.Pgsql/Migrations/
cp ../Jellyfin.Plugin.Pgsql/Migrations/20260128200059_10.11.6-1.Designer.cs ./Jellyfin.Plugin.Pgsql/Migrations/
cp ../Jellyfin.Plugin.Pgsql/Migrations/JellyfinDbContextModelSnapshot.cs ./Jellyfin.Plugin.Pgsql/Migrations/

# Set permissions on directories
chmod 755 Jellyfin.Plugin.Pgsql Jellyfin.Plugin.Pgsql/Configuration Jellyfin.Plugin.Pgsql/Database Jellyfin.Plugin.Pgsql/Migrations

# Create docker subdirectory for files Dockerfile expects there
mkdir -p docker
cp entrypoint.sh docker/
cp database.xml docker/
cp jellyfindb.load docker/
cp jellyfin.PgsqlMigrator.dll docker/ 2>/dev/null || true

# Set permissions on files
chmod 644 Dockerfile docker/entrypoint.sh docker/database.xml jellyfin.ruleset Jellyfin.Plugin.Pgsql.sln build.yaml \
    Jellyfin.Plugin.Pgsql/Jellyfin.Plugin.Pgsql.csproj \
    Jellyfin.Plugin.Pgsql/Plugin.cs \
    Jellyfin.Plugin.Pgsql/Configuration/PluginConfiguration.cs \
    Jellyfin.Plugin.Pgsql/Configuration/configPage.html \
    Jellyfin.Plugin.Pgsql/Database/PgSqlDatabaseProvider.cs \
    Jellyfin.Plugin.Pgsql/Migrations/20250618214615_PgSQL_Init.cs \
    Jellyfin.Plugin.Pgsql/Migrations/20250618214615_PgSQL_Init.Designer.cs \
    Jellyfin.Plugin.Pgsql/Migrations/20250913211637_AddProperParentChildRelationBaseItemWithCascade.cs \
    Jellyfin.Plugin.Pgsql/Migrations/20250913211637_AddProperParentChildRelationBaseItemWithCascade.Designer.cs \
    Jellyfin.Plugin.Pgsql/Migrations/20250929202529_Update_10.11-RC8.cs \
    Jellyfin.Plugin.Pgsql/Migrations/20250929202529_Update_10.11-RC8.Designer.cs \
    Jellyfin.Plugin.Pgsql/Migrations/20260128200059_10.11.6-1.cs \
    Jellyfin.Plugin.Pgsql/Migrations/20260128200059_10.11.6-1.Designer.cs \
    Jellyfin.Plugin.Pgsql/Migrations/JellyfinDbContextModelSnapshot.cs

tar -czf "$PACKAGE_NAME" \
    --uid=0 --gid=0 \
    Dockerfile \
    docker/ \
    jellyfin.ruleset \
    Jellyfin.Plugin.Pgsql.sln \
    build.yaml \
    Jellyfin.Plugin.Pgsql/

# Clean up temporary files
rm jellyfin.ruleset Jellyfin.Plugin.Pgsql.sln build.yaml
rm -rf Jellyfin.Plugin.Pgsql/
rm -rf docker/

# Show package size
SIZE=$(ls -lh "$PACKAGE_NAME" | awk '{print $5}')
echo "Package created: $PACKAGE_NAME ($SIZE)"

echo ""
echo "To use on another system:"
echo "1. Copy $PACKAGE_NAME to the target system"
echo "2. Extract: tar -xzf $PACKAGE_NAME"
echo "3. Build: docker build -t jellyfin-pgsql ."
echo "4. Run with PostgreSQL environment variables"
