using Jittor.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Jittor.App.Models;
using static Jittor.App.Models.ProcessEntityModel;
using Jittor.App.Services;

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
        public async Task<IActionResult> Page(string pageName)
        {
            var jitorPageModel = await _jittorService.GetPage(pageName);
            return Ok(jitorPageModel);
        }
        [HttpGet("colums/{tableName}")]
        public async Task<IActionResult> Page(string tableName, string? schema = "dbo")
        {
            var jitorPageModel = await _jittorService.GetTableAndChildTableColumns(tableName, schema);
            var groupedRes = jitorPageModel.GroupBy(x => x.TableName).Select(x => new { TableName = x.Key, Fields = x });
            return Ok(groupedRes);
        }
    }
}
