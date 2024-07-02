using Jittor.App.DataServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jittor.App.Helpers
{
    public static class DataContexts
    {
        public static FrameworkRepository GetExceptionLoggingDataContext(bool enableAutoSelect = false)
        {
            return (GetNewDataContext("ELConnectionString", enableAutoSelect));
        }

        public static FrameworkRepository GetBackgroundProcessDataContext(ExecutionLogger elHelperInstance = null, bool enableAutoSelect = false)
        {
            return (GetNewDataContext("BGSConnectionString", enableAutoSelect, elHelperInstance));
        }

        public static FrameworkRepository GetBackgroundProcessDataContext(string connectionString, string provider, ExecutionLogger elHelperInstance = null, bool enableAutoSelect = false)
        {
            return (GetNewDataContext(connectionString, provider, enableAutoSelect, elHelperInstance));
        }

        public static FrameworkRepository GetJittorDataContext(ExecutionLogger elHelperInstance, bool enableAutoSelect = true)
        {
            return (GetNewDataContext("ConnectionStrings:SCConnectionString", enableAutoSelect, elHelperInstance));
        }
        public static FrameworkRepository GetLiveDBDataContext(ExecutionLogger elHelperInstance, bool enableAutoSelect = true)
        {
            return (GetNewDataContext("ConnectionStrings:ArgaamLiveDB", enableAutoSelect, elHelperInstance));
        }
        public static FrameworkRepository GetStreamerDataContext(ExecutionLogger elHelperInstance, bool enableAutoSelect = true)
        {
            return (GetNewDataContext("StreamerConnectionString", enableAutoSelect, elHelperInstance));
        }
        public static FrameworkRepository GetJittorDataContext(bool enableAutoSelect = true)
        {
            return (GetJittorDataContext(null, enableAutoSelect));
        }
        public static FrameworkRepository GetLiveDBDataContext(bool enableAutoSelect = true)
        {
            return (GetLiveDBDataContext(null, enableAutoSelect));
        }
        public static FrameworkRepository GetStreamerDataContext(bool enableAutoSelect = true)
        {
            return (GetStreamerDataContext(null, enableAutoSelect));
        }
        public static FrameworkRepository GetCPDataContext(ExecutionLogger elHelperInstance, bool enableAutoSelect = true)
        {
            return (GetNewDataContext("ConnectionStrings:CPConnectionString", enableAutoSelect, elHelperInstance));
        }

        public static FrameworkRepository GetCPDataContext(bool enableAutoSelect = true)
        {
            return (GetCPDataContext(null, enableAutoSelect));
        }

        private static FrameworkRepository GetNewDataContext(string connectionStringName, bool enableAutoSelect, ExecutionLogger elHelperInstance = null)
        {
            FrameworkRepository repository = new FrameworkRepository(connectionStringName)
            {
                EnableAutoSelect = enableAutoSelect,
                ELHelperInstance = elHelperInstance
            };

            return (repository);
        }
        private static FrameworkRepository GetNewDataContext(string connectionString, string providerName, bool enableAutoSelect, ExecutionLogger elHelperInstance = null)
        {
            FrameworkRepository repository = new FrameworkRepository(connectionString, providerName)
            {
                EnableAutoSelect = enableAutoSelect,
                ELHelperInstance = elHelperInstance
            };

            return (repository);
        }

       
    }
}
