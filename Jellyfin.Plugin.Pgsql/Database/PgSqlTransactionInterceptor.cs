using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;

namespace Jellyfin.Plugin.Pgsql.Database;

/// <summary>
/// PostgreSQL transaction interceptor that handles connection issues during transaction start.
/// </summary>
internal sealed class PgSqlTransactionInterceptor : DbTransactionInterceptor
{
    private readonly AsyncPolicy _asyncRetryPolicy;
    private readonly Policy _retryPolicy;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PgSqlTransactionInterceptor"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public PgSqlTransactionInterceptor(ILogger logger)
    {
        _logger = logger;

        // Shorter retry durations for transaction start operations
        TimeSpan[] sleepDurations = [
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(25),
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(250)
        ];

        _retryPolicy = Policy
            .Handle<PostgresException>(ex => IsRetryablePostgresError(ex))
            .WaitAndRetry(sleepDurations, RetryHandle);

        _asyncRetryPolicy = Policy
            .Handle<PostgresException>(ex => IsRetryablePostgresError(ex))
            .WaitAndRetryAsync(sleepDurations, RetryHandle);

        void RetryHandle(Exception exception, TimeSpan timespan, int retryNo, Context context)
        {
            _logger.LogWarning(
                "PostgreSQL transaction start failed, retry {RetryNo} in {Delay}ms. Error: {Error}",
                retryNo,
                timespan.TotalMilliseconds,
                GetErrorSummary(exception));
        }
    }

    /// <inheritdoc/>
    public override InterceptionResult<DbTransaction> TransactionStarting(
        DbConnection connection,
        TransactionStartingEventData eventData,
        InterceptionResult<DbTransaction> result)
    {
        return InterceptionResult<DbTransaction>.SuppressWithResult(
            _retryPolicy.Execute(() => connection.BeginTransaction(eventData.IsolationLevel)));
    }

    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
        DbConnection connection,
        TransactionStartingEventData eventData,
        InterceptionResult<DbTransaction> result,
        CancellationToken cancellationToken = default)
    {
        return InterceptionResult<DbTransaction>.SuppressWithResult(
            await _asyncRetryPolicy.ExecuteAsync(async () =>
                await connection.BeginTransactionAsync(eventData.IsolationLevel, cancellationToken).ConfigureAwait(false))
            .ConfigureAwait(false));
    }

    private static bool IsRetryablePostgresError(PostgresException ex)
    {
        return ex.SqlState switch
        {
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
