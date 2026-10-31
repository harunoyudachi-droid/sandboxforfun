namespace SandboxCalculator;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length == 2 && args[0] == "--smoke-test")
        {
            try { DesktopSmokeTest.Run(args[1]); return 0; }
            catch (Exception ex)
            {
                Directory.CreateDirectory(args[1]);
                File.WriteAllText(Path.Combine(args[1], "error.txt"), ex.ToString());
                return 1;
            }
        }
        Application.Run(new CalculatorForm());
        return 0;
    }
}
