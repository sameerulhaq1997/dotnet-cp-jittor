using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace MacroEconomics.Shared.Helpers
{
    interface IDataCache
    {
        void SetData<T>(string cacheKey, T objectToCache, int expireCacheInSecs);
        T GetData<T>(string cacheKey);
        Task SetDataAsync<T>(string cacheKey, T objectToCache, int expireCacheInSecs);
        Task<T> GetDataAsync<T>(string cacheKey);
        void RemoveEntry(string cacheKey);
        bool ContainsKey(string cacheKey);
    }

    public abstract class DataCache
    {
        protected TimeSpan GetCacheExpirationTimeSpan(int seconds)
        {
            int h, m, s;
            h = m = 0;

            if (seconds >= 60)
            {
                m = seconds / 60;

                if (m >= 60)
                {
                    h = m / 60;
                    m %= 60;
                }

                s = (seconds % 60);
            }
            else
            {
                s = seconds;
            }

            return (new TimeSpan(h, m, s));
        }
    }

    public class InMemoryDataCache : DataCache, IDataCache
    {

        private static readonly InMemoryDataCache _singleton = new InMemoryDataCache();

        public static InMemoryDataCache Instance
        {
            get { return (_singleton); }
        }
        protected InMemoryDataCache()
        {

        }

        public void SetData<T>(string cacheKey, T objectToCache, int expireCacheInSecs)
        {
            // MSDN: Entry is updated if it exists
            MemoryCache.Default.Set(new CacheItem(cacheKey,
                                                  objectToCache),
                                    GetDataCacheItemPolicy(expireCacheInSecs));
        }
        public T GetData<T>(string cacheKey)
        {
            T result = (T)MemoryCache.Default[cacheKey];
            return result;
        }
        public void RemoveEntry(string cacheKey)
        {
            if (ContainsKey(cacheKey))
            {
                MemoryCache.Default.Remove(cacheKey);
            }
        }
        public bool ContainsKey(string cacheKey)
        {
            bool result = false;

            if (MemoryCache.Default.Contains(cacheKey))
            {
                result = true;
            }

            return (result);
        }

        private static CacheItemPolicy GetDataCacheItemPolicy(int absoluteExpirationInSeconds)
        {
            CacheItemPolicy cip = new CacheItemPolicy();
            try
            {
                cip.AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddSeconds(absoluteExpirationInSeconds));

            }
            catch (Exception)
            {

                throw;
            }
            return (cip);
        }

        public List<string> GetAllCacheKeys()
        {
            List<string> CacheKeys = new List<string>();
            foreach (KeyValuePair<string, object> item in MemoryCache.Default)
            {
                CacheKeys.Add(item.Key.ToString());
            }
            return CacheKeys;
        }

        public async Task SetDataAsync<T>(string cacheKey, T objectToCache, int expireCacheInSecs)
        {
            
                await Task.Run(() => {
                    MemoryCache.Default.Set(new CacheItem(cacheKey, objectToCache), GetDataCacheItemPolicy(expireCacheInSecs));
            });
        }

        public async Task<T> GetDataAsync<T>(string cacheKey)
        {
            return await Task.Run(() => {
                return (T)MemoryCache.Default[cacheKey];
            });
            
        }
    }
}
