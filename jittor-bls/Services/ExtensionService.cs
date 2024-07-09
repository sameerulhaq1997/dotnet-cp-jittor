

using Jittor.App.Models;

namespace Jittor.App.Services
{
    public static class ExtensionService
    {
        public static ApplicationValueTypeEnum GetApplicationValueTypeEnum(this string sqlType)
        {
            switch (sqlType.ToLower())
            {
                case "varchar":
                case "nvarchar":
                case "char":
                case "nchar":
                case "text":
                case "ntext":
                    return ApplicationValueTypeEnum.STRING;

                case "int":
                case "bigint":
                case "smallint":
                case "tinyint":
                case "decimal":
                case "numeric":
                case "float":
                case "real":
                    return ApplicationValueTypeEnum.NUMBER;

                case "bit":
                    return ApplicationValueTypeEnum.BOOL;

                default:
                    return ApplicationValueTypeEnum.OBJECT;
            }
        }
        public static T ParseEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }
        public static Dictionary<string, object> GetValuesFromDynamicDictionary(dynamic item, List<string> keys)
        {
            var results = new Dictionary<string, object>();
            if (item is IDictionary<string, object> dictionary)
            {
                foreach (var key in keys)
                {
                    results.Add(key, dictionary[key]);
                }
            }
            return results;
        }

    }
}
