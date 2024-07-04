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
        [HttpGet("tables")]
        public async Task<IActionResult> GetAllTables()
        {
            var jitorTableModel = await _jittorService.GetAllTables();
            //var groupedRes = jitorPageModel.GroupBy(x => x.TableName).Select(x => new { TableName = x.Key, Fields = x });
            return Ok(jitorTableModel);
        }
    }
}
