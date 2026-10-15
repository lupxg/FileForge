using FileForge.Application.Abstractions;

namespace FileForge.Infrastructure.FileStorage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("A storage directory must be configured.", nameof(rootPath));
        }

        _rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        var safeFileName = Path.GetFileName(fileName);

        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new ArgumentException(
                "A valid file name is required.",
                nameof(fileName));
        }

        var extension = Path.GetExtension(safeFileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_rootPath, storedFileName);

        try
        {
            await using var destination = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await content.CopyToAsync(destination, cancellationToken);

            return fullPath;
        }
        catch
        {
            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch
            {
                // Preserve the original save exception.
            }

            throw;
        }
    }

    public Task DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ArgumentException(
                "A storage path is required.",
                nameof(storagePath));
        }

        var fullPath = Path.GetFullPath(storagePath);

        // Do not allow deletion outside the configured storage directory.
        var relativePath = Path.GetRelativePath(_rootPath, fullPath);

        if (relativePath == ".." ||
            relativePath.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal) ||
            Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException(
                "The file is outside the configured storage directory.");
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}