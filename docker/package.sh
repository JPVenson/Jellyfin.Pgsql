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

# Copy the source code files needed for the build: the whole project, without build output
(cd .. && tar -cf - --exclude='bin' --exclude='obj' --exclude='.DS_Store' Jellyfin.Plugin.Pgsql) | tar -xf -

# The Dockerfile copies docker/entrypoint.sh and docker/database.xml from the build context
mkdir -p docker
cp entrypoint.sh database.xml docker/

# Set permissions on directories
find Jellyfin.Plugin.Pgsql docker -type d -exec chmod 755 {} +

# Set permissions on files
chmod 644 Dockerfile docker/entrypoint.sh docker/database.xml jellyfin.ruleset Jellyfin.Plugin.Pgsql.sln build.yaml
find Jellyfin.Plugin.Pgsql -type f -exec chmod 644 {} +

tar -czf "$PACKAGE_NAME" \
    --owner=0 --group=0 \
    Dockerfile \
    docker/entrypoint.sh \
    docker/database.xml \
    jellyfin.ruleset \
    Jellyfin.Plugin.Pgsql.sln \
    build.yaml \
    Jellyfin.Plugin.Pgsql/

# Clean up temporary files
rm jellyfin.ruleset Jellyfin.Plugin.Pgsql.sln build.yaml
rm -rf Jellyfin.Plugin.Pgsql/ docker/

# Show package size
SIZE=$(ls -lh "$PACKAGE_NAME" | awk '{print $5}')
echo "Package created: $PACKAGE_NAME ($SIZE)"

echo ""
echo "To use on another system:"
echo "1. Copy $PACKAGE_NAME to the target system"
echo "2. Extract into an empty directory: tar -xzf $PACKAGE_NAME"
echo "3. Build: docker build -t jellyfin-pgsql ."
echo "4. Run with PostgreSQL environment variables"
