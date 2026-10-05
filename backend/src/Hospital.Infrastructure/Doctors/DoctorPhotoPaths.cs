namespace Hospital.Infrastructure.Doctors;

public static class DoctorPhotoPaths
{
    public static void Apply(DoctorPhotoOptions options, string contentRoot, string webRoot)
    {
        if (options.MaxBytes < 1024 || options.MaxBytes > DoctorPhotoOptions.DefaultMaxBytes)
        {
            options.MaxBytes = DoctorPhotoOptions.DefaultMaxBytes;
        }

        var storage = string.IsNullOrWhiteSpace(options.StoragePath)
            ? Path.GetFullPath(Path.Combine(contentRoot, "..", "data", "doctor-photos"))
            : Path.IsPathRooted(options.StoragePath)
                ? Path.GetFullPath(options.StoragePath)
                : Path.GetFullPath(Path.Combine(contentRoot, options.StoragePath));

        if (IsInsideWebRoot(storage, webRoot))
        {
            throw new InvalidOperationException("Doctor photo storage must be outside the web root.");
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
