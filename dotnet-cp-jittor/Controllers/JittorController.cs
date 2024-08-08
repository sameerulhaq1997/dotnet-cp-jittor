using Jittor.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Jittor.App.Models;
using static Jittor.App.Models.ProcessEntityModel;
using Jittor.App.Services;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using dotnet_cp_jitter.Extender;

namespace Jittor.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JittorController : ControllerBase
    {
        JittorApiService _jittorService;
        public JittorController(JittorApiService jittorService)
        {
            _jittorService = jittorService;
        }
        [HttpGet("page/{pageName}")]
        public async Task<IActionResult> GetPage(string pageName)
        {
            var jitorPageModel = await _jittorService.GetPage(pageName, false);
            return Ok(jitorPageModel);
        }
        [HttpPost("form-builder")]
        public async Task<IActionResult> SavePage([FromBody] FormPageModel form, int? pageID = null)
        {
            var jitorPageModel = await _jittorService.CreateNewPage(form, pageID);
            return Ok(jitorPageModel);
        }
        [HttpGet("colums/{tableName}")]
        public async Task<IActionResult> GetTableColumns(string tableName, string? schema = "dbo")
        {
            var jitorPageModel = await _jittorService.GetTableAndChildTableColumns(tableName, schema);
            var groupedRes = jitorPageModel.GroupBy(x => x.TableName).Select(x => new { TableName = x.Key, Fields = x });
            return Ok(groupedRes);
        }
        [HttpPost("page/process-entity/{pageName}")]
        public async Task<IActionResult> ProcessEntity(Dictionary<string, object> keyValuePairs, string pageName)
        {
            var res = await _jittorService.ProcessEntity(keyValuePairs, pageName);
            return Ok(res);
        }
        [HttpPost("page/deleteRecord/{PageID}")]
        public async Task<IActionResult> pageId([FromBody] jitterDeleteModel ID, int PageID)
        {
            var res = await _jittorService.pageId(ID, PageID);
            return Ok(res);
        }
        [HttpPost("page/SortedData")]
        public async Task<IActionResult> SortedJitterData(int PageID, List<SortableItem> newOrder)
        {
            var res = await _jittorService.SortedJitterData(PageID, newOrder);
            return Ok(res);
        }
        [HttpGet("page/lister/{pageId}")]
        public IActionResult GetPageLister(int pageId, int pageNumber = 1, int pageSize = 10, string? sort = null)
        {
            try
            {
                Request.Headers.TryGetValue("filters", out StringValues filtersString);
                var filters = filtersString.Count > 0 ? (JsonConvert.DeserializeObject<List<PageFilterModel>>(filtersString.ToString()) ?? new List<PageFilterModel>()) : null;

                var request = new DataListerRequest()
                {
                    PageId = pageId,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Sort = sort,
                    Filters = filters
                };
                var res = _jittorService.GetPageLister(request);
                return Ok(res);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("page/lister/articles")]
        public IActionResult GetPageListerArticles(int pageId, int pageNumber = 1, int pageSize = 10, string? sort = null)
        {
            try
            {
                Request.Headers.TryGetValue("filters", out StringValues filtersString);
                var filters = filtersString.Count > 0 ? (JsonConvert.DeserializeObject<List<PageFilterModel>>(filtersString.ToString()) ?? new List<PageFilterModel>()) : null;
                var lang = filters != null ? filters.FirstOrDefault(x => x.Field == "Articles.LanguageID")?.Value ?? "1" : "1";

                var request = new DataListerRequest()
                {
                    PageId = pageId,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Sort = sort ?? "Articles.ArticleID DESC",
                    Filters = filters
                };
                var joins = new List<PageJoinModel>()
                {
                    new PageJoinModel()
                    {
                        JoinType = "left join",
                        JoinTable = "ArticleStatuses",
                        ParentTableColumn = "Articles.ArticleStatusID",
                        JoinTableColumn = "ArticleStatuses.ArticleStatusID"
                    },
                    new PageJoinModel()
                    {
                        JoinType = "left join",
                        JoinTable = "ArticleTypes",
                        ParentTableColumn = "Articles.ArticleTypeID",
                        JoinTableColumn = "ArticleTypes.ArticleTypeID"
                    },
                    new PageJoinModel()
                    {
                        JoinType = "left join",
                        JoinTable = "ArticleViews",
                        ParentTableColumn = "Articles.ArticleID",
                        JoinTableColumn = "ArticleViews.ArticleID"
                    }
                };

                if(filters != null && filters.Any(x => x.Field == "ArticleFeatures.IsModerate"))
                {
                    joins.Add(new PageJoinModel()
                    {
                        JoinType = "inner join",
                        JoinTable = "ArticleFeatures",
                        ParentTableColumn = "Articles.ArticleID",
                        JoinTableColumn = "ArticleFeatures.ArticleID"
                    });
                }
                if (filters != null && filters.Any(x => x.Field == "ArticleExtendedData.CreatedOn"))
                {
                    joins.Add(new PageJoinModel()
                    {
                        JoinType = "inner join",
                        JoinTable = "ArticleExtendedData",
                        ParentTableColumn = "Articles.ArticleID",
                        JoinTableColumn = "ArticleExtendedData.ArticleID"
                    });
                }
                if (filters != null && filters.Any(x => x.Field == "MarketID" || x.Field == "CompanyID" || x.Field == "SectorID" || x.Field == "ArgaamSectorID"))
                {
                    joins.Add(new PageJoinModel()
                    {
                        JoinType = "inner join",
                        JoinTable = "ArticleRelatedToEntities",
                        ParentTableColumn = "Articles.ArticleID",
                        JoinTableColumn = "ArticleRelatedToEntities.ArticleID"
                    });
                }

                 DynamicExtender? extender = null;
                if (filters != null && filters.Any(x => x.ExternalSearch == true))
                    extender = this.GetExtandered("ArticleListerExtender");

                var res = _jittorService.GetPageLister(request, "articles", $"Articles.ArticleID,Articles.Title,Articles.Author,ArticleTypes.Name{(lang == "1" ? "Ar" : "En")} AS Type,ArticleStatuses.Name{(lang == "1" ? "Ar" : "En")} AS Status,ArticleViews.ViewCount", joins, extender == null ? null : extender.ExecuteExternalScripts(filters));
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("table/dropdown/{tableName}/{columnName}")]
        public IActionResult PoplulateDropDowns(string tableName, string columnName, string? sort = null, bool? isArgaamContext = false, bool? isDistinct = false)
        {
            try
            {
                Request.Headers.TryGetValue("filters", out StringValues filtersString);
                var filters = filtersString.Count > 0 ? (JsonConvert.DeserializeObject<List<PageFilterModel>>(filtersString.ToString()) ?? new List<PageFilterModel>()) : null;

                Request.Headers.TryGetValue("joins", out StringValues joinsString);
                var joins = joinsString.Count > 0 ? (JsonConvert.DeserializeObject<List<PageJoinModel>>(joinsString.ToString()) ?? new List<PageJoinModel>()) : null;

                DropdownListerRequest request = new DropdownListerRequest()
                {
                    TableName = tableName,
                    ColumnName = columnName,
                    Sort = sort,
                    Filters = filters,
                    Joins = joins,
                    IsArgaamContext = isArgaamContext,
                    IsDistinct = isDistinct
                };
                var res = _jittorService.PoplulateDropDowns(request);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("form-builder/lister/{pageId}")]
        public async Task<IActionResult> GetFormBuilderLister(int pageId = 0)
        {
            var res = await _jittorService.GetFormBuilderLister(pageId);
            return Ok(res);
        }
        [HttpGet("form-builder/edit/{pageId}")]
        public async Task<IActionResult> GetFormBuilderPageData(int pageId)//Load Form Builder Edit Page
        {
            var res = await _jittorService.GetFormBuilderPageData(pageId);
            return Ok(res);
        }
        [HttpGet("form-builder/lister")]
        public async Task<IActionResult> GetFormBuilderLister()
        {
            var res = await _jittorService.GetFormBuilderLister();
            return Ok(res);
        }
        [HttpGet("tables")]
        public async Task<IActionResult> GetAllTables()
        {
            var jitorTableModel = await _jittorService.GetAllTables();
            //var groupedRes = jitorPageModel.GroupBy(x => x.TableName).Select(x => new { TableName = x.Key, Fields = x });
            return Ok(jitorTableModel);
        }
        [HttpGet("pages")]
        public async Task<IActionResult> GetAllPages()
        {
            var jitorTableModel = await _jittorService.GetAllPages();
            return Ok(jitorTableModel);
        }
        [HttpDelete("page/deleteForm/{pageID}")]
        public async Task<IActionResult> DeleteForm(int pageID)
        {
            var res = await _jittorService.DeleteForm(pageID);
            return Ok(res);
        }

        private DynamicExtender GetExtandered(string name)
        {
            var obj = Activator.CreateInstance("Jittor.Api", string.Format("dotnet_cp_jitter.Extender.{0}", name));
            return (DynamicExtender)obj.Unwrap();
        }
    }
}
