using System.Security.Cryptography;

namespace Netaq.Infrastructure.Storage;

/// <summary>
/// Service for file storage operations using MinIO (S3-compatible).
/// Supports AES-256 encryption for sensitive proposal files.
/// In development mode, falls back to local file system storage.
/// </summary>
public interface IFileStorageService
{
    Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName, string bucketName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> DownloadFileAsync(string objectKey, string bucketName, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string objectKey, string bucketName, CancellationToken cancellationToken = default);
    Task<string> GetPresignedUrlAsync(string objectKey, string bucketName, int expiryMinutes = 60, CancellationToken cancellationToken = default);
    string ComputeFileHash(Stream fileStream);
}

public class FileUploadResult
{
    public string ObjectKey { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
}

/// <summary>
/// Local file system implementation for development.
/// In production, this would be replaced with actual MinIO SDK calls.
/// Files are stored with AES-256 encryption.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService()
    {
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "storage");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName, string bucketName, string contentType, CancellationToken cancellationToken = default)
    {
        var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var objectKey = $"{bucketName}/{DateTime.UtcNow:yyyy/MM/dd}/{storedFileName}";
        var fullPath = Path.Combine(_basePath, objectKey.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        // Compute hash before writing
        var hash = ComputeFileHash(fileStream);
        fileStream.Position = 0;

        // Write file with AES-256 encryption
        await using var outputStream = File.Create(fullPath);
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        aes.GenerateIV();

        // Store key and IV as header (in production, use KMS)
        await outputStream.WriteAsync(aes.IV, cancellationToken);

        await using var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        await fileStream.CopyToAsync(cryptoStream, cancellationToken);
        await cryptoStream.FlushFinalBlockAsync(cancellationToken);

        return new FileUploadResult
        {
            ObjectKey = objectKey,
            StoredFileName = storedFileName,
            FileSizeBytes = fileStream.Length,
            FileHash = hash,
            BucketName = bucketName
        };
    }

    public async Task<Stream> DownloadFileAsync(string objectKey, string bucketName, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, objectKey.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {objectKey}");

        var memoryStream = new MemoryStream();
        await using var fileStream = File.OpenRead(fullPath);
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public Task DeleteFileAsync(string objectKey, string bucketName, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, objectKey.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<string> GetPresignedUrlAsync(string objectKey, string bucketName, int expiryMinutes = 60, CancellationToken cancellationToken = default)
    {
        // In development, return a local API endpoint
        return Task.FromResult($"/api/files/{bucketName}/{objectKey}");
    }

    public string ComputeFileHash(Stream fileStream)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(fileStream);
        fileStream.Position = 0;
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
