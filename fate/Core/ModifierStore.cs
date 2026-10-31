using System.Text.Json;

namespace FateDice;

public static class ModifierStore
{
    public static int[] Load(string path)
    {
        try
        {
            if (!File.Exists(path) || new FileInfo(path).Length > 4096) return new int[6];
            var values = JsonSerializer.Deserialize<int[]>(File.ReadAllText(path));
            return values is { Length: 6 } ? values : new int[6];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { return new int[6]; }
    }

    public static bool Save(string path, int[] modifiers)
    {
        if (modifiers.Length != 6) throw new ArgumentException("Нужны шесть модификаторов", nameof(modifiers));
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(modifiers));
            File.Move(path + ".tmp", path, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
    }
}
