namespace Hospital.Infrastructure.Documents;

public static class MedicalDocumentPaths
{
    public static void Apply(MedicalDocumentOptions options, string contentRoot, string webRoot)
    {
        options.MaxBytes = MedicalDocumentOptions.NormalizeMaxBytes(options.MaxBytes);

        var storage = string.IsNullOrWhiteSpace(options.StoragePath)
            ? Path.GetFullPath(Path.Combine(contentRoot, "..", "data", "medical-documents"))
            : Path.IsPathRooted(options.StoragePath)
                ? Path.GetFullPath(options.StoragePath)
                : Path.GetFullPath(Path.Combine(contentRoot, options.StoragePath));

        if (IsInsideWebRoot(storage, webRoot))
        {
            throw new InvalidOperationException("Medical document storage must be outside the web root.");
        }

        options.StoragePath = storage;
    }

    public static bool IsInsideWebRoot(string storagePath, string webRoot)
    {
        var storage = Normalize(storagePath);
        var web = Normalize(webRoot);
        if (string.Equals(storage, web, PathComparison))
        {
            return true;
        }

        return storage.StartsWith(web + Path.DirectorySeparatorChar, PathComparison);
    }

    private static string Normalize(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
