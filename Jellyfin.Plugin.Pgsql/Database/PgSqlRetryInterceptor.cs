using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;

namespace Jellyfin.Plugin.Pgsql.Database;

/// <summary>
/// PostgreSQL command interceptor that handles deadlocks and retries.
/// </summary>
internal sealed class PgSqlRetryInterceptor : DbCommandInterceptor
{
    private readonly AsyncPolicy _asyncRetryPolicy;
    private readonly Policy _retryPolicy;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PgSqlRetryInterceptor"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public PgSqlRetryInterceptor(ILogger logger)
    {
        _logger = logger;

        // PostgreSQL-specific retry durations with jitter to avoid thundering herd
        TimeSpan[] sleepDurations = [
            TimeSpan.FromMilliseconds(10 + Random.Shared.Next(0, 10)),   // 10-20ms
            TimeSpan.FromMilliseconds(25 + Random.Shared.Next(0, 25)),   // 25-50ms
            TimeSpan.FromMilliseconds(50 + Random.Shared.Next(0, 50)),   // 50-100ms
            TimeSpan.FromMilliseconds(100 + Random.Shared.Next(0, 100)), // 100-200ms
            TimeSpan.FromMilliseconds(250 + Random.Shared.Next(0, 250)), // 250-500ms
            TimeSpan.FromMilliseconds(500 + Random.Shared.Next(0, 500)), // 500ms-1s
            TimeSpan.FromMilliseconds(1000 + Random.Shared.Next(0, 1000)) // 1-2s
        ];

        _retryPolicy = Policy
            .Handle<PostgresException>(ex => IsRetryablePostgresError(ex))
            .WaitAndRetry(sleepDurations, RetryHandle);

        _asyncRetryPolicy = Policy
            .Handle<PostgresException>(ex => IsRetryablePostgresError(ex))
            .WaitAndRetryAsync(sleepDurations, RetryHandle);

        void RetryHandle(Exception exception, TimeSpan timespan, int retryNo, Context context)
        {
            if (retryNo < sleepDurations.Length)
            {
                _logger.LogWarning(
                    "PostgreSQL operation failed, retry {RetryNo} in {Delay}ms. Error: {Error}",
                    retryNo,
                    timespan.TotalMilliseconds,
                    GetErrorSummary(exception));
            }
            else
            {
                _logger.LogError(exception, "PostgreSQL operation failed after {RetryNo} retries", retryNo);
            }
        }
    }

    /// <inheritdoc/>
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        return InterceptionResult<int>.SuppressWithResult(
            _retryPolicy.Execute(command.ExecuteNonQuery));
    }

    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        return InterceptionResult<int>.SuppressWithResult(
            await _asyncRetryPolicy.ExecuteAsync(async () =>
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false))
            .ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        return InterceptionResult<object>.SuppressWithResult(
            _retryPolicy.Execute(() => command.ExecuteScalar()!));
    }

    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        return InterceptionResult<object>.SuppressWithResult(
            (await _asyncRetryPolicy.ExecuteAsync(async () =>
                await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)!)
            .ConfigureAwait(false))!);
    }

    /// <inheritdoc/>
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        return InterceptionResult<DbDataReader>.SuppressWithResult(
            _retryPolicy.Execute(command.ExecuteReader));
    }

    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        return InterceptionResult<DbDataReader>.SuppressWithResult(
            await _asyncRetryPolicy.ExecuteAsync(async () =>
                await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            .ConfigureAwait(false));
    }

    private static bool IsRetryablePostgresError(PostgresException ex)
    {
        return ex.SqlState switch
        {
            "40P01" => true, // deadlock_detected
            "40001" => true, // serialization_failure
            "53300" => true, // too_many_connections
            "08003" => true, // connection_does_not_exist
            "08006" => true, // connection_failure
            "08001" => true, // sqlclient_unable_to_establish_sqlconnection
            "08004" => true, // sqlserver_rejected_establishment_of_sqlconnection
            _ => false
        };
    }

    private static string GetErrorSummary(Exception ex)
    {
        return ex switch
        {
            PostgresException pgEx => $"PostgreSQL {pgEx.SqlState}: {pgEx.MessageText}",
            _ => ex.Message
        };
    }
}
