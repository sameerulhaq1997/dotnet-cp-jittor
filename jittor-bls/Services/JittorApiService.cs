using Jittor.App.Models;

namespace Jittor.App.Services
{
    public class JittorApiService
    {
        JittorDataServices jittorDataServices;
        public JittorApiService(JittorDataServices jittorDataServices)
        {
            this.jittorDataServices = jittorDataServices;
        }
        public async Task<PageModel> GetPage(string pageName)
        {
            var res = await jittorDataServices.GetPageModel(pageName, true) ?? new JittorPageModel();
            return new PageModel() { PageName = pageName, JittorPageModel = res };
        }
        public async Task<List<FieldModel>> GetTableAndChildTableColumns(string tableName, string? schemaName = "dbo")
        {
            var columns = await jittorDataServices.GetTableAndChildTableColumns(tableName, schemaName);
            List<FieldModel> fields = new List<FieldModel>();
            foreach (var column in columns)
            {
                var splittedColumnDescription = column.ColumnDescription != null ? column.ColumnDescription.Split(",") : new string[0];

                FieldModel field = new FieldModel();
                field.TableName = column.TableName;
                field.Label = splittedColumnDescription.Length > 2 ? splittedColumnDescription[2] : column.ColumnName;
                field.HelperText = field.Label;
                field.Placeholder = field.Label;

                field.CssClasses = splittedColumnDescription.Length > 3 ? splittedColumnDescription[3] : column.ColumnName;
                field.CssID = field.CssClasses;

                field.Id = column.ColumnName;
                field.Name = column.ColumnName;

                field.IsDisabled = false;
                field.IsVisible = true;
                field.FieldType = splittedColumnDescription.Length > 0 ? splittedColumnDescription[0].ParseEnum<ApplicationFieldTypeEnum>() : ApplicationFieldTypeEnum.INPUT;
                field.FieldSubType = splittedColumnDescription.Length > 1 ? splittedColumnDescription[1].ParseEnum<ApplicationFieldSubTypeEnum>() : ApplicationFieldSubTypeEnum.TEXT;
                field.InpValue = new InpValue() { 
                    ActualValue = column.DefaultValue,
                    ValueType = column.DataType.GetApplicationValueTypeEnum()
                };
                field.Validations = new Dictionary<string, object>();
                if (column.IsNullable == "YES")
                    field.Validations.Add("required", true);
                if (column.MaxLength > 0)
                    field.Validations.Add("maxLength", column.MaxLength);
                if (column.NumericPrecision > 0)
                {
                    var maxNumber = "9".PadLeft(column.NumericPrecision - 1, '9');
                    field.Validations.Add("maxNumber", maxNumber);
                    field.Validations.Add("maxScale", column.NumericScale);
                }
                fields.Add(field);
            }
            return fields;
        }

            //public async Task<JsonResult> ProcessEntity([FromBody] Dictionary<string, object> keyValuePairs, string pageName)
            //{
            //    if (keyValuePairs.ContainsKey("IconId") && keyValuePairs["IconId"] != null)
            //    {
            //        if (keyValuePairs["IconId"].ToString() == "Select Icon" || keyValuePairs["IconId"].ToString() == "0" || keyValuePairs["IconId"] == null)
            //        {
            //            // Set Default Icon. 
            //            keyValuePairs["IconId"] = 17;
            //        }
            //    }

            //    if (pageName == "MacroCategoryPages")
            //    {
            //        if (!keyValuePairs.ContainsKey("CategoryId"))
            //        {
            //            var errormessage = "Please Select Category.";

            //            JsonResult ActivejsonResult = new JsonResult(new { Result = "Failed", Message = errormessage, Page = pageName });

            //            return ActivejsonResult;
            //        }
            //        else if (keyValuePairs.ContainsKey("CategoryId") && (keyValuePairs["CategoryId"].ToString() == "0" || keyValuePairs["CategoryId"].ToString() == "Select Category Name"))
            //        {
            //            var errormessage = "Please Select Valid Category.";

            //            JsonResult ActivejsonResult = new JsonResult(new { Result = "Failed", Message = errormessage, Page = pageName });

            //            return ActivejsonResult;
            //        }
            //        if (!keyValuePairs.ContainsKey("IconId"))
            //        {
            //            keyValuePairs["IconId"] = 17;
            //        }

            //    }
            //    else if (pageName == "Sections")
            //    {
            //        if (!keyValuePairs.ContainsKey("PageId"))
            //        {
            //            var errormessage = "Please Select Page.";

            //            JsonResult ActivejsonResult = new JsonResult(new { Result = "Failed", Message = errormessage, Page = pageName });

            //            return ActivejsonResult;
            //        }
            //        else if (keyValuePairs.ContainsKey("PageId") && (keyValuePairs["PageId"].ToString() == "0" || keyValuePairs["PageId"].ToString() == "Select Page Name"))
            //        {
            //            var errormessage = "Please Select Valid Page.";

            //            JsonResult ActivejsonResult = new JsonResult(new { Result = "Failed", Message = errormessage, Page = pageName });

            //            return ActivejsonResult;
            //        }
            //    }



            //    var results = new List<List<dynamic>>();
            //    try
            //    {

            //        if (keyValuePairs.ContainsKey("IsActive") && keyValuePairs.ContainsKey("ShowInFooter"))
            //        {
            //            if (keyValuePairs["IsActive"].ToString() == "False" && keyValuePairs["ShowInFooter"].ToString() == "True")
            //            {
            //                var errormessage = "Please Select Active Option. Otherwise De-Activate Show In Footer Option.";

            //                JsonResult ActivejsonResult = new JsonResult(new { Result = "Failed", Message = errormessage, Page = pageName });

            //                return ActivejsonResult;
            //            }
            //        }
            //        if (keyValuePairs.ContainsKey("ShowInFooter") && !keyValuePairs.ContainsKey("IsActive"))
            //        {
            //            var errormessage = "Please Select Active Option. Otherwise De-Activate Show In Footer Option.";

            //            JsonResult ActivejsonResult = new JsonResult(new { Result = "Failed", Message = errormessage, Page = pageName });

            //            return ActivejsonResult;
            //        }
            //        JittorPageModel? pageModel = await jittorDataServices.GetPageModel(pageName);
            //        int created = 0;
            //        int updated = 0;
            //        int deleted = 0;
            //        string message = "";
            //        if (pageModel != null)
            //        {
            //            var types = await jittorDataServices.GetAttributeTypes();
            //            var list = ProcessEntityModel.ProcessFrom(keyValuePairs, pageModel, types);


            //            foreach (var item in list)
            //            {
            //                if (item.ValidToCreate)
            //                {
            //                    var userID = 0;
            //                    var result = jittorDataServices.ExecuteCommand(userID, pageName, item.InsertCommand, item.SelectCommand, item.InsertParamaters, ActionTypeEnum.Insert.ToString());
            //                    results.Add(result);
            //                    created++;
            //                }
            //                if (item.ValidToUpdate)
            //                {
            //                    var userID = 0;
            //                    var result = jittorDataServices.ExecuteCommand(userID, pageName, item.UpdateCommand, item.SelectCommand, item.UpdateParamaters, ActionTypeEnum.Update.ToString());
            //                    results.Add(result);
            //                    updated++;
            //                }
            //                message += string.Join(", ", item.ErrorMessages);
            //            }

            //        }
            //        if (created > 0)
            //        {
            //            message += $"{created} record created !";
            //        }
            //        if (updated > 0)
            //        {
            //            message += $"{updated} record updated !";
            //        }

            //        if (created == 0 && updated == 0)
            //        {
            //            throw (new Exception(message));
            //        }
            //        JsonResult jsonResult = new JsonResult(new { Result = "Success", Message = message, Page = pageName, Data = results, Created = created, Updared = updated });

            //        return jsonResult;
            //    }
            //    catch (Exception e)
            //    {
            //        JsonResult jsonResult = new JsonResult(new { Result = "Failed", Message = e.Message, Page = pageName, Data = results, Created = 0, Updared = 0 });

            //        return jsonResult;
            //    }
            //}

            //public async Task<IActionResult> pageId([FromBody] jitterDeleteModel ID, int PageID)
            //{
            //    var results = new List<List<dynamic>>();

            //    int deleted = 0;
            //    string message = "";
            //    List<string> columnNames = ID.ColumnNames;
            //    List<object> idValues = ID.IdValues;
            //    JittorPageModel? jitorPageModel = new JittorPageModel();
            //    jitorPageModel = jittorDataServices.GetPageId(PageID);
            //    jitorPageModel = await jittorDataServices.GetPageModel(jitorPageModel.UrlFriendlyName, true);

            //    try
            //    {
            //        var firstColumnName = jitorPageModel.PageAttributes.Where(a => a.IsPrimaryKey).Select(a => a.AttributeName).FirstOrDefault();

            //        if (columnNames != null && idValues != null && columnNames.Count == idValues.Count && columnNames.Contains(firstColumnName))
            //        {
            //            bool chartbysectionid = false;

            //            if (firstColumnName == "SectionId")
            //            {
            //                chartbysectionid = jittorDataServices.getRecordsFromChartbySectionId(firstColumnName, ID.IdValues.FirstOrDefault());
            //            }
            //            if (!chartbysectionid)
            //            {
            //                var userID = 0;
            //                bool isDeleted = jittorDataServices.DeleteRecordByIdandPageName(userID, jitorPageModel.UrlFriendlyName, columnNames.FirstOrDefault(), idValues.FirstOrDefault());
            //                if (isDeleted)
            //                {
            //                    deleted++;
            //                }
            //                else
            //                {
            //                    // Record cannot be deleted because it is associated with a Chart
            //                    message = "Record cannot be deleted as it is associated with another record.";
            //                }
            //            }
            //            else
            //            {
            //                message = "Record cannot be deleted as it is associated with another record.";
            //            }
            //        }

            //        if (deleted > 0)
            //        {
            //            message += $"{deleted} record deleted !";
            //        }

            //        JsonResult jsonResult;
            //        if (message.Contains("associated with another record"))
            //        {
            //            jsonResult = new JsonResult(new { Result = "Failed", Message = message, Page = jitorPageModel.PageName, Data = results, Created = 0 });
            //        }
            //        else
            //        {
            //            jsonResult = new JsonResult(new { Result = "Success", Message = message, Page = jitorPageModel.PageName, Data = results, Created = deleted });
            //        }

            //        return jsonResult;
            //    }
            //    catch (Exception e)
            //    {
            //        JsonResult jsonResult = new JsonResult(new { Result = "Failed", Message = e.Message, Page = jitorPageModel.PageName, Data = results, Created = 0 });

            //        return jsonResult;
            //    }
            //}

            //public async Task<IActionResult> SortedJitterData(int PageID, List<SortableItem> newOrder)
            //{
            //    var results = new List<List<dynamic>>();
            //    bool result = false;

            //    JittorPageModel? jitorPageModel = new JittorPageModel();
            //    jitorPageModel = jittorDataServices.GetPageId(PageID);
            //    jitorPageModel = await jittorDataServices.GetPageModel(jitorPageModel.UrlFriendlyName, true);
            //    try
            //    {
            //        var DisplaySeqNo = jitorPageModel.PageAttributes.Select(a => a.AttributeName == "DisplaySeqNo").FirstOrDefault();

            //    }
            //    catch (Exception e)
            //    {

            //        return Ok(result ? "ok" : "error");
            //    }
            //    return Ok(result ? "ok" : "error");
            //}

        }
}
