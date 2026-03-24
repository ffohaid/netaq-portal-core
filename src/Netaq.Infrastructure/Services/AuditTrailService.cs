using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netaq.Domain.Entities;
using Netaq.Domain.Enums;
using Netaq.Infrastructure.Persistence;

namespace Netaq.Infrastructure.Services;

/// <summary>
/// Immutable Audit Trail service with cryptographic hash chain.
/// Hash = SHA-256(CurrentEntryData + PreviousEntryHash + UserIdentity + Timestamp)
/// </summary>
public interface IAuditTrailService
{
    Task LogAsync(
        Guid organizationId,
        Guid? userId,
        AuditActionCategory category,
        string actionType,
        string description,
        string? entityType = null,
        Guid? entityId = null,
        object? oldValues = null,
        object? newValues = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);
    
    Task<bool> VerifyChainIntegrityAsync(Guid organizationId, CancellationToken cancellationToken = default);
}

public class AuditTrailService : IAuditTrailService
{
    private readonly ApplicationDbContext _context;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public AuditTrailService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        Guid organizationId,
        Guid? userId,
        AuditActionCategory category,
        string actionType,
        string description,
        string? entityType = null,
        Guid? entityId = null,
        object? oldValues = null,
        object? newValues = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Get the last entry's hash for chain linking
            var lastEntry = await _context.AuditLogs
                .IgnoreQueryFilters()
                .Where(a => a.OrganizationId == organizationId)
                .OrderByDescending(a => a.SequenceNumber)
                .FirstOrDefaultAsync(cancellationToken);

            var previousHash = lastEntry?.Hash ?? "GENESIS";
            var sequenceNumber = (lastEntry?.SequenceNumber ?? 0) + 1;
            var timestamp = DateTime.UtcNow;

            var entry = new AuditLog
            {
                OrganizationId = organizationId,
                UserId = userId,
                ActionCategory = category,
                ActionType = actionType,
                ActionDescription = description,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = timestamp,
                PreviousHash = previousHash,
                SequenceNumber = sequenceNumber
            };

            // Compute cryptographic hash: SHA-256(Data + PreviousHash + UserId + Timestamp)
            entry.Hash = ComputeHash(entry, previousHash);

            _context.AuditLogs.Add(entry);
            await _context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> VerifyChainIntegrityAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var entries = await _context.AuditLogs
            .IgnoreQueryFilters()
            .Where(a => a.OrganizationId == organizationId)
            .OrderBy(a => a.SequenceNumber)
            .ToListAsync(cancellationToken);

        if (!entries.Any())
            return true;

        string previousHash = "GENESIS";
        
        foreach (var entry in entries)
        {
            // Verify previous hash link
            if (entry.PreviousHash != previousHash)
                return false;

            // Recompute and verify hash
            var computedHash = ComputeHash(entry, previousHash);
            if (entry.Hash != computedHash)
                return false;

            previousHash = entry.Hash;
        }

        return true;
    }

    private static string ComputeHash(AuditLog entry, string previousHash)
    {
        var data = $"{entry.ActionType}|{entry.ActionDescription}|{entry.EntityType}|{entry.EntityId}|" +
                   $"{entry.OldValues}|{entry.NewValues}|{previousHash}|{entry.UserId}|" +
                   $"{entry.Timestamp:O}|{entry.SequenceNumber}";
        
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
