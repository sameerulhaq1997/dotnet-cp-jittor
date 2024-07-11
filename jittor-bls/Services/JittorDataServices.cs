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
using Newtonsoft.Json.Linq;
using System.Transactions;

namespace Jittor.App.Services
{
    public class JittorDataServices
    {
        private readonly FrameworkRepository _tableContext;
        private Dictionary<string, List<JittorColumnInfo>> tableColumns = new Dictionary<string, List<JittorColumnInfo>>();
        private List<TableNode> tableNodes = new List<TableNode>();
        private readonly string _projectId;
        public JittorDataServices(FrameworkRepository tableContext, string projectId)
        {
            _tableContext = tableContext;
            _projectId = projectId;
            GetAllTableStructures();
        }
        public async Task<JittorPageModel?> GetPageModel(string urlFriendlyPageName)
        {
            List<JITAttributeType> types = await GetAttributeTypes();
            return await Executor.Instance.GetDataAsync<JittorPageModel?>(() =>
            {
                using var context = DataContexts.GetJittorDataContext();
                JittorPageModel? model = new JittorPageModel();
                var sql = PetaPoco.Sql.Builder
                    .Select(" * ")
                    .From(" JITPages ")
                    .Where($" UrlFriendlyName = @0 AND ProjectId = '{_projectId}'", urlFriendlyPageName);
                model = context.Fetch<JittorPageModel>(sql).FirstOrDefault();
                if (model != null)
                {
                    model.PageAttributes = context.Fetch<JITPageAttribute>($"Select * From JITPageAttributes Where PageID = @0 AND ProjectId = '{_projectId}'", model.PageID).ToList();
                    model.PageTables = context.Fetch<JITPageTable>($"Select * From JITPageTables Where PageID = @0 AND ProjectId = '{_projectId}'", model.PageID).ToList();
                    var gids = model.PageAttributes.Select(x => x.DisplayGroupID).Distinct().ToArray();
                    model.AttributeDisplayGroups = context.Fetch<AttributeDisplayGroup>("Select * From JITAttributeDisplayGroups D Inner Join JITDisplayGroupTypes T On D.DisplayGroupTypeID = T.DisplayGroupTypeID Where D.DisplayGroupID In(@0)", gids).ToList();
                    Dictionary<string, object?> selectedRecord = new Dictionary<string, object?>();

                    foreach (var att in model.PageAttributes.Where(x => x.Editable))
                    {
                        var at = types.Where(x => x.AttributeTypeID == att.AttributeTypeID).First();
                        if (!selectedRecord.ContainsKey(att.AttributeName))
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
            }, new { }, 300);
        }
        public JittorPageModel GetPageId(int PageId)
        {
            JittorPageModel? model = new JittorPageModel();
            using var context = DataContexts.GetJittorDataContext();
            var sql = PetaPoco.Sql.Builder
                     .Select("*")
                     .From("JITPages")
                     .Where($"PageId = @0 AND ProjectId = '{_projectId}'", PageId);
            model = context.Fetch<JittorPageModel>(sql).FirstOrDefault();
            return model;
        }

        public async Task<JittorPageModel?> GetPageModel(string urlFriendlyPageName, bool loadData, int pageNo = 0, int? pageSize = null)
        {
            return await Executor.Instance.GetDataAsync<JittorPageModel?>(() =>
            {
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
                        att.ForigenValues = tableContext.Fetch<ForigenValue>($"Select {att.AttributeName} As ID, {att.ParentTableNameColumn} As Name From {att.ParentTableName}  {(string.IsNullOrEmpty(att.ParentCondition) ? "" : att.ParentCondition)} order by {att.AttributeName} desc ").ToList();
                    }
                    foreach (var att in model.PageAttributes.Where(x => !string.IsNullOrEmpty(x.AlternameValuesQuery)))
                    {
                        att.AlternateValues = tableContext.Fetch<string>(att.AlternameValuesQuery).FirstOrDefault();
                    }
                }
                return model;

            }, new { urlFriendlyPageName, loadData }, 5);
        }
        public List<dynamic> ExecuteCommand(int userId, string tableName, string operationSql, string selectSql, object[] param, string operation)
        {
            using var context = _tableContext;
            context.Execute(userId, tableName, operation, selectSql, operationSql, param);
            return context.Fetch<dynamic>(selectSql, param).ToList();
        }
        public List<JittorColumnInfo> GetParentTableColumns(List<string> tableName, string? schemaName = "dbo")
        {

            var tablesToGet = tableName.Where(x => !tableColumns.Any(y => y.Key == x));

            List<JittorColumnInfo> tableColumnList = new List<JittorColumnInfo>();
            if (tablesToGet.Count() > 0)
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
                AND c.TABLE_NAME in (@1)
            ORDER BY 
                TableName, ColumnName";
                var newRenderedTables = tableContext.Fetch<JittorColumnInfo>(sql, schemaName ?? "", tablesToGet).ToList();

                foreach (var item in newRenderedTables.GroupBy(x => x.TableName))
                {
                    if (!tableColumns.ContainsKey(item.Key))
                    {
                        tableColumns.Add(item.Key.ToLower(), item.ToList());
                    }
                }
            }

            tableName.ForEach(x =>
            {
                tableColumns.TryGetValue(x, out List<JittorColumnInfo>? value);
                if (value != null)
                {
                    tableColumnList.AddRange(value);
                }
            });
            return tableColumnList;
        }

        
        public List<JittorColumnInfo> GetTableAndChildTableColumns(string tableName, string? schemaName = "dbo")
        {
            var tablesToGet = GetAllRelatedTables(tableName);
            var parentData = GetParentTableColumns(tablesToGet, schemaName);
            return parentData;

        }
        public List<string> GetAllTables()
        {
            return tableNodes.Select(x => x.TableName).Distinct().ToList();
        }
        public async Task<List<JITPage>> GetAllPages()
        {
            return await Executor.Instance.GetDataAsync<List<JITPage>>(() =>
            {
                using var context = DataContexts.GetJittorDataContext();
                var sql = ($"Select * from JITPages Where ProjectId = '{_projectId}'");
                return context.Fetch<JITPage>(sql).ToList();
            }, 5);
        }
        public async Task<bool> DeleteForm(int pageID)
        {
            try
            {
                using var context = DataContexts.GetJittorDataContext();
                using (var tr = context.GetTransaction())
                {

                    var res = context.Execute($"Delete From JITPageAttributes Where PageID = @0", pageID);
                    res = context.Execute($"Delete From JITPageTables Where PageID = @0", pageID);
                    res = context.Execute($"Delete From JITPages Where PageID = @0", pageID);

                    tr.Complete();
                    return true;
                }
            } 
            catch (Exception ex)
            {
                return false;
            }
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
                    p.PageID = @0 AND ProjectId = '" + _projectId + "'", pageId);
            return context.Fetch<FormBuilderListerModel>(sql).ToList();
            }, 5);
        }
        public bool DeleteRecordByIdandPageName(int userId, string pagename, string columnname, object ChartId)
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
                var tableAndChildTableColumns = GetTableAndChildTableColumns(form.Form.TableName);

                using var context = DataContexts.GetJittorDataContext();
                using (var tr = context.GetTransaction())
                {
                    var page = JittorMapperHelper.Map<JITPage, FormPageModel>(form);
                    page.ProjectId = _projectId;
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
                        table.ProjectId = _projectId;
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
                                attribute.ProjectId = _projectId;
                                context.Insert(attribute);
                            }
                        }
                    }
                    tr.Complete();
                }
                return true;
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
        public async Task<DataListerResponse<dynamic>> GetPageLister(DataListerRequest request)
        {
            try
            {
                List<string> JoinTypes = new List<string>() { "inner join", "outer join", "cross join", "left join", "right join" };
                using var tableContext = _tableContext;
                using var context = DataContexts.GetJittorDataContext();
                var table = context.Fetch<JITPageTable>($"SELECT * FROM JITPageTables WHERE PageID = @0 AND ForView = 1 AND ProjectId = '{_projectId}'", request.PageId).FirstOrDefault();
                if (table == null)
                {
                    return new DataListerResponse<dynamic>();
                }

                var selectClause = string.IsNullOrEmpty(table.SelectColumns) ? (table.TableName + ".*") : table.SelectColumns;
                var selectColumnList = selectClause.Split(',').ToList();

                //var joins = table.Joins?.Split(',').Where(x => !string.IsNullOrEmpty(x)).ToList() ?? new List<string>();
                var joins = JsonConvert.DeserializeObject<List<PageJoinModel>>(table.Joins ?? "[]");

                request.Filters = request.Filters ?? new List<PageFilterModel>();
                request.Filters.Concat(JsonConvert.DeserializeObject<List<PageFilterModel>>(table.Filters) ?? new List<PageFilterModel>());

                string orderString = "";
                if (table.Orders != null || request.Sort != null)
                    orderString = request.Sort ?? (table.Orders ?? "");

                var tableColumns = GetTableAndChildTableColumns(table.TableName);
                selectColumnList = selectColumnList.ValidateTableColumns(tableColumns);
                request.Filters = request.Filters.ValidateTableColumns(tableColumns);
                var orders = orderString.Split(",").ToList().ValidateTableColumns(tableColumns, true);

                if (selectColumnList.Any(x => x.Contains("*")))
                {
                    selectColumnList.AddRange(tableColumns.Where(y => selectColumnList.Where(x => x.Contains("*")).Select(x => x.Replace(".*", "").Trim()).Contains(y.TableName)).Select(x => x.TableName + "." + x.ColumnName).ToList());
                    selectColumnList.RemoveAll(x => x.Contains("*"));
                    selectColumnList = selectColumnList.GroupBy(x => x.Split(".")[1]).Select(x => x.FirstOrDefault() ?? "").ToList();
                }


                var sql = Sql.Builder.Append($"SELECT {string.Join(',', selectColumnList)} FROM {table.TableName}");
                var count = tableContext.ExecuteScalar<long>($"SELECT COUNT(*) FROM {table.TableName}");
                if (joins != null)
                {
                    foreach (var join in joins.ValidateTableColumns(tableColumns))
                    {
                        bool tableExists = tableColumns.Select(x => x.TableName).Contains(join.JoinTable);

                        if (JoinTypes.Contains(join.JoinType.ToLower()) && tableExists)
                        {
                            sql.Append($"{join.JoinType} {join.JoinTable} on {join.ParentTableColumn} = {join.JoinTableColumn}");
                        }
                    }
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
                    Columns = selectColumnList.Select(x =>
                    {
                        var splittedKey = x.Split(".");
                        return new
                        {
                            Field = splittedKey[1],
                            HeaderName = splittedKey[1],
                            TableName = splittedKey[0],
                        };
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public List<string> GetAllRelatedTables(string mainTable)
        {
            mainTable = mainTable.ToLower();
            List<string> allRelatedTableNames = new List<string>() { mainTable };
            var childTables = GetChildTableNames(mainTable, tableNodes);
            allRelatedTableNames.AddRange(childTables);
            var parentTables = tableNodes.Where(x => x.ChildTables.Any(x => x.TableName.ToLower() == mainTable)).Select(x => x.TableName).ToList();
            foreach (var item in parentTables)
            {
                allRelatedTableNames.Add(item.ToLower());
                var itemChildTables = GetChildTableNames(mainTable, tableNodes, allRelatedTableNames);
                allRelatedTableNames.AddRange(itemChildTables);
            }
            return allRelatedTableNames;
        }
        public void GetAllTableStructures()
        {
            var relationships = _tableContext.Fetch<TableRelationship>(@"
    SELECT 
        TP.name AS ParentTable,
        TR.name AS ChildTable
    FROM 
        sys.foreign_keys AS FK
    INNER JOIN 
        sys.tables AS TP ON FK.referenced_object_id = TP.object_id
    INNER JOIN 
        sys.tables AS TR ON FK.parent_object_id = TR.object_id
    ORDER BY 
        TP.name, TR.name;
    ");

            // Debug output to verify relationships fetched
            Console.WriteLine("Relationships:");
            foreach (var rel in relationships)
            {
                Console.WriteLine($"Parent: {rel.ParentTable}, Child: {rel.ChildTable}");
            }

            // Create a dictionary for all tables with empty child lists
            var tableDict = relationships
                .SelectMany(r => new[] { r.ParentTable, r.ChildTable })
                .Distinct()
                .ToDictionary(t => t, t => new TableNode { TableName = t });

            // Populate child nodes recursively
            var visited = new HashSet<string>();
            foreach (var table in tableDict.Keys)
            {
                if (!visited.Contains(table))
                {
                    BuildTableTree(table, relationships, tableDict, visited);
                }
            }

            // Collect all nodes into tableNodes list
            tableNodes = tableDict.Values.ToList();

            // Debug output to verify table nodes
            Console.WriteLine("Table Nodes:");
            foreach (var node in tableNodes)
            {
                Console.WriteLine($"Table: {node.TableName}, Child Count: {node.ChildTables.Count}");
            }
        }

        private static void BuildTableTree(string parentTable, List<TableRelationship> relationships, Dictionary<string, TableNode> tableDict, HashSet<string> visited)
        {
            if (visited.Contains(parentTable))
                return;

            visited.Add(parentTable);

            var childRelationships = relationships
                .Where(r => r.ParentTable == parentTable)
                .ToList();

            var parentNode = tableDict[parentTable];
            foreach (var rel in childRelationships)
            {
                var childNode = tableDict[rel.ChildTable];
                parentNode.ChildTables.Add(childNode);
                BuildTableTree(rel.ChildTable, relationships, tableDict, visited);
            }
        }

        public static List<string> GetChildTableNames(string nodeName, List<TableNode> rootNodes, List<string>? excludeTables = null)
        {
            var childTableNames = new List<string>();
            var node = rootNodes.FirstOrDefault(n => n.TableName.ToLower() == nodeName);
            if (node != null)
            {
                foreach (var node2 in node.ChildTables.Where(x => excludeTables != null ? !excludeTables.Contains(x.TableName.ToLower()) : true))
                {
                    childTableNames.Add(node2.TableName.ToLower());
                    GetChildTableNames(node2.TableName, node2.ChildTables)
                        .ForEach(t => childTableNames.Add(t));
                }
            }
            return childTableNames;
        }
    }
    public class TableRelationship
    {
        public string ParentTable { get; set; }
        public string ChildTable { get; set; }
    }

    public class TableNode
    {
        public string TableName { get; set; }
        public List<TableNode> ChildTables { get; set; } = new List<TableNode>();
    }
}
