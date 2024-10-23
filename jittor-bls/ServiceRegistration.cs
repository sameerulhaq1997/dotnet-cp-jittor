using Jittor.App.DataServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetaPoco;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jittor.App
{
    public static class ServiceRegistation
    {
        public static void AddJittorApp(this IServiceCollection services, Dictionary<string, string> connectionStrings, int poolSize)
        {
            services.AddSingleton<BlockingCollection<Database>>();
            //Register Services
            services.AddSingleton<DatabasePoolManager>(provider =>
            {
                //var configurationBuilder = new ConfigurationBuilder();
                //string path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                //configurationBuilder.AddJsonFile(path, false);
                //string jittorConnectionString = connectionString;
                var dbPool = provider.GetService<BlockingCollection<Database>>();
                return new DatabasePoolManager(poolSize, connectionStrings, dbPool);
            });
        }
    }
}
