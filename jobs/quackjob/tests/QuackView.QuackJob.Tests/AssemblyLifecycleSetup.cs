namespace TypoDukk.QuackView.QuackJob.Tests;

[TestClass]
public static class AssemblyLifecycleSetup
{
    public static readonly string TempDir;
    public static readonly string QuackViewDir;
    static AssemblyLifecycleSetup()
    {
        var timestamp = DateTime.Now.Ticks.ToString();
        AssemblyLifecycleSetup.TempDir = Environment.GetEnvironmentVariable("TEMP") ?? "./tmp";
        AssemblyLifecycleSetup.QuackViewDir = Path.Combine(TempDir, "QuackView", timestamp);
    }

    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        Directory.CreateDirectory(AssemblyLifecycleSetup.QuackViewDir);

        Environment.SetEnvironmentVariable("QUACKVIEW_DIR", AssemblyLifecycleSetup.QuackViewDir);
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        Directory.Delete(AssemblyLifecycleSetup.QuackViewDir, true);
        Environment.SetEnvironmentVariable("QUACKVIEW_DIR", null);
    }
}