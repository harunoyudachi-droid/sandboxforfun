using System.Security.Cryptography;

namespace FateDice;

public sealed record CheckResult(IReadOnlyList<int> Dice, int Modifier)
{
    public int DiceSum => Dice.Sum();
    public long Total => (long)Modifier + DiceSum;
}

public static class FateRoller
{
    public static IReadOnlyList<string> Approaches { get; } = Array.AsReadOnly(new[]
        { "Аккуратный", "Умный", "Эффектный", "Силовой", "Быстрый", "Тайный" });

    // Six equally likely physical faces: two plus, two minus, two blank.
    public static int FaceValue(int face) => face switch
    {
        0 or 1 => 1,
        2 or 3 => -1,
        4 or 5 => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(face))
    };

    public static CheckResult Roll(int modifier, Func<int>? nextFace = null)
    {
        nextFace ??= () => RandomNumberGenerator.GetInt32(6);
        var dice = new int[4];
        for (var i = 0; i < dice.Length; i++) dice[i] = FaceValue(nextFace());
        return new CheckResult(Array.AsReadOnly(dice), modifier);
    }

    public static string Signed(long value) => value > 0 ? "+" + value : value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public static string Symbol(int value) => value switch
    {
        1 => "+", -1 => "−", 0 => "", _ => throw new ArgumentOutOfRangeException(nameof(value))
    };
}
