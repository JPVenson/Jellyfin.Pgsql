#!/bin/bash

# Clean and create plugins directory, then copy plugin
rm -rf /config/plugins/PostgreSQL
mkdir -p /config/plugins/PostgreSQL
cp -r /jellyfin-pgsql/plugin/* /config/plugins/PostgreSQL/

# Create database.xml if it doesn't exist
if [ ! -f /config/config/database.xml ]; then
    mkdir -p /config/config
    cp /jellyfin-pgsql/database.xml /config/config/database.xml
fi

# Check database.xml correctly configured
ConfiguredPluginName="$(xmlstarlet select -t -m '//DatabaseConfigurationOptions/CustomProviderOptions/PluginName' -v . -n /config/config/database.xml)"
if [ "${ConfiguredPluginName}" != "PostgreSQL" ]; then
    echo "Plugin name is not set to PostgreSQL. abort."
    exit 2;
fi

# Check env variables set
if [ -z "${POSTGRES_HOST}" ]; then
    echo "PostgreSQL connectionstring variable unset. Please set 'POSTGRES_HOST' 'POSTGRES_PORT' 'POSTGRES_DB' 'POSTGRES_USER' and 'POSTGRES_PASSWORD' then restart"
    exit 3;
fi

# Build connection string for migration
ConnectionString="Password=${POSTGRES_PASSWORD};User ID=${POSTGRES_USER};Host=${POSTGRES_HOST};Port=${POSTGRES_PORT};Database=${POSTGRES_DB}"

# Add SSL options if provided
if [ -n "${POSTGRES_SSLMODE}" ]; then
    ConnectionString="${ConnectionString};SSL Mode=${POSTGRES_SSLMODE}"
fi

if [ -n "${POSTGRES_TRUSTSERVERCERTIFICATE}" ]; then
    ConnectionString="${ConnectionString};Trust Server Certificate=${POSTGRES_TRUSTSERVERCERTIFICATE}"
fi

# Update database.xml with connection string
xmlstarlet edit -L -u '//DatabaseConfigurationOptions/CustomProviderOptions/ConnectionString' -v "${ConnectionString}" /config/config/database.xml

# Migrate jellyfin.db if exists
if [ ! -f /config/data/jellyfin.db ]; then

    # run the EFbundle to migrate db to current state
    dotnet run /jellyfin-pgsql/jellyfin.PgsqlMigrator.dll --connection "${ConnectionString}"
    # run pgloader to move data
    pgloader /jellyfin-pgsql/jellyfindb.load
    # rename jellyfin db
    mv /config/data/jellyfin.db /config/data/jellyfin.db.pgsql
fi


# Run original Jellyfin entrypoint
exec /jellyfin/jellyfin "$@"
