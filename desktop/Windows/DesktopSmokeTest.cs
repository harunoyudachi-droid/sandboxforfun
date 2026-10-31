namespace SandboxCalculator;

internal static class DesktopSmokeTest
{
    internal static void Run(string output)
    {
        Directory.CreateDirectory(output);
        using var form = new CalculatorForm(persist: false);
        form.Show();
        Application.DoEvents();
        foreach (var key in new[] { "sin", "3", "0", ")", "=" }) form.ClickKey(key);
        Require(form.ResultText == "0.5", "sin(30) button flow");
        Require(form.HistoryCount == 1, "history added");
        form.ClickKey("AC");
        form.ClickKey("Ans"); form.ClickKey("×"); form.ClickKey("2"); form.ClickKey("=");
        Require(form.ResultText == "1", "Ans button flow");
        form.SetExpression("1/0"); form.ClickKey("=");
        Require(form.ResultText == "Ошибка" && form.HistoryCount == 2, "domain error kept out of history");
        form.SetExpression("123"); form.SelectExpression(1, 1); form.ClickKey("9");
        Require(form.ExpressionText == "193", "selection replacement");
        form.ClickKey("⌫"); Require(form.ExpressionText == "13", "backspace at cursor");
        form.ToggleMode(); Require(!form.Degrees, "RAD toggle");
        form.SetExpression("sin(pi/2)"); form.ClickKey("=");
        Require(form.ResultText == "1", "RAD evaluation");
        form.ToggleMode(); form.RecallFirst(); Require(!form.Degrees, "history restores RAD");
        form.SetExpression("sqrt(81)+log(100)+5!"); form.ClickKey("=");
        Require(form.ResultText == "131", "scientific evaluation");
        Application.DoEvents();
        Save(form, Path.Combine(output, "desktop.png"));
        form.Size = form.MinimumSize;
        Application.DoEvents();
        Save(form, Path.Combine(output, "desktop-small.png"));
        File.WriteAllText(Path.Combine(output, "smoke-test.txt"), "Desktop startup, keypad, selection editing, domains, history and DEG/RAD checks passed.");
        form.Close();
    }
    private static void Require(bool condition, string name)
    { if (!condition) throw new InvalidOperationException("Failed: " + name); }
    private static void Save(Form form, string path)
    {
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, form.ClientRectangle with { Width = form.Width, Height = form.Height });
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }
}
