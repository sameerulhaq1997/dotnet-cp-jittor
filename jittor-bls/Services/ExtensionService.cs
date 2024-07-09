﻿

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
                    filter.Value = "'%" + filter.Value + "%'";
                    break;

                case "equals":
                    filter.Operator = "=";
                    break;

                case "starts with":
                    filter.Operator = "LIKE";
                    filter.Value = filter.Value + "%";
                    break;

                case "ends with":
                    filter.Operator = "LIKE";
                    filter.Value = "%" + filter.Value;
                    break;

                case "is empty":
                    filter.Operator = "=";
                    filter.Value = "";
                    break;

                case "is not empty":
                    filter.Operator = "<>";
                    filter.Value = "";
                    break;

                case "is any of":
                    filter.Operator = "IN";
                    filter.Value = "(" + filter.Value + ")";
                    break;

                default:
                    throw new ArgumentException("Invalid operator type");
            }

            return sql.Where("{@0} = {@1}", filter.Field, filter.Value);

            //return sql.Where(" {@0} {@1} {@2} {@3}", filter.Operation, filter.Field,filter.Operator, filter.Value);

        }


    }
}
