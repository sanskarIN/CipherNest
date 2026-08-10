namespace CipherNest.UiTests;

public sealed class GeneratorMemorySourceTests
{
    [Fact]
    public void Generator_ClearsTemporaryPasswordAndPassphraseArrays()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "PasswordGenerator.cs"));

        Assert.Contains("Array.Clear(chars);", source, StringComparison.Ordinal);
        Assert.Contains("Array.Clear(words);", source, StringComparison.Ordinal);
        Assert.Contains("RandomNumberGenerator.GetInt32", source, StringComparison.Ordinal);
    }

    private static string PathAt(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CipherNest.slnx"))) directory = directory.Parent;
        if (directory is null) throw new DirectoryNotFoundException("Could not locate the CipherNest repository root from the test output directory.");
        var path = directory.FullName;
        foreach (var segment in segments) path = Path.Combine(path, segment);
        return path;
    }
}
