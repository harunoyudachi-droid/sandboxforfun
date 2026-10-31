namespace FateDice;

internal static class SmokeTest
{
    internal static void Run(string output)
    {
        Directory.CreateDirectory(output);
        var calls = 0;
        int NextFace()
        {
            var call = calls++;
            return call < 24 ? new[] { 0, 2, 4, 1 }[call % 4] : call < 28 ? 3 : 5;
        }
        using var form = new FateForm(persist: false, nextFace: NextFace);
        form.Show(); Application.DoEvents();
        for (var approach = 0; approach < 6; approach++)
        {
            Require(form.TotalText(approach) == "—", "no result before roll");
            form.SetModifier(approach, approach - 3);
            form.ClickRoll(approach);
            Require(form.FaceText(approach, 0) == "+" && form.FaceText(approach, 1) == "−"
                && form.FaceText(approach, 2) == "" && form.FaceText(approach, 3) == "+", "four visible faces");
            Require(form.Result(approach)?.Total == approach - 2, "correct approach modifier");
            Require(form.TotalText(approach) == FateRoller.Signed(approach - 2), "displayed total");
        }
        Require(calls == 24, "four independent draws per click");
        form.ClickRoll(0);
        Require(calls == 28 && form.Result(0)?.Total == -7, "fresh roll replaces previous faces");
        Require(form.Result(1)?.Total == -1, "other rows unaffected");
        form.SetModifier(0, 8);
        Require(form.Result(0) is null && form.TotalText(0) == "—", "modified value clears stale roll");
        form.TypeModifier(5, "-7"); form.ClickRoll(5);
        Require(form.Result(5)?.Modifier == -7 && form.TotalText(5) == "-7", "typed modifier committed");
        Application.DoEvents(); Save(form, Path.Combine(output, "fate-desktop.png"));
        form.Size = form.MinimumSize;
        Application.DoEvents(); Save(form, Path.Combine(output, "fate-desktop-small.png"));
        File.WriteAllText(Path.Combine(output, "smoke-test.txt"),
            "Six approach buttons, signed modifiers, typed numeric input, four die faces, blank faces, totals, re-roll and stale-result clearing passed.");
        form.Close();
    }
    private static void Require(bool condition, string name)
    { if (!condition) throw new InvalidOperationException("Failed: " + name); }
    private static void Save(Form form, string path)
    {
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }
}
