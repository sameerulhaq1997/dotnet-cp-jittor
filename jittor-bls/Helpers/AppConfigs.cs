using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using MacroEconomics.Shared.DataServices;
using static MacroEconomics.Shared.DataServices.FrameworkRepository;

namespace MacroEconomics.Shared.Helpers
{
    public class AppConfigs
    {
         
        static AppConfigs()
        {
            using FrameworkRepository db = DataContexts.GetBackgroundProcessDataContext(null, true);
            PetaPoco.Sql pSql = PetaPoco.Sql.Builder.Select("*").From("AppConfigs");
            List<AppConfig> list = db.Fetch<AppConfig>(pSql);

            foreach (AppConfig item in list)
            {
                GlobalVariables.AppConfigs.Add(item.AppConfigKey, item.AppConfigValue);
            }
        }
        public static bool Contain(string configKey)
        {
            return GlobalVariables.AppConfigs.ContainsKey(configKey) || GlobalVariables.AppConfigs.ContainsKey(configKey);
        }
        public static T GetConfigValue<T>(string configKey)
        {
      
            ExecutionLogger elh = new ExecutionLogger();

            if (!GlobalVariables.AppConfigs.ContainsKey(configKey))
            {
                ApplicationException ae = new ApplicationException("No such App Configuration key found: " + configKey);
                elh.LogException(ae);

                throw (ae);
            }

            try
            {
                return ConvertValue<T>(GlobalVariables.AppConfigs[configKey]);
            }
            catch (Exception e)
            {
                ApplicationException ae = new ApplicationException(string.Format("Cannot convert App Configuration value to {0}", typeof(T)), e);
                elh.LogException(ae);

                throw (ae);
            }
        }

        //It will convert the string to anything the user asked for
        static T ConvertValue<T>(string value)
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }


    }
}
