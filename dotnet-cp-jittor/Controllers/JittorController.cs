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
            var jitorPageModel = await _jittorService.GetPage(pageName);
            return Ok(jitorPageModel);
        }
        [HttpPost("form-builder")]
        public async Task<IActionResult> SavePage([FromBody] FormPageModel form)
        {
            var jitorPageModel = await _jittorService.CreateNewPage(form);
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
        public async Task<IActionResult> GetPageLister(int pageId, int pageNumber = 1, int pageSize = 10, string? sort = null)
        {
            try
            {
                Request.Headers.TryGetValue("filters", out StringValues filtersString);
                var filters = filtersString.Count > 0 ? (JsonConvert.DeserializeObject<List<PageFilterModel>>(filtersString.ToString()) ?? new List<PageFilterModel>()) : null;
                //var filters = filtersString.Count > 0 ? (JsonConvert.DeserializeObject<Dictionary<string, string>?>(filtersString.ToString())) : null;

                var request = new DataListerRequest()
                {
                    PageId = pageId,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Sort = sort,
                    Filters = filters
                };
                var res = await _jittorService.GetPageLister(request);
                return Ok(res);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("form-builder/lister/{pageId}")]
        public async Task<IActionResult> GetFormBuilderLister(int pageId)
        {
            var res = await _jittorService.GetFormBuilderLister(pageId);
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

    }
}
