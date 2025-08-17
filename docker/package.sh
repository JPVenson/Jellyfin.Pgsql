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
cp ../Jellyfin.Plugin.Pgsql/Database/PgSqlRetryInterceptor.cs ./Jellyfin.Plugin.Pgsql/Database/
cp ../Jellyfin.Plugin.Pgsql/Database/PgSqlTransactionInterceptor.cs ./Jellyfin.Plugin.Pgsql/Database/
cp ../Jellyfin.Plugin.Pgsql/Migrations/20250618214615_PgSQL_Init.cs ./Jellyfin.Plugin.Pgsql/Migrations/
cp ../Jellyfin.Plugin.Pgsql/Migrations/20250618214615_PgSQL_Init.Designer.cs ./Jellyfin.Plugin.Pgsql/Migrations/
cp ../Jellyfin.Plugin.Pgsql/Migrations/JellyfinDbContextModelSnapshot.cs ./Jellyfin.Plugin.Pgsql/Migrations/

# Set permissions on directories
chmod 755 Jellyfin.Plugin.Pgsql Jellyfin.Plugin.Pgsql/Configuration Jellyfin.Plugin.Pgsql/Database Jellyfin.Plugin.Pgsql/Migrations

# Set permissions on files
chmod 644 Dockerfile entrypoint.sh database.xml jellyfin.ruleset Jellyfin.Plugin.Pgsql.sln build.yaml \
    Jellyfin.Plugin.Pgsql/Jellyfin.Plugin.Pgsql.csproj \
    Jellyfin.Plugin.Pgsql/Plugin.cs \
    Jellyfin.Plugin.Pgsql/Configuration/PluginConfiguration.cs \
    Jellyfin.Plugin.Pgsql/Configuration/configPage.html \
    Jellyfin.Plugin.Pgsql/Database/PgSqlDatabaseProvider.cs \
    Jellyfin.Plugin.Pgsql/Database/PgSqlRetryInterceptor.cs \
    Jellyfin.Plugin.Pgsql/Database/PgSqlTransactionInterceptor.cs \
    Jellyfin.Plugin.Pgsql/Migrations/20250618214615_PgSQL_Init.cs \
    Jellyfin.Plugin.Pgsql/Migrations/20250618214615_PgSQL_Init.Designer.cs \
    Jellyfin.Plugin.Pgsql/Migrations/JellyfinDbContextModelSnapshot.cs

tar -czf "$PACKAGE_NAME" \
    --uid=0 --gid=0 \
    Dockerfile \
    entrypoint.sh \
    database.xml \
    jellyfin.ruleset \
    Jellyfin.Plugin.Pgsql.sln \
    build.yaml \
    Jellyfin.Plugin.Pgsql/

# Clean up temporary files
rm jellyfin.ruleset Jellyfin.Plugin.Pgsql.sln build.yaml
rm -rf Jellyfin.Plugin.Pgsql/

# Show package size
SIZE=$(ls -lh "$PACKAGE_NAME" | awk '{print $5}')
echo "Package created: $PACKAGE_NAME ($SIZE)"

echo ""
echo "To use on another system:"
echo "1. Copy $PACKAGE_NAME to the target system"
echo "2. Extract: tar -xzf $PACKAGE_NAME"
echo "3. Build: docker build -t jellyfin-pgsql ."
echo "4. Run with PostgreSQL environment variables"
