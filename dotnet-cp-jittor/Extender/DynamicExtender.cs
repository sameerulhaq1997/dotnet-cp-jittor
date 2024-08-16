using Jittor.App.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dotnet_cp_jitter.Extender
{


    public abstract  class DynamicExtender
    {
        public virtual string ExecuteFilterScripts(List<PageFilterModel>? filters) { return string.Empty; }
        public virtual string ExecuteDeleteScripts(string value) { return string.Empty; }
    }

    public  class ArticleListerExtender : DynamicExtender
    {
        public override string ExecuteFilterScripts(List<PageFilterModel>? filters)
        {
            var query = "";
            if(filters == null)
            {
                return query;
            }

            var marketId = filters.FirstOrDefault(x => x.Field == "MarketID")?.Value;
            var companyId = filters.FirstOrDefault(x => x.Field == "CompanyID")?.Value;
            var sectorId = filters.FirstOrDefault(x => x.Field == "SectorID")?.Value;
            var argaamSectorId = filters.FirstOrDefault(x => x.Field == "ArgaamSectorID")?.Value;

            if (!string.IsNullOrEmpty(marketId))
            {
                query += $" AND (ArticleRelatedToEntities.RowID = {marketId} AND ArticleRelatedToEntities.EntityID = 3) ";
            }
            if (!string.IsNullOrEmpty(companyId))
            {
                query += $" AND (ArticleRelatedToEntities.RowID = {companyId} AND ArticleRelatedToEntities.EntityID = 4) ";
            }
            if (!string.IsNullOrEmpty(sectorId))
            {
                query += $" AND (ArticleRelatedToEntities.RowID = {sectorId} AND ArticleRelatedToEntities.EntityID = 5) ";
            }
            if (!string.IsNullOrEmpty(argaamSectorId))
            {
                query += $" AND (ArticleRelatedToEntities.RowID = {argaamSectorId} AND ArticleRelatedToEntities.EntityID = 6) ";
            }

            return query;
        }
    }

    public class ArticleId_Delete : DynamicExtender
    {
        public override string ExecuteDeleteScripts(string value)
        {
            return $"Update Articles Set IsDeleted = 1,ArticleStatusID = 7 Where ArticleID = {value}";
        }
    }
}
