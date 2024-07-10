using Jittor.Shared.Enums;
using System.Data.SqlClient;
using Jittor.App.DataServices;
using static Jittor.App.DataServices.FrameworkRepository;
using Jittor.App;
using Jittor.App.Helpers;
using Jittor.App.Models;
using PetaPoco;
using Newtonsoft.Json;
using System.Drawing;
using System.Reflection;
using System.Collections.Generic;
using System;

namespace Jittor.App.Services
{
	public class JittorDataServices 
	{
        private readonly FrameworkRepository _tableContext;
        private Dictionary<string, List<JittorColumnInfo>> tableColumns = new Dictionary<string, List<JittorColumnInfo>>();

        public JittorDataServices(FrameworkRepository tableContext) {
            _tableContext = tableContext;   
        }
        public async Task<JittorPageModel?> GetPageModel(string urlFriendlyPageName)
        {
            List<JITAttributeType> types = await GetAttributeTypes();
            return await Executor.Instance.GetDataAsync<JittorPageModel?>(() => {
                using var context = DataContexts.GetJittorDataContext();
                JittorPageModel? model = new JittorPageModel();
                 var sql = PetaPoco.Sql.Builder
                     .Select(" * ")
                     .From(" JITPages ")
                     .Where(" UrlFriendlyName = @0", urlFriendlyPageName);
                model = context.Fetch<JittorPageModel>(sql).FirstOrDefault();
                if (model != null)
                {
                    model.PageAttributes = context.Fetch<JITPageAttribute>("Select * From JITPageAttributes Where PageID = @0", model.PageID).ToList();
                    model.PageTables = context.Fetch<JITPageTable>("Select * From JITPageTables Where PageID = @0", model.PageID).ToList();
                    var gids = model.PageAttributes.Select(x => x.DisplayGroupID).Distinct().ToArray();
                    model.AttributeDisplayGroups = context.Fetch<AttributeDisplayGroup>("Select * From JITAttributeDisplayGroups D Inner Join JITDisplayGroupTypes T On D.DisplayGroupTypeID = T.DisplayGroupTypeID Where D.DisplayGroupID In(@0)", gids).ToList();
                    Dictionary<string, object?> selectedRecord = new Dictionary<string, object?>();

                    foreach (var att in model.PageAttributes.Where(x => x.Editable))
                    {
                        var at = types.Where(x => x.AttributeTypeID == att.AttributeTypeID).First();
                        if(!selectedRecord.ContainsKey(att.AttributeName))
                        {
                            selectedRecord.Add(att.AttributeName, at.GetDefaultValue());
                        }
                    }
                    model.SelectedRecord = selectedRecord;
                }
                return model;

            }, new { urlFriendlyPageName }, 5);
        }
        public async Task<List<JITAttributeType>> GetAttributeTypes()
        {
            return await Executor.Instance.GetDataAsync<List<JITAttributeType>>(() =>
            {
                using var context = DataContexts.GetJittorDataContext();
                var sql = PetaPoco.Sql.Builder
                    .Select("*")
                    .From("JITAttributeTypes");
                return context.Fetch<JITAttributeType>(sql).ToList();
            }, new {  }, 300);
        }
        public JittorPageModel GetPageId(int PageId)
        {
            JittorPageModel? model = new JittorPageModel();
            using var context = DataContexts.GetJittorDataContext();
            var sql = PetaPoco.Sql.Builder
                     .Select("*")
                     .From("JITPages")
                     .Where("PageId = @0", PageId);
           model = context.Fetch<JittorPageModel>(sql).FirstOrDefault();
            return model;
        }
       
        public async Task<JittorPageModel?> GetPageModel(string urlFriendlyPageName, bool loadData, int pageNo = 0, int? pageSize = null)
        {
            return await Executor.Instance.GetDataAsync<JittorPageModel?>(() => {
                using var tableContext = _tableContext;
                JittorPageModel? model = GetPageModel(urlFriendlyPageName).Result;
               
                if (loadData && model != null)
                {
                    model.PageTablesData.Clear();
                    foreach (var table in model.PageTables.Where(x => x.ForView))
                    {
                        //var list = tableContext.Fetch<dynamic>($"Select top 10 * From {table.TableName} Order By ModifiedOn desc").ToList();
                        //model.PageTablesData.Add(table.TableName, list);
                    }
                    foreach (var att in model.PageAttributes.Where(x => x.IsForeignKey && !string.IsNullOrEmpty(x.ParentTableName)))
                    {
                        att.ForigenValues = tableContext.Fetch<ForigenValue>($"Select {att.AttributeName} As ID, {att.ParentTableNameColumn} As Name From {att.ParentTableName}  {(string.IsNullOrEmpty(att.ParentCondition) ? "" :  att.ParentCondition) } order by {att.AttributeName} desc ").ToList();
                    }
                    foreach (var att in model.PageAttributes.Where(x => !string.IsNullOrEmpty(x.AlternameValuesQuery)))
                    {
                        att.AlternateValues = tableContext.Fetch<string>(att.AlternameValuesQuery).FirstOrDefault();
                    }
                }
                return model;

            }, new { urlFriendlyPageName , loadData}, 5);
        }
        public List<dynamic> ExecuteCommand(int userId, string tableName, string operationSql, string selectSql, object[] param, string operation)
        {
            using var context = _tableContext;
            context.Execute(userId, tableName, operation, selectSql, operationSql, param);
            return context.Fetch<dynamic>(selectSql, param).ToList();
        }
        public async Task<List<JittorColumnInfo>> GetTableAndChildTableColumns(string tableName, string? schemaName = "dbo")
        {
            if (tableColumns.ContainsKey(tableName))
                return tableColumns[tableName];
            else
            {
                return await Executor.Instance.GetDataAsync<List<JittorColumnInfo>>(() =>
                {
                    using var tableContext = _tableContext;
                    var sql = @"
            SELECT 
                c.TABLE_NAME AS TableName,
                c.COLUMN_NAME AS ColumnName,
                c.DATA_TYPE AS DataType,
                CASE WHEN c.DATA_TYPE IN ('int', 'bigint', 'smallint', 'tinyint', 'float', 'real') THEN c.NUMERIC_PRECISION ELSE NULL END AS NumericPrecision,
                CASE WHEN c.DATA_TYPE IN ('int', 'bigint', 'smallint', 'tinyint', 'float', 'real') THEN c.NUMERIC_SCALE ELSE NULL END AS NumericScale,
                c.CHARACTER_MAXIMUM_LENGTH AS MaxLength,
                c.IS_NULLABLE AS IsNullable,
                CASE WHEN COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') = 1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsAutoIncrement,
                c.COLUMN_DEFAULT AS DefaultValue,
                CASE WHEN EXISTS (
                    SELECT 1
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu
                    ON tc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
                    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    AND ccu.TABLE_NAME = c.TABLE_NAME
                    AND ccu.COLUMN_NAME = c.COLUMN_NAME
                ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsPrimaryKey,
                CASE WHEN EXISTS (
                    SELECT 1
                    FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                    ON rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                    WHERE kcu.TABLE_NAME = c.TABLE_NAME
                    AND kcu.COLUMN_NAME = c.COLUMN_NAME
                ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsForeignKey,
                ep.value AS ColumnDescription
            FROM 
                INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN 
                sys.columns AS sc
            ON 
                sc.object_id = OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME)
                AND sc.name = c.COLUMN_NAME
            LEFT JOIN 
                sys.extended_properties AS ep
            ON 
                ep.major_id = sc.object_id 
                AND ep.minor_id = sc.column_id 
                AND ep.name = 'MS_Description'
            WHERE 
                c.TABLE_SCHEMA = @0
                AND c.TABLE_NAME = @1

            UNION ALL

            SELECT 
                fk.TABLE_NAME AS TableName,
                c.COLUMN_NAME AS ColumnName,
                c.DATA_TYPE AS DataType,
                CASE WHEN c.DATA_TYPE IN ('int', 'bigint', 'smallint', 'tinyint', 'float', 'real') THEN c.NUMERIC_PRECISION ELSE NULL END AS NumericPrecision,
                CASE WHEN c.DATA_TYPE IN ('int', 'bigint', 'smallint', 'tinyint', 'float', 'real') THEN c.NUMERIC_SCALE ELSE NULL END AS NumericScale,
                c.CHARACTER_MAXIMUM_LENGTH AS MaxLength,
                c.IS_NULLABLE AS IsNullable,
                CASE WHEN COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') = 1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsAutoIncrement,
                c.COLUMN_DEFAULT AS DefaultValue,
                CASE WHEN EXISTS (
                    SELECT 1
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu
                    ON tc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
                    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    AND ccu.TABLE_NAME = c.TABLE_NAME
                    AND ccu.COLUMN_NAME = c.COLUMN_NAME
                ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsPrimaryKey,
                CASE WHEN EXISTS (
                    SELECT 1
                    FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                    ON rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                    WHERE kcu.TABLE_NAME = c.TABLE_NAME
                    AND kcu.COLUMN_NAME = c.COLUMN_NAME
                ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsForeignKey,
                ep.value AS ColumnDescription
            FROM 
                INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
            JOIN 
                INFORMATION_SCHEMA.KEY_COLUMN_USAGE fk
            ON 
                rc.CONSTRAINT_NAME = fk.CONSTRAINT_NAME
            JOIN 
                INFORMATION_SCHEMA.COLUMNS c
            ON 
                c.TABLE_SCHEMA = fk.TABLE_SCHEMA
                AND c.TABLE_NAME = fk.TABLE_NAME
            LEFT JOIN 
                sys.columns AS sc
            ON 
                sc.object_id = OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME)
                AND sc.name = c.COLUMN_NAME
            LEFT JOIN 
                sys.extended_properties AS ep
            ON 
                ep.major_id = sc.object_id 
                AND ep.minor_id = sc.column_id 
                AND ep.name = 'MS_Description'
            WHERE 
                rc.UNIQUE_CONSTRAINT_SCHEMA = @0
                AND rc.UNIQUE_CONSTRAINT_NAME IN (
                    SELECT CONSTRAINT_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                    WHERE TABLE_SCHEMA = @0
                    AND TABLE_NAME = @1
                    AND CONSTRAINT_TYPE = 'PRIMARY KEY'
                )
            ORDER BY 
                TableName, ColumnName";
                    return tableContext.Fetch<JittorColumnInfo>(sql, schemaName, tableName).ToList();
                }, new { schemaName, tableName }, 5);
            }
        }
        public async Task<List<string>> GetAllTables()
        {
            return await Executor.Instance.GetDataAsync<List<string>>(() =>
            {
                using var tableContext = _tableContext;
                var sql = @"
            SELECT 
            DISTINCT c.TABLE_NAME AS TableName
            FROM 
            INFORMATION_SCHEMA.COLUMNS c";
                return tableContext.Fetch<string>(sql).ToList();
            }, 5);
        }
        public async Task<List<JITPage>> GetAllPages()
        {
            return await Executor.Instance.GetDataAsync<List<JITPage>>(() =>
            {
                using var context = DataContexts.GetJittorDataContext();
                var sql = @"
            Select * from JITPages";
                return context.Fetch<JITPage>(sql).ToList();
            }, 5);
        }
        public async Task<List<FormBuilderListerModel>> GetFormBuilderLister(int pageId)
        {
            return await Executor.Instance.GetDataAsync<List<FormBuilderListerModel>>(() =>
            {
                using var context = DataContexts.GetJittorDataContext();
                var sql = PetaPoco.Sql.Builder.Append(@"SELECT p.PageName,p.UrlFriendlyName,p.Title,
                STUFF((SELECT ', ' + t1.TableName
               FROM JITPageTables t1
               WHERE t1.PageID = p.PageID and t1.ForOperation = 1
               FOR XML PATH (''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS ForOperation,
                STUFF((SELECT ', ' + t1.TableName
               FROM JITPageTables t1
               WHERE t1.PageID = p.PageID  and t1.ForView = 1
               FOR XML PATH (''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS ForView
                FROM 
                    JITPages p
                WHERE 
                    p.PageID = @0", pageId);
                return context.Fetch<FormBuilderListerModel>(sql).ToList();
            }, 5);
        }
        public bool DeleteRecordByIdandPageName(int userId, string pagename, string columnname ,object ChartId)
        {
            using (var context = _tableContext)
            {
                string chartidchange = ChartId.ToString();
                int chartidint = Convert.ToInt32(chartidchange);
                //context.Execute(string.Format("Delete From {0} Where {1} = {2}", pagename, columnname, chartidint));

                try
                {
                    // Perform the deletion
                    context.Execute(userId, pagename, ActionTypeEnum.Delete.ToString(), string.Format("Select * From {0} Where {1} = {2}", pagename, columnname, chartidint), string.Format("Delete From {0} Where {1} = {2}", pagename, columnname, chartidint));
                    return true;
                }
                catch (SqlException ex)
                {
                    // Handle the exception (e.g., log the error or display a user-friendly message)
                    Console.WriteLine($"Error deleting record: {ex.Message}");
                    // Return false to indicate that the record cannot be deleted due to an error
                    return false;
                }
            }

        }
        public bool getRecordsFromChartbySectionId(string columnName, object chartId)
        {
            using (var context = _tableContext)
            {
                string chartIdString = chartId.ToString();
                int chartIdInt;

                if (!int.TryParse(chartIdString, out chartIdInt))
                {
                    // Failed to parse chartId as an integer, return false
                    return false;
                }

                try
                {
                    // Use PetaPoco's Query method to execute the SQL query and get the result.
                    string sqlCommandText = $"SELECT TOP 1 * FROM Charts WHERE {columnName} = @0";
                    var result = context.Query<dynamic>(sqlCommandText, chartIdInt);

                    // Check if the result contains any rows (records)
                    return result.Any();
                }
                catch (SqlException ex)
                {
                    // Handle the exception (e.g., log the error or display a user-friendly message)
                    Console.WriteLine($"Error executing SQL query: {ex.Message}");
                    // Return false to indicate that the record cannot be fetched due to an error
                    return false;
                }
            }
        }
        public async Task<bool> CreateNewPage(FormPageModel form)
        {
            try
            {
                var attributeTypes = await GetAttributeTypes();
                var tableAndChildTableColumns = await GetTableAndChildTableColumns(form.Form.TableName);

                using var context = DataContexts.GetJittorDataContext();

                var page = JittorMapperHelper.Map<JITPage, FormPageModel>(form);
                var pageId = context.Insert(page);

                var tableNames = form.Sections.SelectMany(x => x.Fields).Select(x => x.TableName).Distinct().ToList();
                List<JITPageTable> tables = new List<JITPageTable>();
                var mainTable = form.Form.TableName;

                foreach (var item in tableNames)
                {
                    var newTable = form.Form;
                    newTable.TableName = item;
                    newTable.ListerTableName = mainTable;
                    var table = JittorMapperHelper.Map<JITPageTable, Form>(newTable);
                    table.PageID = Convert.ToInt32(pageId);
                    context.Insert(table);
                    tables.Add(table);
                }


                foreach (var section in form.Sections)
                {
                    foreach (var field in section.Fields)
                    {
                        var currentColumn = tableAndChildTableColumns.FirstOrDefault(x => x.ColumnName == field.Id && x.TableName == field.TableName);
                        if (currentColumn != null)
                        {
                            var attributeType = attributeTypes.FirstOrDefault(x => x.TypeName == currentColumn.DataType);
                            field.AttributeTypeId = attributeType?.AttributeTypeID ?? 0;
                            field.TableId = tables.FirstOrDefault(x => x.TableName == currentColumn.TableName)?.TableID ?? 0;
                            field.PageId = page.PageID;
                            field.CurrentColumn = currentColumn;

                            var attribute = JittorMapperHelper.Map<JITPageAttribute, FieldModel>(field);
                            context.Insert(attribute);
                        }
                    }
                }
               // context.Insert(attributes);
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
        public async Task<DataListerResponse<dynamic>> GetPageLister(DataListerRequest request)
        {
            try
            {
                using var tableContext = _tableContext;
                using var context = DataContexts.GetJittorDataContext();
                var table = context.Fetch<JITPageTable>("SELECT * FROM JITPageTables WHERE PageID = @0 AND ForView = 1", request.PageId).FirstOrDefault();
                if (table == null)
                {
                    return new DataListerResponse<dynamic>();
                }

                var selectClause = string.IsNullOrEmpty(table.SelectColumns) ? (table.TableName + ".*") : table.SelectColumns;
                var selectColumnList = selectClause.Split(',').ToList();

                var joins = table.Joins?.Split(',').Where(x => !string.IsNullOrEmpty(x)).ToList() ?? new List<string>();

                request.Filters = request.Filters ?? new List<PageFilterModel>();
                request.Filters.Concat(JsonConvert.DeserializeObject<List<PageFilterModel>>(table.Filters) ?? new List<PageFilterModel>());

                string orderString = "";
                if (table.Orders != null || request.Sort != null)
                    orderString = request.Sort ?? (table.Orders ?? "");

                var tableColumns = await GetTableAndChildTableColumns(table.TableName);
                selectColumnList = selectColumnList.ValidateTableColumns(tableColumns);
                request.Filters = request.Filters.ValidateTableColumns(tableColumns);
                var orders = orderString.Split(",").ToList().ValidateTableColumns(tableColumns, true);

                var sql = Sql.Builder.Append($"SELECT {string.Join(',', selectColumnList)} FROM {table.TableName}");
                var count = tableContext.ExecuteScalar<long>($"SELECT COUNT(*) FROM {table.TableName}");

                foreach (var join in joins)
                {
                    sql.Append(" " + join);
                }

                if (request.Filters.Count > 0)
                {
                    sql.Append(" WHERE ");
                    request.Filters.ForEach(filter => sql = sql.BuildWhereClause(filter));
                }

                if (orders.Count() > 0)
                    sql.OrderBy(string.Join(',', orders));

                int pageSize = (table.Page > 0 ? table.Page.Value : request.PageSize);
                int offset = (request.PageNumber - 1) * pageSize;
                sql.Append($"OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY");

                var list = tableContext.Fetch<dynamic>(sql).ToList();
                List<string> columns = new List<string>();
                if (list.Count > 0)
                    columns = ((IDictionary<string, object>)list[0]).Keys.ToList();

                return new DataListerResponse<dynamic>()
                {
                    Items = list,
                    PageNumber = request.PageNumber,
                    PageSize = pageSize,
                    TotalItemCount = count,
                    Columns = columns.Select(x => new
                    {
                        Field = selectColumnList.FirstOrDefault(y => y.Contains(x)),
                        HeaderName = x
                    }).ToList()
                };
            }
            catch(Exception ex) 
            {
                return null;
            }
        }
    }
}
