using System.Text.Json;
using StackExchange.Redis;

namespace Netaq.Infrastructure.Cache;

/// <summary>
/// Redis cache service for session management, refresh tokens, and permission matrix caching.
/// Redis is password-protected and isolated within internal Docker network.
/// </summary>
public interface ICacheService
{
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<T?> GetAsync<T>(string key);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task SetRefreshTokenAsync(Guid userId, string refreshToken, TimeSpan expiry);
    Task<string?> GetRefreshTokenAsync(Guid userId);
    Task RemoveRefreshTokenAsync(Guid userId);
}

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = redis.GetDatabase();
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, expiry);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await _db.StringGetAsync(key);
        if (json.IsNullOrEmpty)
            return default;
        return JsonSerializer.Deserialize<T>(json!);
    }

    public async Task RemoveAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _db.KeyExistsAsync(key);
    }

    public async Task SetRefreshTokenAsync(Guid userId, string refreshToken, TimeSpan expiry)
    {
        var key = $"refresh_token:{userId}";
        await _db.StringSetAsync(key, refreshToken, expiry);
    }

    public async Task<string?> GetRefreshTokenAsync(Guid userId)
    {
        var key = $"refresh_token:{userId}";
        var token = await _db.StringGetAsync(key);
        return token.IsNullOrEmpty ? null : token.ToString();
    }

    public async Task RemoveRefreshTokenAsync(Guid userId)
    {
        var key = $"refresh_token:{userId}";
        await _db.KeyDeleteAsync(key);
    }
}
