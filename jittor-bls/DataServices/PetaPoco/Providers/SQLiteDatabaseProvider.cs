﻿using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using PetaPoco.Core;

namespace PetaPoco.Providers
{
    public class SQLiteDatabaseProvider : DatabaseProvider
    {
        public override DbProviderFactory GetFactory()
        {
            return GetFactory("System.Data.SQLite.SQLiteFactory, System.Data.SQLite", "Microsoft.Data.Sqlite.SqliteFactory, Microsoft.Data.Sqlite");
        }

        public override object MapParameterValue(object value)
        {
            if (value is uint u)
                return (long) u;

            return base.MapParameterValue(value);
        }

        public override object ExecuteInsert(Database db, IDbCommand cmd, string primaryKeyName)
        {
            if (primaryKeyName != null)
            {
                cmd.CommandText += ";\nSELECT last_insert_rowid();";
                return ExecuteScalarHelper(db, cmd);
            }

            ExecuteNonQueryHelper(db, cmd);
            return -1;
        }

#if ASYNC

        public override async Task<object> ExecuteInsertAsync(CancellationToken cancellationToken, Database db, IDbCommand cmd, string primaryKeyName)
        {
            if (primaryKeyName != null)
            {
                cmd.CommandText += ";\nSELECT last_insert_rowid();";
                return await ExecuteScalarHelperAsync(cancellationToken, db, cmd);
            }

            await ExecuteNonQueryHelperAsync(cancellationToken, db, cmd);
            return -1;
        }

#endif

        public override string GetExistsSql()
        {
            return "SELECT EXISTS (SELECT 1 FROM {0} WHERE {1})";
        }
    }
}