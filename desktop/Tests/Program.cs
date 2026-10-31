using SandboxCalculator;

var count = 0;
void Require(bool condition, string name)
{
    count++;
    if (!condition) throw new Exception("Failed: " + name);
}
void Check(string text, double expected, bool degrees = true)
{
    var actual = ExpressionEngine.Evaluate(text, degrees, 7);
    Require(Math.Abs(actual - expected) <= 1e-10 * Math.Max(1, Math.Abs(expected)), text);
}
void Invalid(string text)
{
    try { ExpressionEngine.Evaluate(text); }
    catch (ArgumentException) { count++; return; }
    throw new Exception("Expected error: " + text);
}
Check("2+3*4", 14); Check("(2+3)*4", 20); Check("7/2-1", 2.5);
Check("-2^2", -4); Check("(-2)^2", 4); Check("2^-2", .25);
Check("2^3^2", 512); Check("2--3", 5); Check(".5+1,5", 2);
Check(" 2 × 3 − 8 ÷ 4 ", 4); Check("sin(30)", .5); Check("cos(60)", .5);
Check("tan(45)", 1); Check("asin(.5)", 30); Check("acos(.5)", 60);
Check("atan(1)", 45); Check("sqrt(81)+log(100)+ln(e)", 12);
Check("abs(-4)+exp(0)", 5); Check("sin(cos(0)*30)", .5);
Check("sin(pi/2)", 1, false); Check("asin(1)", Math.PI / 2, false);
Check("π", Math.PI); Check("e", Math.E); Check("Ans*2", 14);
Check("0!+5!", 121); Check("3!^2", 36); Check("200*10%", 20);
Check("100+10%", 100.1); Check("1.5E3+2e-2", 1500.02);
foreach (var text in new[] { "", " ", "1/0", "0/0", "sqrt(-1)", "ln(0)", "log(-1)",
    "asin(2)", "acos(-2)", "tan(90)", "(-1)!", "2.5!", "171!", "1e309", "exp(1000)",
    "(-2)^0.5", "2+", "(2+3", "2+3)", "2(3)", "2pi", "sin30", "bad(1)", "1.2.3", "1e",
    "1e+", "NaN", "1;quit()", new string('(', 100) + "1" + new string(')', 100), new string('-', 100) + "1",
    string.Concat(Enumerable.Repeat("1+", 300)) + "1" }) Invalid(text);

var session = new CalculatorSession();
session.Calculate("1/3");
Require(session.Calculate("Ans*3") == 1, "Ans retains full precision");
var before = session.Answer;
try { session.Calculate("1/0"); } catch (ArgumentException) { }
Require(session.Answer == before && session.History.Count == 2, "failed calculation preserves answer and history");
for (var i = 0; i < 30; i++) session.Calculate(i.ToString());
Require(session.History.Count == 20 && session.History[0].Value == 29, "history cap and order");
session.Degrees = false; session.Calculate("sin(pi/2)");
var radians = session.History[0];
session.Degrees = true; session.Recall(radians);
Require(!session.Degrees && session.Answer == 1 && session.Expression == "sin(pi/2)", "history restores mode and answer");
Require(CalculatorSession.Format(1.25) == "1.25" && CalculatorSession.Format(-0d) == "0", "invariant display");

var temporary = Path.Combine(Path.GetTempPath(), "calculator-tests-" + Guid.NewGuid());
try
{
    var path = Path.Combine(temporary, "state.json");
    Require(SessionStore.Load(path).History.Count == 0, "missing state");
    Require(SessionStore.Save(path, session), "save state");
    var restored = SessionStore.Load(path);
    Require(restored.Expression == session.Expression && restored.Answer == session.Answer
        && restored.Degrees == session.Degrees && restored.History.Count == 20, "state round trip");
    File.WriteAllText(path, "not JSON");
    Require(SessionStore.Load(path).History.Count == 0, "corrupt JSON");
    File.WriteAllText(path, "{\"History\":null}");
    Require(SessionStore.Load(path).History.Count == 0, "null collection");
    File.WriteAllText(path, "{\"History\":[null]}");
    Require(SessionStore.Load(path).History.Count == 0, "null history entry");
    File.WriteAllText(path, new string('x', 1_000_001));
    Require(SessionStore.Load(path).History.Count == 0, "oversize file");
    session.Calculate("2+2");
    Require(SessionStore.Save(path, session) && SessionStore.Load(path).Answer == 4, "atomic replacement");
}
finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true); }
Console.WriteLine($"{count} core checks passed.");
