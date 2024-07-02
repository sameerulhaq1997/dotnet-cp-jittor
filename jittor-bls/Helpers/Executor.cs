using Jittor.App.Helpers;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jittor.App
{
    public class Executor
    {

        #region Singlton 
        private static readonly Executor _singleton = new Executor();
        public static Executor Instance
        {
            get { return (_singleton); }
        }
        protected Executor()
        {
        }
        #endregion

        /// <summary>
        /// Get Data will execute the code block and if caching is enabled it will cache for the duration specified or for 30 Second if not specified.
        /// </summary>
        /// <typeparam name="T">Code block return type</typeparam>
        /// <param name="codeBlock"></param>
        /// <param name="paramWithValues">Parameteres will values that will change the record set in the database, will be used in Cache key generation.</param>
        /// <param name="cacheDuration">0 Second will disable caching for any method, default is 30 seconds.</param>
        /// <returns></returns>
        public T GetData<T>(Func<T> codeBlock, dynamic paramWithValues, int cacheDuration = 30) 
        {
            ///TODO: Should we use CallerInfo attributes, which is not reflection based but Compile time attributes ?  
            if (Executor.UseCaching && cacheDuration > 0) //Cacheing is enabled ?
            {
                var method = codeBlock.Method;
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}.{1}:-", method.ReflectedType.FullName, method.Name).Append(Convert.ToString(paramWithValues));
                string cacheKey = sb.ToString();
                if (DataCache.ContainsKey(cacheKey))
                {
                    return DataCache.GetData<T>(cacheKey);
                }
                try
                {
                    T result = codeBlock();

                    if(result != null)
                    {
                        DataCache.SetData(cacheKey, result, cacheDuration);
                    }

                    return (result);
                }
                catch (Exception)
                {
                    throw;
                    //return default;
                }
            }
            else
            {
                return codeBlock();
            }
        }

        public async Task<T> GetDataAsync<T>(Func<T> codeBlock, dynamic paramWithValues, int cacheDuration = 30)
        {
            if (Executor.UseCaching) //Cacheing is enabled ?
            {
                var method = codeBlock.Method;
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}.{1}:-", method.ReflectedType.FullName, method.Name).Append(Convert.ToString(paramWithValues));
                string cacheKey = sb.ToString();
                
                if (DataCache.ContainsKey(cacheKey))
                {
                    return await DataCache.GetDataAsync<T>(cacheKey);
                }

                try
                {
                    T result;
                    result = await Task.Run(() => {
                        return codeBlock();
                    });
                    if (result == null)
                    {
                        return result;
                    }
                    await DataCache.SetDataAsync(cacheKey, result, cacheDuration);
                    return (result);
                }
                catch (Exception)
                {
                    return default;
                }
            }
            else
            {
                return await Task.Run(() => {
                    return codeBlock();
                });
            }
        }

        #region Propertheees
        private static bool UseCaching
        {
            get 
            {
                return true;//bool.Parse(AppConfigs.AppSettingsJson.GetSection("AppSettings:UseCaching").Value);
            }
        }
        private static IDataCache DataCache
        {
            get { return  InMemoryDataCache.Instance;  }
        }

        #endregion
    }
}
