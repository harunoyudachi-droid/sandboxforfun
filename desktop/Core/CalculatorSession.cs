using System.Globalization;

namespace SandboxCalculator;

public sealed record HistoryEntry(string Expression, bool Degrees, double Value)
{
    public override string ToString() => $"{Expression} [{(Degrees ? "DEG" : "RAD")}] = {CalculatorSession.Format(Value)}";
}

public sealed class CalculatorSession
{
    public bool Degrees { get; set; } = true;
    public double Answer { get; set; }
    public string Expression { get; set; } = "";
    public string Display { get; set; } = "0";
    public List<HistoryEntry> History { get; set; } = [];

    public double Calculate(string expression)
    {
        var value = ExpressionEngine.Evaluate(expression, Degrees, Answer);
        Expression = expression;
        Answer = value;
        Display = Format(value);
        History.Insert(0, new(expression, Degrees, value));
        if (History.Count > 20) History.RemoveRange(20, History.Count - 20);
        return value;
    }

    public void Recall(HistoryEntry entry)
    {
        Expression = entry.Expression;
        Degrees = entry.Degrees;
        Answer = entry.Value;
        Display = Format(entry.Value);
    }

    public static string Format(double value) => value == 0 ? "0" : value.ToString("G12", CultureInfo.InvariantCulture);
}
