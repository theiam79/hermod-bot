using System.Diagnostics;
using OpenTelemetry;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Filters out Wolverine durability agent PostgreSQL traces by clearing the Recorded flag
/// after the span completes (when db.query.text is actually populated).
/// A sampler can't do this because tags aren't available at span creation time.
/// </summary>
public sealed class WolverineTraceFilterProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity data)
    {
        if (IsWolverineInternal(data))
            data.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
    }

    private static bool IsWolverineInternal(Activity data)
    {
        if (!data.OperationName.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
            return false;

        var queryText = data.GetTagItem("db.query.text")?.ToString();

        return queryText is not null
            && (queryText.Contains("wolverine.", StringComparison.OrdinalIgnoreCase)
                || queryText.Contains("pg_try_advisory_xact_lock", StringComparison.OrdinalIgnoreCase));
    }
}
