using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Drops Wolverine durability agent traces entirely (internal bookkeeping noise)
/// while keeping all other traces (HTTP requests, application handlers, etc.) at 100%.
/// </summary>
public sealed class SelectiveSampler : Sampler
{
    private static readonly SamplingResult Drop = new(SamplingDecision.Drop);
    private readonly Sampler _defaultSampler = new ParentBasedSampler(new AlwaysOnSampler());

    public override SamplingResult ShouldSample(in SamplingParameters parameters)
    {
        return IsWolverineInternal(parameters) ? Drop : _defaultSampler.ShouldSample(parameters);
    }

    private static bool IsWolverineInternal(in SamplingParameters parameters)
    {
        // Wolverine durability agent spans show up as Npgsql "postgresql" client spans,
        // not as Wolverine activity source spans. Match on the SQL query text instead.
        if (!parameters.Name.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
            return false;

        var queryText = parameters.Tags?
            .FirstOrDefault(t => t.Key == "db.query.text")
            .Value?.ToString();

        return queryText is not null
            && (queryText.Contains("wolverine.", StringComparison.OrdinalIgnoreCase)
                || queryText.Contains("pg_try_advisory_xact_lock", StringComparison.OrdinalIgnoreCase));
    }
}
