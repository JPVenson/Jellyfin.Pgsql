#!/usr/bin/env -S dotnet run
using System.Diagnostics;
using System.Xml.Linq;
using System.Linq;

const string PluginDir = "/config/plugins/PostgreSQL";
const string DbConfigDir = "/config/config";
const string DbConfigFile = DbConfigDir + "/database.xml";
const string SqliteDb = "/config/data/jellyfin.db";
const string SqliteDbMigrated = "/config/data/jellyfin.db.pgsql";
const string JellyfinBin = "/jellyfin/jellyfin";

// --- Prepare plugin directory ---
Log("Preparing PostgreSQL plugin files...");
if (Directory.Exists(PluginDir))
    Directory.Delete(PluginDir, recursive: true);
Directory.CreateDirectory(PluginDir);
CopyDirectory("/jellyfin-pgsql/plugin", PluginDir);

// --- Ensure database.xml exists ---
if (!File.Exists(DbConfigFile))
{
    Log("Creating default database.xml...");
    Directory.CreateDirectory(DbConfigDir);
    File.Copy("/jellyfin-pgsql/database.xml", DbConfigFile);
}

// --- Validate PluginName ---
var doc = XDocument.Load(DbConfigFile);
var pluginNameEl = doc.Descendants("PluginName").FirstOrDefault();
if (pluginNameEl?.Value != "PostgreSQL")
{
    Error($"PluginName in database.xml must be PostgreSQL. Found: {pluginNameEl?.Value}");
    return 2;
}

// --- Validate required environment variables ---
string[] requiredVars = ["POSTGRES_HOST", "POSTGRES_PORT", "POSTGRES_DB", "POSTGRES_USER", "POSTGRES_PASSWORD"];
var missingVariables = (from varName in requiredVars
                        where string.IsNullOrEmpty(Environment.GetEnvironmentVariable(varName))
                        select varName).ToList();

if (missingVariables.Any())
{
    Error($"Missing required environment variable: {string.Join(", ", missingVariables)}");
    return 3;
}

// --- Build connection string ---
var connectionString = $"Password={Env("POSTGRES_PASSWORD")};User ID={Env("POSTGRES_USER")};Host={Env("POSTGRES_HOST")};Port={Env("POSTGRES_PORT")};Database={Env("POSTGRES_DB")}";

if (Env("POSTGRES_SSLMODE") is { Length: > 0 } sslMode)
    connectionString += $";SSL Mode={sslMode}";

if (Env("POSTGRES_TRUSTSERVERCERTIFICATE") is { Length: > 0 } trustCert)
    connectionString += $";Trust Server Certificate={trustCert}";

// --- Write connection string into database.xml ---
Log("Writing PostgreSQL connection string to database.xml...");
var connEl = doc.Descendants("ConnectionString").FirstOrDefault()
    ?? throw new InvalidOperationException("ConnectionString element not found in database.xml");
connEl.Value = connectionString;
doc.Save(DbConfigFile);

// --- Migrate from SQLite if present and not yet migrated ---
if (File.Exists(SqliteDb) && !File.Exists(SqliteDbMigrated))
{
    Log("Existing SQLite database detected, starting migration flow...");
    Log("Seed Jellyfin PGSQL database...");
    Exec(JellyfinBin, "--mode", "SeedSystem");
    Log("Migrate Data from sqlite.db to pgsql...");
    Exec("pgloader", "/jellyfin-pgsql/jellyfindb.load");
    Log($"Rename '{SqliteDb}' to '{SqliteDbMigrated}'.");
    File.Move(SqliteDb, SqliteDbMigrated);
    Log("SQLite migration complete.");
}

// --- Launch Jellyfin ---
Log("Starting Jellyfin...");
return Exec(JellyfinBin, args);

// --- Helpers ---

static void Log(string msg) => Console.WriteLine($"[entrypoint] {msg}");
static void Error(string msg) => Console.Error.WriteLine($"[entrypoint] {msg}");
static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? string.Empty;

static void CopyDirectory(string src, string dst)
{
    foreach (var file in Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories))
    {
        var rel = Path.GetRelativePath(src, file);
        var dest = Path.Combine(dst, rel);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(file, dest, overwrite: true);
    }
}

static int Exec(string executable, params string[] arguments)
{
    var psi = new ProcessStartInfo(executable) { UseShellExecute = false };
    foreach (var arg in arguments)
        psi.ArgumentList.Add(arg);
    using var proc = Process.Start(psi)
        ?? throw new InvalidOperationException($"Failed to start {executable}");
    proc.WaitForExit();
    return proc.ExitCode;
}
