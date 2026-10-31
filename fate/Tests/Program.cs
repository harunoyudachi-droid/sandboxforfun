using FateDice;

var checks = 0;
void Require(bool condition, string name)
{
    checks++;
    if (!condition) throw new Exception("Failed: " + name);
}
var faces = Enumerable.Range(0, 6).Select(FateRoller.FaceValue).ToArray();
Require(faces.Count(v => v == 1) == 2 && faces.Count(v => v == -1) == 2 && faces.Count(v => v == 0) == 2,
    "two plus, two minus, two blank physical faces");
foreach (var invalid in new[] { -1, 6 })
{
    try { FateRoller.FaceValue(invalid); throw new Exception("Invalid face accepted"); }
    catch (ArgumentOutOfRangeException) { checks++; }
}
var distribution = new int[9];
// Exhaust all 6^4 physical outcomes; no random statistical/flaky assertions.
for (var outcome = 0; outcome < 1296; outcome++)
{
    var remaining = outcome;
    var draws = 0;
    var check = FateRoller.Roll(-3, () => { draws++; var face = remaining % 6; remaining /= 6; return face; });
    var encoded = outcome;
    var expected = 0;
    for (var die = 0; die < 4; die++) { expected += faces[encoded % 6]; encoded /= 6; }
    Require(draws == 4 && check.Dice.Count == 4 && check.DiceSum == expected && check.Total == expected - 3,
        "four-die sum and negative modifier: " + outcome);
    distribution[check.DiceSum + 4]++;
}
Require(distribution.SequenceEqual(new[] { 16, 64, 160, 256, 304, 256, 160, 64, 16 }), "full 4dF distribution");
Require(FateRoller.Roll(int.MaxValue, () => 0).Total == (long)int.MaxValue + 4, "positive total cannot overflow");
Require(FateRoller.Roll(int.MinValue, () => 2).Total == (long)int.MinValue - 4, "negative total cannot overflow");
Require(FateRoller.Roll(0, () => 4).Total == 0, "all blanks with zero modifier");
Require(FateRoller.Symbol(0) == "" && FateRoller.Symbol(1) == "+" && FateRoller.Symbol(-1) == "−", "face symbols");
Require(FateRoller.Approaches.SequenceEqual(new[] { "Аккуратный", "Умный", "Эффектный", "Силовой", "Быстрый", "Тайный" }), "six named independent approaches");
for (var i = 0; i < 100; i++)
{
    var random = FateRoller.Roll(2);
    Require(random.Dice.Count == 4 && random.Dice.All(v => v is -1 or 0 or 1) && random.Total == 2 + random.Dice.Sum(), "production random source");
}
var folder = Path.Combine(Path.GetTempPath(), "fate-tests-" + Guid.NewGuid());
try
{
    var path = Path.Combine(folder, "modifiers.json");
    Require(ModifierStore.Load(path).SequenceEqual(new int[6]), "first run defaults");
    int[] values = [-3, -2, -1, 0, 1, 2];
    Require(ModifierStore.Save(path, values) && ModifierStore.Load(path).SequenceEqual(values), "save each modifier independently");
    values[2] = 7;
    Require(ModifierStore.Save(path, values) && ModifierStore.Load(path)[2] == 7, "replace saved modifiers");
    foreach (var corrupt in new[] { "not json", "null", "[1,2]", "[1,2,3,4,5,2147483648]", new string('x', 4097) })
    {
        File.WriteAllText(path, corrupt);
        Require(ModifierStore.Load(path).SequenceEqual(new int[6]), "recover from invalid settings");
    }
}
finally { if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true); }
Console.WriteLine($"{checks} Fate checks passed.");
