using System.Text.Json;

namespace SandboxCalculator;

public static class SessionStore
{
    public static CalculatorSession Load(string path)
    {
        try
        {
            if (!File.Exists(path) || new FileInfo(path).Length > 1_000_000) return new();
            var state = JsonSerializer.Deserialize<CalculatorSession>(File.ReadAllText(path));
            if (state is null || !double.IsFinite(state.Answer) || state.Expression is null
                || state.Expression.Length > 512 || state.Display is null || state.Display.Length > 512
                || state.History is null || state.History.Any(item => item is null || !double.IsFinite(item.Value)
                    || item.Expression is null || item.Expression.Length > 512)) return new();
            state.History = state.History.Take(20).ToList();
            return state;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { return new(); }
    }

    public static bool Save(string path, CalculatorSession state)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(state));
            File.Move(temporary, path, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        { return false; }
    }
}
