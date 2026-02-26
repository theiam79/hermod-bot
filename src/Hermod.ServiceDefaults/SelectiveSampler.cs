using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Samples Wolverine durability agent traces at a reduced rate
/// while keeping all other traces (HTTP requests, application handlers, etc.) at 100%.
/// </summary>
public sealed class SelectiveSampler(double wolverineRatio = 0.05) : Sampler
{
    private readonly Sampler _wolverineSampler =
        new ParentBasedSampler(new TraceIdRatioBasedSampler(wolverineRatio));
    private readonly Sampler _defaultSampler =
        new ParentBasedSampler(new AlwaysOnSampler());

    public override SamplingResult ShouldSample(in SamplingParameters parameters)
    {
        var sampler = IsWolverineInternal(parameters) ? _wolverineSampler : _defaultSampler;
        return sampler.ShouldSample(parameters);
    }

    private static bool IsWolverineInternal(in SamplingParameters parameters) =>
        parameters.Name.StartsWith("wolverine", StringComparison.OrdinalIgnoreCase);
}
