using Jittor.Services;
using MacroEconomics.Models;
using MacroEconomics.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static MacroEconomics.Models.ProcessEntityModel;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JittorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JittorController : ControllerBase
    {
        JittorDataServices jittorDataServices;
        IHttpContextAccessor _httpContextAccessor;
        public JittorController(JittorDataServices jittorDataServices, IHttpContextAccessor httpContextAccessor)
        {
            this.jittorDataServices = jittorDataServices;
            this._httpContextAccessor = httpContextAccessor;
        }


        [HttpGet("page/{pageName}")]
        public async Task<IActionResult> Page(string pageName)
        {
            JittorPageModel? jitorPageModel = await jittorDataServices.GetPageModel(pageName, true);
            var res = new { PageName = pageName, jitorPageModel };
            return Ok(res);
        }
        [HttpPost("page/process-entity/{pageName}")]
        public async Task<JsonResult> ProcessEntity([FromBody] Dictionary<string, object> keyValuePairs, string pageName)
        {
            if (keyValuePairs.ContainsKey("IconId") && keyValuePairs["IconId"] != null)
            {
                if (keyValuePairs["IconId"].ToString() == "Select Icon" || keyValuePairs["IconId"].ToString() == "0" || keyValuePairs["IconId"] == null)
                {
                    // Set Default Icon. 
                    keyValuePairs["IconId"] = 17;
                }
            }

            if (pageName == "MacroCategoryPages")
            {
                if (!keyValuePairs.ContainsKey("CategoryId"))
                {
                    var errormessage = "Please Select Category.";

                    JsonResult ActivejsonResult = new JsonResult(new { Result = "Failed", Message = errormessage, Page = pageName });

                    return ActivejsonResult;
                }
                else if (keyValuePairs.ContainsKey("CategoryId") && (keyValuePairs["CategoryId"].ToString() == "0" || keyValuePairs["CategoryId"].ToString() == "Select Category Name"))
                {
                    var errormessage = "Please Select Valid Category.";

                    JsonResult ActivejsonResult = new JsonResult(new { Result = "Failed", Message = errormessage, Page = pageName });

                    return ActivejsonResult;
                }
                if (!keyValuePairs.ContainsKey("IconId"))
                {
                    keyValuePairs["IconId"] = 17;
                }

            }
            else if (pageName == "Sections")
            {
                if (!keyValuePairs.ContainsKey("PageId"))
                {
                    var errormessage = "Please Select Page.";

                    JsonResult ActivejsonResult = new JsonResult(new { Result = "Failed", Message = errormessage, Page = pageName });

                    return ActivejsonResult;
                }
                else if (keyValuePairs.ContainsKey("PageId") && (keyValuePairs["PageId"].ToString() == "0" || keyValuePairs["PageId"].ToString() == "Select Page Name"))
                {
                    var errormessage = "Please Select Valid Page.";

                    JsonResult ActivejsonResult = new JsonResult(new { Result = "Failed", Message = errormessage, Page = pageName });

                    return ActivejsonResult;
                }
            }



            var results = new List<List<dynamic>>();
            try
            {

                if (keyValuePairs.ContainsKey("IsActive") && keyValuePairs.ContainsKey("ShowInFooter"))
                {
                    if (keyValuePairs["IsActive"].ToString() == "False" && keyValuePairs["ShowInFooter"].ToString() == "True")
                    {
                        var errormessage = "Please Select Active Option. Otherwise De-Activate Show In Footer Option.";

                        JsonResult ActivejsonResult = new JsonResult(new { Result = "Failed", Message = errormessage, Page = pageName });

                        return ActivejsonResult;
                    }
                }
                if (keyValuePairs.ContainsKey("ShowInFooter") && !keyValuePairs.ContainsKey("IsActive"))
                {
                    var errormessage = "Please Select Active Option. Otherwise De-Activate Show In Footer Option.";

                    JsonResult ActivejsonResult = new JsonResult(new { Result = "Failed", Message = errormessage, Page = pageName });

                    return ActivejsonResult;
                }
                JittorPageModel? pageModel = await jittorDataServices.GetPageModel(pageName);
                int created = 0;
                int updated = 0;
                int deleted = 0;
                string message = "";
                if (pageModel != null)
                {
                    var types = await jittorDataServices.GetAttributeTypes();
                    var list = ProcessEntityModel.ProcessFrom(keyValuePairs, pageModel, types);


                    foreach (var item in list)
                    {
                        if (item.ValidToCreate)
                        {
                            var userID = int.Parse(_httpContextAccessor.HttpContext.Session.GetString("userID"));
                            var result = jittorDataServices.ExecuteCommand(userID, pageName, item.InsertCommand, item.SelectCommand, item.InsertParamaters, ActionTypeEnum.Insert.ToString());
                            results.Add(result);
                            created++;
                        }
                        if (item.ValidToUpdate)
                        {
                            var userID = int.Parse(_httpContextAccessor.HttpContext.Session.GetString("userID"));
                            var result = jittorDataServices.ExecuteCommand(userID, pageName, item.UpdateCommand, item.SelectCommand, item.UpdateParamaters, ActionTypeEnum.Update.ToString());
                            results.Add(result);
                            updated++;
                        }
                        message += string.Join(", ", item.ErrorMessages);
                    }

                }
                if (created > 0)
                {
                    message += $"{created} record created !";
                }
                if (updated > 0)
                {
                    message += $"{updated} record updated !";
                }

                if (created == 0 && updated == 0)
                {
                    throw (new Exception(message));
                }
                JsonResult jsonResult = new JsonResult(new { Result = "Success", Message = message, Page = pageName, Data = results, Created = created, Updared = updated });

                return jsonResult;
            }
            catch (Exception e)
            {
                JsonResult jsonResult = new JsonResult(new { Result = "Failed", Message = e.Message, Page = pageName, Data = results, Created = 0, Updared = 0 });

                return jsonResult;
            }
        }

        [HttpPost("page/deleteRecord/{PageID}")]
        public async Task<IActionResult> pageId([FromBody] jitterDeleteModel ID, int PageID)
        {
            var results = new List<List<dynamic>>();

            int deleted = 0;
            string message = "";
            List<string> columnNames = ID.ColumnNames;
            List<object> idValues = ID.IdValues;
            JittorPageModel? jitorPageModel = new JittorPageModel();
            jitorPageModel = jittorDataServices.GetPageId(PageID);
            jitorPageModel = await jittorDataServices.GetPageModel(jitorPageModel.UrlFriendlyName, true);

            try
            {
                var firstColumnName = jitorPageModel.PageAttributes.Where(a => a.IsPrimaryKey).Select(a => a.AttributeName).FirstOrDefault();

                if (columnNames != null && idValues != null && columnNames.Count == idValues.Count && columnNames.Contains(firstColumnName))
                {
                    bool chartbysectionid = false;

                    if (firstColumnName == "SectionId")
                    {
                        chartbysectionid = jittorDataServices.getRecordsFromChartbySectionId(firstColumnName, ID.IdValues.FirstOrDefault());
                    }
                    if (!chartbysectionid)
                    {
                        var userID = int.Parse(_httpContextAccessor.HttpContext.Session.GetString("userID"));
                        bool isDeleted = jittorDataServices.DeleteRecordByIdandPageName(userID, jitorPageModel.UrlFriendlyName, columnNames.FirstOrDefault(), idValues.FirstOrDefault());
                        if (isDeleted)
                        {
                            deleted++;
                        }
                        else
                        {
                            // Record cannot be deleted because it is associated with a Chart
                            message = "Record cannot be deleted as it is associated with another record.";
                        }
                    }
                    else
                    {
                        message = "Record cannot be deleted as it is associated with another record.";
                    }
                }

                if (deleted > 0)
                {
                    message += $"{deleted} record deleted !";
                }

                JsonResult jsonResult;
                if (message.Contains("associated with another record"))
                {
                    jsonResult = new JsonResult(new { Result = "Failed", Message = message, Page = jitorPageModel.PageName, Data = results, Created = 0 });
                }
                else
                {
                    jsonResult = new JsonResult(new { Result = "Success", Message = message, Page = jitorPageModel.PageName, Data = results, Created = deleted });
                }

                return jsonResult;
            }
            catch (Exception e)
            {
                JsonResult jsonResult = new JsonResult(new { Result = "Failed", Message = e.Message, Page = jitorPageModel.PageName, Data = results, Created = 0 });

                return jsonResult;
            }
        }

        [HttpPost("page/SortedData")]
        public async Task<IActionResult> SortedJitterData(int PageID, List<SortableItem> newOrder)
        {
            var results = new List<List<dynamic>>();
            bool result = false;

            JittorPageModel? jitorPageModel = new JittorPageModel();
            jitorPageModel = jittorDataServices.GetPageId(PageID);
            jitorPageModel = await jittorDataServices.GetPageModel(jitorPageModel.UrlFriendlyName, true);
            try
            {
                var DisplaySeqNo = jitorPageModel.PageAttributes.Select(a => a.AttributeName == "DisplaySeqNo").FirstOrDefault();

            }
            catch (Exception e)
            {

                return Ok(result ? "ok" : "error");
            }
            return Ok(result ? "ok" : "error");
        }

    }
}
