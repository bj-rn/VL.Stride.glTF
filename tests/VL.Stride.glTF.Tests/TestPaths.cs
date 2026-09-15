namespace VL.Stride.glTF.Tests;

/// <summary>Finds paths that are relative to the repository root.</summary>
internal static class TestPaths
{
    /// <summary>The repository root. Found by walking up from the test output directory.</summary>
    public static string RepoRoot { get; } = FindRepoRoot();

    /// <summary>The folder with the real test assets.</summary>
    public static string AssetsDirectory => Path.Combine(RepoRoot, "tests", "assets");

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VL.Stride.glTF.vl")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"No 'VL.Stride.glTF.vl' file was found above '{AppContext.BaseDirectory}'.");
    }
}
