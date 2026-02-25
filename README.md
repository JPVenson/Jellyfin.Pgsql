# The Unoffical Postgre SQL adapter for the jellyfin server

This adds postgres SQL support via an plugin to the jellyfin server. There are several steps required to make this work and it is to be considered __HIGHLY__ experimental.

# How to use it

You can use your existing jellyfin compose file and change the image accordingly to: `ghcr.io/jpvenson/jellyfin.pgsql:10.11.6-1`.

You need to add the connection paramters as enviorment variables in your compose file:

```yaml

services:
  jellyfin:
    image: ghcr.io/jpvenson/jellyfin.pgsql:10.11.6-1
    volumes:
        - /path/to/config:/config
        - /path/to/cache:/cache
        - /path/to/media:/media
    environment:
        - POSTGRES_HOST=
        - POSTGRES_PORT=
        - POSTGRES_DB=jellyfin
        - POSTGRES_USER=jellyfin
        - POSTGRES_PASSWORD=jellyfin
      # Optional settings bellow, uncomment if you want to connect using SSL
      # - POSTGRES_SSLMODE=Require
      # - POSTGRES_TRUSTSERVERCERTIFICATE=true
```

# Build

Checkout the Jellyfin submodule.
Use dotnet build to build the plugin.
Place the plugin in the plugin folder of the JF app.
Update the database.xml file to switch to the plugin as its database provider:

```xml
<?xml version="1.0" encoding="utf-8"?>
<DatabaseConfigurationOptions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <DatabaseType>PLUGIN_PROVIDER</DatabaseType>
  <CustomProviderOptions>
    <PluginAssembly>../../../Jellyfin.Plugin.Pgsql/bin/debug/net9.0/Jellyfin.Plugin.Pgsql.dll</PluginAssembly>
    <PluginName>PostgreSQL</PluginName>
    <ConnectionString>CONNECTION_STRING_TO_LOCAL_PGSQL_SERVER</ConnectionString>
  </CustomProviderOptions>
  <LockingBehavior>NoLock</LockingBehavior>
</DatabaseConfigurationOptions>

```

launch your jellyfin server.

# Add migration
Run `dotnet ef migrations add {MIGRATION_NAME} --project "/workspaces/Jellyfin.Pgsql/Jellyfin.Plugin.Pgsql" -- --migration-provider Jellyfin-PgSql`

# Release flow

To create a new release, first sync all Jellyfin server changes then create a new migration as seen above. After that create a new efbundle:
`dotnet ef migrations bundle -o docker/jellyfin.PgsqlMigrator.dll -r linux-x64 --self-contained --project "/workspaces/Jellyfin.Pgsql/Jellyfin.Plugin.Pgsql" --  --migration-provider Jellyfin-PgSql`
Then build the container


# Migration Instructions (ADVANCED, UNTESTED)

To migrate your JF install to a custom database (not using the docker image) follow the steps IN THIS ORDER.

1. Download the Jellyfin PGSQL container and configure it to point to an existing empty database and empty config directory. DO NOT USE YOUR EXISTING DATA OR SQLITE LIBRARY CONFIGURE A FULLY CLEAR INSTANCE.
2. Run jellyfin once with it configured to your empty database, this will seed the database and its migration history.
3. Stop your JF instance after its been started once (no need to setup fully though the startup wizzard). If you did not get the setup wizzard you did something wrong!
4. Install the pgloader tool `apt install pgloader` or see https://pgloader.readthedocs.io/en/latest/install.html.
5. Download the [jellyfindb.load](/docker/jellyfindb.load) file
6. Adapt the `jellyfindb.load` file accordingly to point towards your old jellyfin.db and your postgres instance. See https://pgloader.readthedocs.io/en/latest/ref/sqlite.html
7. Use the load file in `jellyfindb.load` to transfer your sqlite db into the postgres db like `pgloader /jellyfin-pgsql/jellyfindb.load`.
8. Move your old Data back to the jellyfin directories
9. Start jellyfin

If you get an error regarding a missing `__EFMigrationsHistory` you did not start jellyfin with a clear state.
