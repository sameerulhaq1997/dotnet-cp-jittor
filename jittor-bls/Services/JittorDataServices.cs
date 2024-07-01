using MacroEconomics.Models;
using MacroEconomics.Shared;
using MacroEconomics.Shared.DataServices;
using MacroEconomics.Shared.Enums;
using MacroEconomics.Shared.Helpers;
using System.Data.SqlClient;
using static MacroEconomics.Shared.DataServices.FrameworkRepository;

namespace Jittor.Services
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
        public async Task<JittorPageModel?> GetPageModel(string urlFriendlyPageName, bool loadData)
        {
          
            return await Executor.Instance.GetDataAsync<JittorPageModel?>(() => {
                using var tableContext = _tableContext;
                using var context = DataContexts.GetJittorDataContext();
                JittorPageModel? model = GetPageModel(urlFriendlyPageName).Result;
               
                if (loadData && model != null)
                {
                    model.PageTablesData.Clear();
                    foreach (var table in model.PageTables.Where(x => x.ForView))
                    {
                        var list = tableContext.Fetch<dynamic>($"Select * From {table.TableName} Order By ModifiedOn desc").ToList();
                        model.PageTablesData.Add(table.TableName, list);
                    }
                    foreach (var att in model.PageAttributes.Where(x => x.IsForeignKey && !string.IsNullOrEmpty(x.ParentTableName)))
                    {
                        att.ForigenValues = context.Fetch<ForigenValue>($"Select {att.AttributeName} As ID, {att.ParentTableNameColumn} As Name From {att.ParentTableName}  {(string.IsNullOrEmpty(att.ParentCondition) ? "" :  att.ParentCondition) } order by {att.AttributeName} desc ").ToList();
                    }
                    foreach (var att in model.PageAttributes.Where(x => !string.IsNullOrEmpty(x.AlternameValuesQuery)))
                    {
                        att.AlternateValues = context.Fetch<string>(att.AlternameValuesQuery).FirstOrDefault();
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

    }
}
