# Build stage
# Context hint: project root ../
# sudo docker build -t ghcr.io/jpvenson/jellyfin.pgsql:10.11.0-1 -f ./Dockerfile ../
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Install native build tools required for NativeAOT/self-contained single-file publish
RUN apt-get update && \
    apt-get install -y --no-install-recommends clang zlib1g-dev && \
    rm -rf /var/lib/apt/lists/*

# Copy project files
COPY jellyfin.ruleset .
COPY Jellyfin.Plugin.Pgsql.sln .
COPY build.yaml .
COPY Jellyfin.Plugin.Pgsql/ ./Jellyfin.Plugin.Pgsql/
COPY docker/entrypoint.cs ./entrypoint.cs

# Restore and publish plugin
RUN dotnet restore Jellyfin.Plugin.Pgsql.sln
RUN dotnet publish Jellyfin.Plugin.Pgsql.sln -c Release --no-restore -o /app/publish

# Publish entrypoint as a self-contained single-file binary
RUN dotnet publish entrypoint.cs -r linux-x64 -c Release --self-contained \
    -p:PublishSingleFile=true -o /app/entrypoint

# Final stage - Jellyfin with plugin
FROM jellyfin/jellyfin:10.11.8

# Install PostgreSQL 18 client tools for backup/restore functionality
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl ca-certificates gpg lsb-release && \
    curl -fsSL https://www.postgresql.org/media/keys/ACCC4CF8.asc | gpg --dearmor -o /usr/share/keyrings/postgresql-keyring.gpg && \
    echo "deb [signed-by=/usr/share/keyrings/postgresql-keyring.gpg] http://apt.postgresql.org/pub/repos/apt/ $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list && \
    apt-get update && \
    apt-get install -y --no-install-recommends postgresql-client-18 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy the published plugin, entrypoint binary, and config files
COPY --from=build /app/publish/ /jellyfin-pgsql/plugin/
COPY --from=build /app/entrypoint/entrypoint /entrypoint
COPY docker/database.xml /jellyfin-pgsql/database.xml
COPY docker/jellyfindb.load /jellyfin-pgsql/jellyfindb.load

# Make entrypoint executable
RUN chmod +x /entrypoint

ENTRYPOINT ["/entrypoint"]
