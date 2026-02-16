using NCalc;

namespace Hermod.BGStats.Models;

public sealed record Score
{
    public required Player Player { get; init; }
    public string? ScoreExpression { get; init; }
    public bool Winner { get; init; }
    public bool NewPlayer { get; init; }
    public bool StartPlayer { get; init; }
    public string Role { get; init; } = "";
    public int Rank { get; init; }
    public int SeatOrder { get; init; }
    public string? Team { get; init; }
    public string StartPosition { get; init; } = "";

    public double? CalculateScore()
    {
        return ScoreExpression switch
        {
            null or "" => null,
            _ when double.TryParse(ScoreExpression, out var parsed) => parsed,
            _ when TryEvaluate(ScoreExpression, out var evaluated) => evaluated,
            _ => null
        };
    }

    private static bool TryEvaluate(string expression, out double? result)
    {
        result = null;
        try
        {
            var exp = new Expression(expression, ExpressionOptions.None);
            var evaluated = exp.Evaluate();
            result = Convert.ToDouble(evaluated);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
