# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY jellyfin.ruleset .
COPY Jellyfin.Plugin.Pgsql.sln .
COPY build.yaml .
COPY Jellyfin.Plugin.Pgsql/ ./Jellyfin.Plugin.Pgsql/

# Restore and publish
RUN dotnet restore Jellyfin.Plugin.Pgsql.sln
RUN dotnet publish Jellyfin.Plugin.Pgsql.sln -c Release --no-restore -o /app/publish

# Final stage - Jellyfin with plugin
FROM jellyfin/jellyfin:latest

# Install PostgreSQL 18 client tools for backup/restore functionality
RUN apt-get update && \
    apt-get install -y wget ca-certificates gnupg lsb-release && \
    wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | gpg --dearmor -o /usr/share/keyrings/postgresql-keyring.gpg && \
    echo "deb [signed-by=/usr/share/keyrings/postgresql-keyring.gpg] http://apt.postgresql.org/pub/repos/apt/ $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list && \
    apt-get update && \
    apt-get install -y postgresql-client-18 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy the published plugin and config files
COPY --from=build /app/publish/ /jellyfin-pgsql/plugin/
COPY entrypoint.sh /entrypoint.sh
COPY database.xml /jellyfin-pgsql/database.xml

# Make entrypoint executable
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
