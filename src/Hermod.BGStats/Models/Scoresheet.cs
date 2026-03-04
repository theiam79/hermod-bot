namespace Hermod.BGStats.Models;

public sealed record Scoresheet
{
    public int BggId { get; init; }
    public int Version { get; init; }
    public string LangCode { get; init; } = "";
    public string ScoreType { get; init; } = "";
    public string? VariantId { get; init; }
    public string? VariantLabel { get; init; }
    public List<ScoresheetGroup> Groups { get; init; } = [];
}

public sealed record ScoresheetGroup
{
    public string TemplateId { get; init; } = "";
    public int Repetition { get; init; }
    public bool HasSubTotal { get; init; }
    public bool HideSingleGroupLabel { get; init; }
    public bool IsExtra { get; init; }
    public List<ScoresheetRow> Rows { get; init; } = [];
}

public sealed record ScoresheetRow
{
    public string TemplateId { get; init; } = "";
    public string Label { get; init; } = "";
    public int Repetition { get; init; }
    public bool Repeatable { get; init; }
    public bool Negative { get; init; }
    public bool IsExtra { get; init; }

    /// <summary>
    /// Maps player UUID (string) to score value (string).
    /// </summary>
    public Dictionary<string, string> Scores { get; init; } = [];
}
