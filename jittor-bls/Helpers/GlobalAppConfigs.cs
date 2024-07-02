using System;
using System.Collections.Generic;
using System.Text;

namespace Jittor.App.Helpers
{
    public static class GlobalAppConfigs
    {
        #region REDIS Settings

        public static string RedisPassword
        {
            get
            {
                return (AppConfigs.GetConfigValue<string>("G_REDIS_PASSWORD"));
            }
        }

        public static int RedisConnectionPoolSize
        {
            get
            {
                return (AppConfigs.GetConfigValue<int>("G_REDIS_CONN_POOL_SIZE"));
            }
        }

        public static string RedisServerIP
        {
            get
            {
                return (AppConfigs.GetConfigValue<string>("G_REDIS_SERVER_IP"));
            }
        }

        public static int RedisServerPort
        {
            get
            {
                return (AppConfigs.GetConfigValue<int>("G_REDIS_SERVER_PORT"));
            }
        }

        public static int RedisDbID
        {
            get
            {
                return (AppConfigs.GetConfigValue<int>("G_REDIS_DB_ID"));
            }
        }
        public static bool UseRedisServer
        {
            get
            {
                return (AppConfigs.GetConfigValue<bool>("G_USE_REDIS"));
            }
        }
        public static bool UseCachedData
        {
            get
            {
                return (AppConfigs.GetConfigValue<bool>("G_USE_CACHED_DATA"));
            }
        }
        #endregion
    }
}
