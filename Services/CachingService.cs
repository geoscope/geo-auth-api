using System;
using System.Threading.Tasks;
using geo_auth_api.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace geo_auth_api.Services
{
    public class CachingService : ICachingService
    {
        private readonly IDistributedCache cache;

        public CachingService(IDistributedCache _cache)
        {
            cache = _cache;
        }

        public async Task CreateUserSession(long ownerId, string username, string token)
        {
            var cacheOptions = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromDays(1));

            await cache.SetStringAsync($"{ownerId}:{username}", token, cacheOptions);
        }

        public async Task GetCurrentUserSession(long ownerId, string userName)
        {
            await cache.GetStringAsync($"{ownerId}:{userName}");
        }

        public async Task DestroyUserSession(long ownerId, string userName)
        {
            await cache.RemoveAsync($"{ownerId}:{userName}");
        }

        public Task ExtendUserSession(long ownerId, string userName, string token)
        {
            throw new NotImplementedException();
        }
    }
}