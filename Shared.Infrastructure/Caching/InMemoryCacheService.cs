using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.Common.Interfaces.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Caching;

    // ============================================================
    // In-Memory Cache Service
    // Design Pattern: Strategy Pattern
    // สามารถเปลี่ยนเป็น Redis ได้โดยไม่ต้องแก้ code ที่เรียกใช้
    // ============================================================
    public class InMemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<InMemoryCacheService> _logger;
        private readonly HashSet<string> _cacheKeys = new();
        private readonly object _lock = new();

        public InMemoryCacheService(IMemoryCache cache, ILogger<InMemoryCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public Task<T?> GetAsync<T>(string key)
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                _logger.LogDebug("Cache HIT for key: {Key}", key);
                return Task.FromResult(value);
            }

            _logger.LogDebug("Cache MISS for key: {Key}", key);
            return Task.FromResult(default(T));
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5),
                SlidingExpiration = TimeSpan.FromMinutes(2)
            };

            _cache.Set(key, value, options);

            lock (_lock)
            {
                _cacheKeys.Add(key);
            }

            _logger.LogDebug("Cache SET for key: {Key}, Expiration: {Expiration}", key, expiration);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            lock (_lock)
            {
                _cacheKeys.Remove(key);
            }

            _logger.LogDebug("Cache REMOVE for key: {Key}", key);
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix)
        {
            List<string> keysToRemove;
            lock (_lock)
            {
                keysToRemove = _cacheKeys.Where(k => k.StartsWith(prefix)).ToList();
            }

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                lock (_lock)
                {
                    _cacheKeys.Remove(key);
                }
            }

            _logger.LogDebug("Cache REMOVE BY PREFIX: {Prefix}, Removed: {Count}", prefix, keysToRemove.Count);
            return Task.CompletedTask;
        }
    }


