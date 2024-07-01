using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PetaPoco.Core;
using PetaPoco.Utilities;

namespace PetaPoco.Providers
{
    public class SqlServerDatabaseProvider : DatabaseProvider
    {
        public override DbProviderFactory GetFactory()
        {
            return GetFactory("System.Data.SqlClient.SqlClientFactory, System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
        }

        public override string BuildPageQuery(long skip, long take, SQLParts parts, ref object[] args)
        {
            PagingHelper helper = (PagingHelper) PagingUtility;
            // when the query does not contain an "order by", it is very slow
            if (helper.SimpleRegexOrderBy.IsMatch(parts.SqlSelectRemoved))
            {
                System.Text.RegularExpressions.Match m = helper.SimpleRegexOrderBy.Match(parts.SqlSelectRemoved);
                if (m.Success)
                {
                    System.Text.RegularExpressions.Group g = m.Groups[0];
                    parts.SqlSelectRemoved = parts.SqlSelectRemoved.Substring(0, g.Index);
                }
            }

            if (helper.RegexDistinct.IsMatch(parts.SqlSelectRemoved))
                parts.SqlSelectRemoved = "peta_inner.* FROM (SELECT " + parts.SqlSelectRemoved + ") peta_inner";

            string sqlPage =
                $"SELECT * FROM (SELECT ROW_NUMBER() OVER ({parts.SqlOrderBy ?? "ORDER BY (SELECT NULL)"}) peta_rn, {parts.SqlSelectRemoved}) peta_paged WHERE peta_rn > @{args.Length} AND peta_rn <= @{args.Length + 1}";
            args = args.Concat(new object[] { skip, skip + take }).ToArray();
            return sqlPage;
        }

        public override object ExecuteInsert(Database db, IDbCommand cmd, string primaryKeyName)
        {
            return ExecuteScalarHelper(db, cmd);
        }

        public override string GetExistsSql()
        {
            return "IF EXISTS (SELECT 1 FROM {0} WHERE {1}) SELECT 1 ELSE SELECT 0";
        }

        public override string GetInsertOutputClause(string primaryKeyName)
        {
            return $" OUTPUT INSERTED.[{primaryKeyName}]";
        }

#if ASYNC
        public override Task<object> ExecuteInsertAsync(CancellationToken cancellationToken, Database db, IDbCommand cmd, string primaryKeyName)
            => ExecuteScalarHelperAsync(cancellationToken, db, cmd);
#endif
    }
}