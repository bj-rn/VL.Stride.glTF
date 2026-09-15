// This fixture loads the package documents in a headless VL host. It checks that the
// documents compile without errors. The fixture finds every patch below help/
// automatically. A new help patch is covered as soon as the file exists.
//
// Keep preCompilePackages true. Without it the VL compiler does not load the runtime
// assemblies of the referenced packages. The computation of imported pin defaults then
// fails when a referenced assembly declares an enum parameter default.
//
// The fixture is Explicit. The test host of vvvv 8.0 preview 2026.8.0-0123 cannot resolve
// the VL.Skia version inside the shipped VL.Stride.Windows document. Run the fixture with
// dotnet test --filter FullyQualifiedName~VlDocumentTests when a vvvv build fixes this.

using NUnit.Framework;
using VL.TestFramework;

namespace VL.Stride.glTF.Tests;

[TestFixture]
[Explicit("vvvv 8.0 preview: the test host does not resolve the VL.Skia version of VL.Stride.Windows.vl")]
public class VlDocumentTests
{
    private TestEnvironment? testEnvironment;

    // The vvvv installation for this library. Set the VVVV_DIR environment variable to use a different one.
    private static string VvvvDir =>
        Environment.GetEnvironmentVariable("VVVV_DIR") ?? @"D:\_vvvv\vvvv_gamma_8.0-0123";

    // Keep the setup non async. An async setup breaks the NUnit synchronization context.
    [OneTimeSetUp]
    public void Setup()
    {
        // The search paths work like the vvvv option --package-repositories.
        var searchPaths = new[]
        {
            Path.GetDirectoryName(TestPaths.RepoRoot)!, // The parent folder that contains this package.
        };
        testEnvironment = TestEnvironmentLoader.Load(Path.Combine(VvvvDir, "vvvv.exe"), searchPaths, preCompilePackages: true);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        testEnvironment?.Dispose();
        testEnvironment = null;
    }

    [Test]
    public async Task MainDocumentCompiles()
    {
        await testEnvironment!.LoadAndTestAsync(Path.Combine(TestPaths.RepoRoot, "VL.Stride.glTF.vl"));
    }

    [TestCaseSource(nameof(HelpPatches))]
    public async Task HelpPatchCompiles(string path)
    {
        await testEnvironment!.LoadAndTestAsync(path);
    }

    public static IEnumerable<string> HelpPatches()
    {
        var helpDirectory = Path.Combine(TestPaths.RepoRoot, "help");
        if (!Directory.Exists(helpDirectory))
            yield break;

        foreach (var file in Directory.EnumerateFiles(helpDirectory, "*.vl", SearchOption.AllDirectories))
            yield return file;
    }
}
