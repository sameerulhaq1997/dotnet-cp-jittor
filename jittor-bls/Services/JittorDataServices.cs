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
using System.Collections;
using System.Text.RegularExpressions;

namespace Jittor.App.Services
{
    public class JittorDataServices
    {
        private readonly FrameworkRepository _tableContext;
        private readonly FrameworkRepository? _secondaryTableContext;
        private Dictionary<string, List<JittorColumnInfo>> tableColumns = new Dictionary<string, List<JittorColumnInfo>>();
        private List<TableNode> tableNodes = new List<TableNode>();
        private readonly string _projectId;
        private static readonly Regex ValidAliasRegex = new Regex(@"^[\w]+$", RegexOptions.Compiled);  // Allow alphanumeric and underscores

        public JittorDataServices(FrameworkRepository tableContext, string projectId, FrameworkRepository? secondaryTableContext = null)
        {
            _tableContext = tableContext;
            _projectId = projectId;
            _secondaryTableContext = secondaryTableContext;
            GetAllTableStructures();
        }
        public List<TableNode> getTableNodes()
        {
            return tableNodes;
        }
        public async Task<JittorPageModel?> GetPageModel(string? urlFriendlyPageName = null, int? pageID = null)
        {
            List<JITAttributeType> types = await GetAttributeTypes();
            return await Executor.Instance.GetDataAsync<JittorPageModel?>(() =>
            {
            using var context = DataContexts.GetJittorDataContext();
            JittorPageModel? model = new JittorPageModel();
            var sql = PetaPoco.Sql.Builder
                .Select(" * ")
                .From(" JITPages ");
            if (urlFriendlyPageName != null)
            {
                sql.Where($" UrlFriendlyName = @0 AND ProjectId = '{_projectId}'", urlFriendlyPageName);
            }
            if (pageID != null)
            {
                sql.Where($" PageID = @0 AND ProjectId = '{_projectId}'", pageID);
            }
            model = context.Fetch<JittorPageModel>(sql).FirstOrDefault();
            if (model != null)
            {
                model.PageAttributes = context.Fetch<JITPageAttribute>($"Select * From JITPageAttributes Where PageID = @0 AND ProjectId = '{_projectId}'", model.PageID).ToList();
                model.PageTables = context.Fetch<JITPageTable>($"Select * From JITPageTables Where PageID = @0 AND ProjectId = '{_projectId}'", model.PageID).ToList();
                model.PageSections = context.Fetch<JITPageSection>($"Select * From JITPageSections Where PageID = @0 AND ProjectId = '{_projectId}'", model.PageID).ToList();
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
                        var list = tableContext.Fetch<dynamic>($"Select top 10 * From {table.TableName} Order By ModifiedOn desc").ToList();
                        model.PageTablesData.Add(table.TableName, list);
                    }
                    foreach (var att in model.PageAttributes.Where(x => x.IsForeignKey && !string.IsNullOrEmpty(x.ParentTableName) && x.Displayable))
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
        public List<JittorColumnInfo> GetTableSchema(List<string> tables, string? schemaName = "dbo", FrameworkRepository? context = null)
        {
            tables = tables.Select(x => x.ToLower().Replace("pub.", "")).ToList();
            var tablesToGet = tables.Where(x => !tableColumns.Any(y => y.Key == x));

            List<JittorColumnInfo> tableColumnList = new List<JittorColumnInfo>();
            if (tablesToGet.Count() > 0)
            {
                using var tableContext = context == null ? _tableContext : context;
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
                rf.ReferencedTableName,
                rf.ReferencedTableNameColumnName,
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
            LEFT JOIN (
                SELECT 
                    fkc.parent_object_id,
                    fkc.parent_column_id,
                    OBJECT_NAME(fkc.referenced_object_id) AS ReferencedTableName,
                    (SELECT TOP 1 col.name 
                     FROM sys.columns col 
                     WHERE col.object_id = fkc.referenced_object_id 
				             AND col.system_type_id IN (167, 175, 231, 35)
                     ORDER BY col.column_id) AS ReferencedTableNameColumnName
                FROM 
                    sys.foreign_key_columns AS fkc
            ) AS rf
            ON 
                rf.parent_object_id = OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME)
                AND rf.parent_column_id = sc.column_id
            WHERE c.TABLE_NAME in (@0)
            ORDER BY 
                TableName, ColumnName";
                var newRenderedTables = tableContext.Fetch<JittorColumnInfo>(sql, tablesToGet).ToList();

                foreach (var item in newRenderedTables.GroupBy(x => x.TableName))
                {
                    if (!tableColumns.ContainsKey(item.Key))
                    {
                        tableColumns.Add(item.Key.ToLower(), item.ToList());
                    }
                }
            }

            tables.ForEach(x =>
            {
                tableColumns.TryGetValue(x, out List<JittorColumnInfo>? value);
                if (value != null)
                {
                    tableColumnList.AddRange(value);
                }
            });
            return tableColumnList;
        }


        public List<JittorColumnInfo> GetTableAndChildTableColumns(string tableName, string? schemaName = "dbo", FrameworkRepository? context = null)
        {
            var tablesToGet = tableName.Contains(",") ? tableName.ToLower().Split(",").ToList() : GetAllRelatedTables(tableName);
            var parentData = GetTableSchema(tablesToGet, schemaName, context); 
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
                    res = context.Execute($"Delete From JITPageSections Where PageID = @0", pageID);
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
        public async Task<List<FormBuilderListerModel>> GetFormBuilderLister(int pageId = 0)
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
                WHERE ProjectId = @0", _projectId);
                if (pageId != 0)
                {
                    sql = sql.Append(@" AND p.PageID = @0", pageId);
                }
                return context.Fetch<FormBuilderListerModel>(sql).ToList();
            }, 5);
        }
        public async Task<FormPageModel> GetFormBuilderPageData(int pageId)
        {
            //return await Executor.Instance.GetDataAsync<FormPageModel>(() =>
            //{
            using var context = DataContexts.GetJittorDataContext();
            JittorPageModel? model = GetPageModel(null, pageId).Result;
            FormPageModel formPage = new FormPageModel();
            formPage.Form = JittorMapperHelper.Map<Form, JITPageTable>(model.PageTables.FirstOrDefault(x => x.ForView == true)); //model.PageTables;
            formPage.Form.FormName = model.PageName;
            formPage.Form.CurrentPage = model.CurrentPage;
            formPage.Form.RecordsPerPage = model.RecordsPerPage;
            formPage.Form.Extender = model.Extender;
            formPage.Form.SoftDeleteColumn = model.SoftDeleteColumn;
            formPage.Form.Description = model.Description;
            formPage.Form.ListingTitle = model.ListingTitle;
            foreach (var jitSection in model.PageSections)
            {
                var formSection = new FormSection();
                formSection.Fields = new List<FieldModel>();
                formSection = JittorMapperHelper.Map<FormSection, JITPageSection>(jitSection);
                foreach (var jitAttr in model.PageAttributes.Where(x => x.SectionId == jitSection.PageSectionId))
                {
                    var field = new FieldModel();
                    field = JittorMapperHelper.Map<FieldModel, JITPageAttribute>(jitAttr);
                    field.TableName = model.PageTables.FirstOrDefault(x => x.TableID == field.TableId).TableName;
                    formSection.Fields.Add(field);
                }
                formPage.Sections.Add(formSection);
            }
            formPage.ProjectId = formPage.Form.ProjectId;

            var allFields = formPage.Sections.SelectMany(section => section.Fields).ToList();
            
            foreach (var tbl in model.PageTables.Distinct())
            {
                var formTable = new FormTable();
                formTable.Label = tbl.TableName;
                formTable.Value = tbl.TableName; //allFields.Where(x => x.TableId == tbl.TableID).ToList();



                formPage.Form.FormTables.Add(formTable);
            }
            formPage.Form.ShowListing = (formPage.Form.Joins != null || formPage.Form.Filters != null || formPage.Form.SelectColumns != null || 
                formPage.Form.Orders != null || formPage.Form.CurrentPage != null || formPage.Form.RecordsPerPage != null || formPage.Form.ShowSearch == true || 
                formPage.Form.ShowFilters == true || formPage.Form.ListingTitle != null) ? true : false;
            return formPage;
            //}, 5);
        }
        public async Task<List<dynamic>> GetFormBuilderAllData()
        {
            return await Executor.Instance.GetDataAsync<List<dynamic>>(() =>
            {
                using var context = DataContexts.GetJittorDataContext();
                var sql = PetaPoco.Sql.Builder.Append(@"SELECT p.PageID AS id,p.PageName,p.UrlFriendlyName,p.Title,
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
                WHERE ProjectId = @0", _projectId);

                return context.Fetch<dynamic>(sql).ToList();
            }, 5);
        }
        public bool DeleteRecordByIdandPageName(int userId, string pagename, string columnname, object ChartId, string? deleteQuery = null)
        {
            using (var context = _tableContext)
            {
                string chartidchange = ChartId.ToString();
                int chartidint = Convert.ToInt32(chartidchange);
                //context.Execute(string.Format("Delete From {0} Where {1} = {2}", pagename, columnname, chartidint));

                try
                {
                    // Perform the deletion
                    //if (isArticleRelated == true)// Update IsDeleted = true and ArticleStatusID = 7
                    //{
                    //    context.Execute(userId, pagename, ActionTypeEnum.Delete.ToString(), string.Format("Select * From {0} Where {1} = {2}", pagename, columnname, chartidint), string.Format("Update {0} Set IsDeleted = 1,ArticleStatusID = 7 Where {1} = {2}", pagename, columnname, chartidint));
                    //    return true;
                    //}
                    //else
                    //{
                        context.Execute(userId, pagename, ActionTypeEnum.Delete.ToString(), string.Format("Select * From {0} Where {1} = {2}", pagename, columnname, chartidint), string.IsNullOrEmpty(deleteQuery) ? string.Format("Delete From {0} Where {1} = {2}", pagename, columnname, chartidint) : deleteQuery);
                        return true;
                    //}
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
        public async Task<bool> CreateNewPage(FormPageModel form, int? pageID)
        {
            try
            {
                var attributeTypes = await GetAttributeTypes();
                var tableAndChildTableColumns = GetTableAndChildTableColumns(form.Form.TableName);

                using var context = DataContexts.GetJittorDataContext();
                using (var tr = context.GetTransaction())
                {
                    form.ProjectId = _projectId;
                    var page = JittorMapperHelper.Map<JITPage, FormPageModel>(form);
                    page.ProjectId = _projectId;

                    if (pageID != null)
                    {
                        string sql = @"Select PageName from JITPages Where PageID = @0";//JITPage Update
                        var pageResult = context.Query<dynamic>(sql, pageID);
                        if (pageResult.Any())
                        {
                            page.PageID = (int)pageID;
                            context.Update(page);
                        }
                        var res = context.Execute($"Delete From JITPageAttributes Where PageID = @0", pageID);
                        res = context.Execute($"Delete From JITPageTables Where PageID = @0", pageID);
                        res = context.Execute($"Delete From JITPageSections Where PageID = @0", pageID);

                    }
                    else
                    {
                        context.Insert(page);
                    }
                    var tableNames = form.Sections.SelectMany(x => x.Fields).Select(x => x.TableName).Distinct().ToList();
                    List<JITPageTable> tables = new List<JITPageTable>();
                    var mainTable = form.Form.TableName;

                    foreach (var item in tableNames) //Insert Page Tables
                    {
                        var newTable = form.Form;
                        newTable.TableName = item;
                        newTable.ListerTableName = mainTable;
                        newTable.PageID = Convert.ToInt32(page.PageID);
                        newTable.ProjectId = _projectId;
                        newTable.ForView = mainTable == item;

                        var table = JittorMapperHelper.Map<JITPageTable, Form>(newTable);
                        table.PageID = Convert.ToInt32(page.PageID);
                        table.ProjectId = _projectId;
                        context.Insert(table);
                        tables.Add(table);
                    }

                    foreach (var section in form.Sections) //Insert PageSections
                    {
                        section.ProjectId = _projectId;
                        section.PageID = Convert.ToInt32(page.PageID);
                        var sectionDb = JittorMapperHelper.Map<JITPageSection, FormSection>(section);
                        context.Insert(sectionDb);
                        foreach (var field in section.Fields) //Insert PageAttributes
                        {
                            var currentColumn = tableAndChildTableColumns.FirstOrDefault(x => x.ColumnName == field.Id && x.TableName == field.TableName);
                            if (currentColumn != null)
                            {
                                var attributeType = attributeTypes.FirstOrDefault(x => x.TypeName == currentColumn.DataType);
                                field.AttributeTypeId = attributeType?.AttributeTypeID ?? 0;
                                field.TableId = tables.FirstOrDefault(x => x.TableName == currentColumn.TableName)?.TableID ?? 0;
                                field.PageId = page.PageID;
                                field.CurrentColumn = currentColumn;
                                field.SectionId = sectionDb.PageSectionId;
                                field.ProjectId = _projectId;
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
        public DataListerResponse<dynamic>? GetPageLister(DataListerRequest request, string? externalTable = null, string? externalSelectedColumns = null, List<PageJoinModel>? externalJoins = null,string? externalScripts = null)
        {
            try
            {
                List<int> hideAddUpdateForPages = new List<int>() { 186};
                using var tableContext = _tableContext;
                using var context = DataContexts.GetJittorDataContext();

                var table = new JITPageTable();
                if (externalTable != null)
                    table.TableName = externalTable;
                else
                    table = context.Fetch<JITPageTable>($"SELECT * FROM JITPageTables WHERE PageID = @0 AND ForView = 1 AND ProjectId = '{_projectId}'", request.PageId ?? 0).FirstOrDefault();
                if (table == null)
                {
                    return new DataListerResponse<dynamic>();
                }

                var selectClause = string.IsNullOrEmpty(table.SelectColumns) ? (string.IsNullOrEmpty(externalSelectedColumns) ? (table.TableName + ".*") : externalSelectedColumns) : table.SelectColumns;
                var joins = externalJoins != null ? externalJoins : (JsonConvert.DeserializeObject<List<PageJoinModel>>(table.Joins ?? "[]")?.Select(x =>
                {
                    x.FixedJoin = true;
                    return x;
                }).ToList() ?? new List<PageJoinModel>());
                request.Filters = request.Filters ?? new List<PageFilterModel>();
                if (!string.IsNullOrEmpty(table.Filters))
                    request.Filters = request.Filters.Concat((JsonConvert.DeserializeObject<List<PageFilterModel>>(table.Filters ?? "[]"))?.Select(x =>
                    {
                        x.FixedFilter = true;
                        return x;
                    }) ?? new List<PageFilterModel>()).ToList();
                if (table.Orders != null || request.Sort != null)
                    request.Sort = request.Sort ?? (table.Orders ?? "");
                request.PageSize = (table.Page > 0 ? table.Page.Value : request.PageSize ?? 0);

                var newRequest = new DropdownListerRequest()
                {
                    TableName = table.TableName,
                    Joins = joins,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    Sort = request.Sort,
                    Filters = request.Filters,
                    PageId = request.PageId,
                    idColumn = request.idColumn
                };
                var listerQuery = BuildListerQuery(newRequest, selectClause, joins,externalScripts);
                var count = tableContext.ExecuteScalar<int>(listerQuery.CountSql);
                var list = tableContext.Fetch<dynamic>(listerQuery.Sql).ToList();
                var columnsList = listerQuery.SelectColumnList.Select(x =>
                {
                    var splittedKey = x.Split(".");
                    return new TableColumns()
                    {
                        Field = listerQuery.ColumnDictionary.GetValueOrDefault<string, string?>(x) ?? splittedKey[1],
                        HeaderName = listerQuery.ColumnDictionary.GetValueOrDefault<string, string?>(x) ?? splittedKey[1],
                        TableName = splittedKey[0],
                        Hideable = splittedKey[1] == "id" ? false : true,
                    };
                }).ToList();
                
                listerQuery.SelectColumnList.Add(table.TableName + ".id");
                return new DataListerResponse<dynamic>()
                {
                    Items = list,
                    PageNumber = request.PageNumber ?? 0,
                    PageSize = request.PageSize ?? 0,
                    TotalItemCount = count,
                    IsSelectable = table.IsSelectable,
                    HideAddUpdate = hideAddUpdateForPages.Contains(request.PageId ?? 0),
                    Columns = columnsList,
                    //PageName = table.UrlFriendlyName
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
        public DataListerResponse<dynamic>? GetPageRecord(DataListerRequest request, string? externalTable = null, string? externalSelectedColumns = null, List<PageJoinModel>? externalJoins = null, string? externalScripts = null)
        {
            try
            {
                List<int> hideAddUpdateForPages = new List<int>() { 186 };
                using var tableContext = _tableContext;
                using var context = DataContexts.GetJittorDataContext();

                var table = new JITPageTable();
                if (externalTable != null)
                    table.TableName = externalTable;
                else
                    table = context.Fetch<JITPageTable>($"SELECT * FROM JITPageTables WHERE PageID = @0 AND ForView = 1 AND ProjectId = '{_projectId}'", request.PageId ?? 0).FirstOrDefault();
                if (table == null)
                {
                    return new DataListerResponse<dynamic>();
                }

                var tableColumns = context.Fetch<string>($"SELECT STRING_AGG(CONCAT('{table.TableName}.', AttributeName), ', ') FROM JITPageAttributes WHERE PageID = @0 AND TableID = {table.TableID} AND ProjectId = '{_projectId}'", request.PageId ?? 0).FirstOrDefault();
                var selectClause = string.IsNullOrEmpty(tableColumns) ? (string.IsNullOrEmpty(externalSelectedColumns) ? (table.TableName + ".*") : externalSelectedColumns) : tableColumns;
                var joins = externalJoins != null ? externalJoins : JsonConvert.DeserializeObject<List<PageJoinModel>>(table.Joins ?? "[]");
                request.Filters = request.Filters ?? new List<PageFilterModel>();
                if (!string.IsNullOrEmpty(table.Filters))
                    request.Filters = request.Filters.Concat(JsonConvert.DeserializeObject<List<PageFilterModel>>(table.Filters ?? "[]") ?? new List<PageFilterModel>()).ToList();
                if (table.Orders != null || request.Sort != null)
                    request.Sort = request.Sort ?? (table.Orders ?? "");
                request.PageSize = (table.Page > 0 ? table.Page.Value : request.PageSize ?? 0);

                var count = tableContext.ExecuteScalar<long>($"SELECT COUNT(*) FROM {table.TableName}");


                var newRequest = new DropdownListerRequest()
                {
                    TableName = table.TableName,
                    Joins = joins,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    Sort = request.Sort,
                    Filters = request.Filters,
                    PageId = request.PageId,
                };
                var listerQuery = BuildListerQuery(newRequest, selectClause, joins, externalScripts);
                var list = tableContext.Fetch<dynamic>(listerQuery.Sql).ToList();

                listerQuery.SelectColumnList.Add(table.TableName + ".id");
                return new DataListerResponse<dynamic>()
                {
                    Items = list,
                    PageNumber = request.PageNumber ?? 0,
                    PageSize = request.PageSize ?? 0,
                    TotalItemCount = count,
                    IsSelectable = table.IsSelectable,
                    HideAddUpdate = hideAddUpdateForPages.Contains(request.PageId ?? 0),
                    Columns = listerQuery.SelectColumnList.Select(x =>
                    {
                        var splittedKey = x.Split(".");
                        return new TableColumns()
                        {
                            Field = listerQuery.ColumnDictionary.GetValueOrDefault<string, string?>(x) ?? splittedKey[1],
                            HeaderName = listerQuery.ColumnDictionary.GetValueOrDefault<string, string?>(x) ?? splittedKey[1],
                            TableName = splittedKey[0],
                            Hideable = splittedKey[1] == "id" ? false : true,
                        };
                    }).ToList(),
                    //PageName = table.UrlFriendlyName
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
        public DropdownListerResponse PoplulateDropDowns(DropdownListerRequest request)
        {
            try
            {
                using var tableContext = request.IsArgaamContext == true ? _secondaryTableContext ?? _tableContext : _tableContext;
                using var context = DataContexts.GetJittorDataContext();

                var joins = request.Joins ?? new List<PageJoinModel>();
                request.Filters = request.Filters ?? new List<PageFilterModel>();

                var listerQuery = BuildListerQuery(request, (request.ColumnName ?? ""), joins,null, true);
                var list = tableContext.Fetch<FieldOption>(listerQuery.Sql).ToList();

                var defaultValues = (request.Values ?? "").Split(",").ToList();
                if(defaultValues.Any())
                    list.Where(x => defaultValues.Contains(x.Value.ToString())).ToList().ForEach(x => x.IsSelected = true);

                return new DropdownListerResponse()
                {
                    Items = list
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        #region Db Structure
        private List<string> GetAllRelatedTables(string mainTable)
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
        private void GetAllTableStructures()
        {
            var relationships = _tableContext.Fetch<TableRelationship>(@"
    SELECT 
        TP.name AS ParentTable,
        TR.name AS ChildTable
    FROM 
        sys.tables AS TP 
    LEFT JOIN 
        sys.foreign_keys AS FK
        ON FK.referenced_object_id = TP.object_id
    LEFT JOIN 
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
                .Distinct().Where(x => x != null)
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
            foreach (var rel in childRelationships.Where(x => x.ChildTable != null))
            {
                var childNode = tableDict[rel.ChildTable];
                parentNode.ChildTables.Add(childNode);
                BuildTableTree(rel.ChildTable, relationships, tableDict, visited);
            }
        }
        private static List<string> GetChildTableNames(string nodeName, List<TableNode> rootNodes, List<string>? excludeTables = null)
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
        private static string? ValidAlias(string alias)
        {
            if(ValidAliasRegex.IsMatch(alias))
            {
                return alias;
            }
            return null;
        }
        public BuildListerQueryResponse BuildListerQuery(DropdownListerRequest request, string selectClause, List<PageJoinModel>? joins = null,string? externalScripts = null, bool isDropDown = false)
        {
            List<string> JoinTypes = new List<string>() { "inner join", "outer join", "cross join", "left join", "right join" };

            selectClause = selectClause.ToLower();
            var selectColumnList = selectClause.Split(',').Select(x => x.Split("as")[0].Trim()).ToList();
            var asColumnDictionary = selectClause.Split(',').Select(column => column.Split(" as ")).Where(parts => parts.Length == 2).ToDictionary(parts => parts[0], parts => ValidAlias(parts[1])) ?? new Dictionary<string, string?>();

            var tableName = (request.TableName ?? "").Split(",")[0];
            request.TableName = (request.TableName ?? "").Split(".").Length == 2 ? (request.TableName ?? "").Split(".")[1] : request.TableName;

            var tables = request.TableName + (joins != null ? (joins.Count > 0 ? "," : "") + string.Join(",", joins.Select(x => x.JoinTable)) : "");
            request.TableName = (request.TableName ?? "").Split(",")[0];
            var tableColumns = GetTableAndChildTableColumns(tables ?? "", "dbo", request.IsArgaamContext == true ? _secondaryTableContext : null);
            selectColumnList = selectColumnList.ValidateTableColumns(tableColumns);
            request.Filters = request.Filters != null ? request.Filters.ValidateTableColumns(tableColumns) : new List<PageFilterModel>();

            var orders = (request.Sort ?? "").Split(",").Where(x => !string.IsNullOrEmpty(x)).ToList().ValidateTableColumns(tableColumns, true);

            if (selectColumnList.Any(x => x.Contains("*")))
            {
                selectColumnList.AddRange(tableColumns.Where(y => selectColumnList.Where(x => x.Contains("*")).Select(x => x.Replace(".*", "").Trim()).Contains(y.TableName)).Select(x => x.TableName + "." + x.ColumnName).ToList());
                selectColumnList.RemoveAll(x => x.Contains("*"));
                selectColumnList = selectColumnList.GroupBy(x => x.Split(".")[1]).Select(x => x.FirstOrDefault() ?? "").ToList();
            }

            string selectColumnsString = "";
            if (isDropDown)
                selectColumnsString = (selectColumnList.Count > 1 ? string.Join(" + ' - ' + ", selectColumnList.Select(column => $"ISNULL(CAST({column} as NVARCHAR(MAX)), '')")) : (selectColumnList.FirstOrDefault() ?? "")) + " as Label";
            else
            {
                selectColumnsString = string.Join(',', selectColumnList.Select(column =>
                {
                    var alias = asColumnDictionary.GetValueOrDefault<string, string?>(column);
                    return column + (alias == null ? "" : " as " + alias);
                }));
            }
            string primaryKey = "";
            if (tableColumns.FirstOrDefault(x => x.IsPrimaryKey == true & x.TableName.ToLower() == (request.TableName ?? "").ToLower()) != null)
            {
                primaryKey = request.TableName + "." + (tableColumns.FirstOrDefault(x => x.IsPrimaryKey == true & x.TableName.ToLower() == (request.TableName ?? "").ToLower())!.ColumnName ?? "") + (isDropDown ? " as Value, " : " as id, ");
            }
            else
            {
                primaryKey = request.TableName + "." + request.idColumn + (isDropDown ? " as Value, " : " as id, ");
            }
            var sql = Sql.Builder.Append($"SELECT {primaryKey} {selectColumnsString} FROM {tableName} ");

            var countSql = Sql.Builder.Append($"SELECT COUNT(1) FROM {tableName}");

            if (joins != null)
            {
                foreach (var join in joins.ValidateTableColumns(tableColumns))
                {
                    bool tableExists = tableColumns.Select(x => x.TableName).Contains(join.JoinTable.Replace("pub.", ""));

                    if (JoinTypes.Contains(join.JoinType.ToLower()) && tableExists)
                    {
                        sql.Append($" {join.JoinType} {join.JoinTable} on {join.ParentTableColumn} = {join.JoinTableColumn} ");
                        //if(join.FixedJoin == true)
                        countSql.Append($" {join.JoinType} {join.JoinTable} on {join.ParentTableColumn} = {join.JoinTableColumn} ");
                    }
                }
            }

            if ((request.Filters != null && request.Filters.Count > 0) || !string.IsNullOrEmpty(externalScripts))
            {
                sql.Append(" WHERE ");
                countSql.Append(" WHERE ");
                if ((request.Filters != null && request.Filters.Count > 0))
                {
                    //countSql.Append(request.Filters.Where(x => x.FixedFilter == true).Any() ? " WHERE " : "");
                    request.Filters.Where(x => !(x.ExternalSearch == true)).ToList().ForEach(filter =>
                    {
                        var newFilter = JsonConvert.DeserializeObject<PageFilterModel>(JsonConvert.SerializeObject(filter));
                        sql = sql.BuildWhereClause(filter, request.Filters.IndexOf(filter));
                        //if (filter.FixedFilter == true)
                        countSql = countSql.BuildWhereClause(newFilter ?? new PageFilterModel(), request.Filters.IndexOf(filter));
                    });
                }
                if(!string.IsNullOrEmpty(externalScripts))
                {
                    sql.Append(externalScripts);
                    countSql.Append(externalScripts);
                }
            }

            var customOrderingCases = request.CustomOrdering?.Split(",").Select(x => new { id = (x.Split(":")[0]), position = x.Split(":")[1] }).ToList() ?? null;
            string? primaryKeyColumn = request.idColumn;
            if (tableColumns.FirstOrDefault(x => x.IsPrimaryKey == true & x.TableName.ToLower() == (request.TableName ?? "").ToLower()) != null)
            {
                primaryKeyColumn = request.TableName + "." + (tableColumns.FirstOrDefault(x => x.IsPrimaryKey == true & x.TableName.ToLower() == (request.TableName ?? "").ToLower())!.ColumnName ?? "");
            }
                var orderString = orders.Count() > 0 ? string.Join(',', orders) : (primaryKeyColumn) + " DESC ";
            if (request.IsDistinct == true)
            {
                sql.Append($" GROUP BY {primaryKey.ToLower().Split("as")[0].Replace("," , "")}, {selectColumnsString.ToLower().Split("as")[0].Replace(",", "")}, {orderString.Split(" ")[0]} ");
            }
            var customOrderingSQL = new Sql();
            if(customOrderingCases != null && customOrderingCases.Count > 0)
            {
                customOrderingSQL.Append(" CASE ");
                customOrderingCases.ForEach(x => customOrderingSQL.Append($" WHEN {primaryKeyColumn} = {x.id} THEN {x.position} "));
                customOrderingSQL.Append($" ELSE {(int.Parse(customOrderingCases.OrderByDescending(x => x.position).Select(x => x.position)?.FirstOrDefault() ?? "0") + 1)} ");
                customOrderingSQL.Append(" END ");
                customOrderingSQL.Append($" ,{(string.IsNullOrEmpty(orderString?.Split(" ")[0] ?? null) ? primaryKeyColumn:orderString?.Split(" ")[0])} ");
            }
            sql.OrderBy((customOrderingCases != null && customOrderingCases.Count > 0) ? customOrderingSQL : orderString);

            if (!isDropDown && request.PageSize > 0)
            {
                int offset = ((request.PageNumber ?? 0) - 1) * (request.PageSize ?? 0);
                sql.Append($" OFFSET {offset} ROWS FETCH NEXT {(request.PageSize ?? 0)} ROWS ONLY ");
            }
            return new BuildListerQueryResponse()
            {
                Sql = sql,
                CountSql = countSql,
                SelectColumnList = selectColumnList,
                ColumnDictionary = asColumnDictionary
            };
        }

        #endregion
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
