using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using StackExchange.Redis;

public interface ICacheService
{
    void Set<T>(string key, T value);
    T Get<T>(string key);
    void Remove(string key);
}

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;

    public MemoryCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public void Set<T>(string key, T value)
    {
        _memoryCache.Set(key, value);
    }

    public T Get<T>(string key)
    {
        return _memoryCache.Get<T>(key);
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
    }
}

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _redisDatabase;

    public RedisCacheService(IConnectionMultiplexer redisConnection)
    {
        _redisDatabase = redisConnection.GetDatabase();
    }

    public void Set<T>(string key, T value)
    {
        var jsonValue = JsonConvert.SerializeObject(value);
        _redisDatabase.StringSet(key, jsonValue);
    }

    public T Get<T>(string key)
    {
        var jsonValue = _redisDatabase.StringGet(key);
        if (jsonValue.HasValue)
        {
            return JsonConvert.DeserializeObject<T>(jsonValue);
        }
        return default(T);
    }

    public void Remove(string key)
    {
        _redisDatabase.KeyDelete(key);
    }
}
