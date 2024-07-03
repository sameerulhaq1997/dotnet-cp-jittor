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

namespace Jittor.App.Services
{
	public class JittorDataServices 
	{
        private readonly FrameworkRepository _tableContext;
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
                     .Select("*")
                     .From("JITPages")
                     .Where("UrlFriendlyName = @0", urlFriendlyPageName);
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

                        selectedRecord.Add(att.AttributeName, at.GetDefaultValue());
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
                        var selectClause = table.SelectColumns ?? "*";
                        string query = $"SELECT {selectClause} FROM {table.TableName}";

                        var externalTables = selectClause.Split(",").Where(x => !x.Contains(table.TableName)).Select(x => x.Split('.')[0]).Distinct().ToList();
                        var joins = table.Joins.Split(',');
                        foreach (var item in joins)
                        {
                            query += " " + item;
                        }

                        var filters = JsonConvert.DeserializeObject<Dictionary<string, string>>(table.Filters);
                        if (filters != null && filters.Count > 0)
                        {
                            var whereClause = string.Join(" AND ", filters.Select(x => x.Key + " = " + x.Value));
                            query += " WHERE " + whereClause;
                        }

                        var orderBy = JsonConvert.DeserializeObject<Dictionary<string, string>>(table.Orders);
                        if (orderBy != null && orderBy.Any())
                        {
                            var orderClause = string.Join(" , ", orderBy.Select(x => x.Key + " " + x.Value));
                            query += " Order By " + orderClause;
                        }
                        else
                            query += "Order By ModifiedOn desc";

                        if (pageSize > 0)
                        {
                            int offset = (pageNo - 1) * (pageSize ?? 0);
                            query += $" OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
                        }


                        var list = tableContext.Fetch<dynamic>($"Select * From {table.TableName} Order By ModifiedOn desc").ToList();
                        model.PageTablesData.Add(table.TableName, list);
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

                var page = new JITPage();
                page.PageName = form.Form.FormName;
                page.UrlFriendlyName = form.Form.FormName;
                page.Title = form.Form.FormName;
                page.GroupID = 1;
                page.AddNew = true;
                page.EditRecord = true;
                page.DeleteRecord = true;
                page.SoftDeleteColumn = form.Form.SoftDeleteColumn;
                page.Preview = true;
                page.ShowSearch = form.Form.ShowSearch;
                page.ShowListing = form.Form.ShowListing;
                page.ListingTitle = form.Form.ListingTitle;
                page.Extender = form.Extender;
                page.Description = form.Form.Description;
                page.ShowFilters = form.Form.ShowFilters;
                page.RecordsPerPage = form.Form.RecordsPerPage;
                page.CurrentPage = form.Form.CurrentPage;
                page.PageView = "";
                page.ListingCommands = "";
                context.Insert(page);


                var tableNames = form.Sections.SelectMany(x => x.Fields).Select(x => x.TableName).Distinct().ToList();
                List<JITPageTable> tables = new List<JITPageTable>();
                foreach (var item in tableNames)
                {
                    JITPageTable table = new JITPageTable();
                    table.PageID = page.PageID;
                    table.TableName = form.Form.TableName;
                    table.TableAlias = "";
                    table.ForView = item == form.Form.TableName ? true : form.Form.ShowSearch;
                    table.ForOperation = true;
                    table.SelectColumns = form.Form.SelectColumns;
                    table.Filters = form.Form.Filters;
                    table.Orders = form.Form.Orders;
                    table.Joins = form.Form.Joins;
                    tables.Add(table);
                }
                context.Insert(tables);


                var attributes = new List<JITPageAttribute>();
                foreach (var section in form.Sections)
                {
                    foreach (var field in section.Fields)
                    {
                        var currentColumn = tableAndChildTableColumns.FirstOrDefault(x => x.ColumnName == field.Id);
                        if (currentColumn != null)
                        {
                            var attributeType = attributeTypes.FirstOrDefault(x => x.TypeName == currentColumn.DataType);

                            JITPageAttribute attribute = new JITPageAttribute();
                            attribute.PageID = page.PageID;
                            attribute.TableID = tables.FirstOrDefault(x => x.TableName == currentColumn.TableName)?.TableID ?? 0;
                            attribute.AttributeName = field.Name;
                            attribute.DisplayNameAr = field.LabelAr;
                            attribute.DisplayNameEn = field.LabelEn;
                            attribute.AttributeTypeID = attributeType?.AttributeTypeID ?? 0;
                            attribute.IsRequired = field.Validations.ContainsKey("required");
                            attribute.IsForeignKey = currentColumn.IsForeignKey;
                            attribute.ParentTableName = "Users";
                            attribute.ParentTableNameColumn = "UserID";
                            attribute.ParentCondition = "IsActive = 1";
                            attribute.AutoComplete = true;
                            attribute.AddNewParentRecord = false;

                            attribute.ValidationExpression = JsonConvert.SerializeObject(field.Validations);
                            attribute.IsAutoIncreament = currentColumn.IsAutoIncrement;
                            attribute.IsPrimaryKey = currentColumn.IsPrimaryKey;
                            attribute.Editable = !field.IsDisabled;
                            attribute.Searchable = true;
                            attribute.Displayable = true;
                            attribute.Sortable = true;
                            attribute.Filterable = true;
                            attribute.EditableSeqNo = 1;
                            attribute.SearchableSeqNo = 1;
                            attribute.DisplayableSeqNo = 1;
                            attribute.MaxLength = field.Validations.ContainsKey("maxLenth") ? int.Parse(field.Validations.FirstOrDefault(x => x.Key == "maxLenth").Value.ToString() ?? "0") : currentColumn.MaxLength;
                            attribute.PlaceholderText = field.Placeholder;
                            attribute.DisplayFormat = "";
                            attribute.InputPartialView = "";
                            attribute.DefaultValue = field.InpValue.ActualValue;
                            attribute.IsRange = false;
                            attribute.Range = "";
                            attribute.DisplayGroupID = 1;
                            attribute.DisplayStyle = "default";
                            attribute.IsFile = false;
                            attribute.AllowListInput = false;
                            attribute.UploadPath = "";
                            attribute.FileType = "";
                            attribute.PartialURLTemplate = "";
                            attribute.AlternateValues = "";
                            attribute.AlternameValuesQuery = "";
                            attributes.Add(attribute);
                        }
                    }
                }
                context.Insert(attributes);
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

    }
}
