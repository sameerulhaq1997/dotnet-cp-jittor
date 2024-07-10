

using Jittor.App.Models;
using PetaPoco;

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
        public static Sql BuildWhereClause(this Sql sql, PageFilterModel filter)
        {
            switch (filter.Operator.ToLower())
            {
                case "contains":
                    filter.Operator = "LIKE";
                    filter.Value = "%" + filter.Value + "%";
                    break;

                case "equals":
                    filter.Operator = "=";
                    break;

                case "startswith":
                    filter.Operator = "LIKE";
                    filter.Value = filter.Value + "%";
                    break;

                case "endswith":
                    filter.Operator = "LIKE";
                    filter.Value = "%" + filter.Value;
                    break;

                case "isempty":
                    filter.Operator = "=";
                    filter.Value = "";
                    break;

                case "isnotempty":
                    filter.Operator = "<>";
                    filter.Value = "";
                    break;

                case "isanyof":
                    filter.Operator = "IN";
                    filter.Value = "(" + filter.Value + ")";
                    break;

                default:
                    throw new ArgumentException("Invalid operator type");
            }
            return sql.Append($" {filter.Operation} {filter.Field} {filter.Operator} @0 ", filter.Value);
        }

        public static List<string> ValidateTableColumns(this List<string> value, List<JittorColumnInfo> columns, bool isOrderBy = false)
        {
            return value.Where(item =>
            {
                var parts = item.Split('.');
                if (isOrderBy)
                    parts[1] = parts[1].ToLower().Replace("asc", "").Replace("desc", "");
                return parts.Length == 2 && columns.Any(x => x.TableName.ToLower() == parts[0].Trim().ToLower() && (x.ColumnName.ToLower() == parts[1].Trim().ToLower() || parts[1].Trim() == "*"));
            }).ToList();
        }
        public static List<PageFilterModel> ValidateTableColumns(this List<PageFilterModel> value, List<JittorColumnInfo> columns)
        {
            return value.Where(item =>
            {
                var parts = item.Field.Split('.');
                return parts.Length == 2 && columns.Any(x => x.TableName.ToLower() == parts[0].Trim().ToLower() && (x.ColumnName.ToLower() == parts[1].Trim().ToLower() || parts[1].Trim() == "*"));
            }).ToList();
        }
    }
}
