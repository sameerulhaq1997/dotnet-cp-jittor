using Jittor.App.Models;

namespace dotnet_cp_jitter.Extender
{


    public abstract  class DynamicExtender
    {
        public virtual string ExecuteExternalScripts(List<PageFilterModel> filters) { return string.Empty; }
    }

    public  class ArticleListerExtender : DynamicExtender
    {
        public override string ExecuteExternalScripts(List<PageFilterModel> filters)
        {
            var query = "";

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
}
