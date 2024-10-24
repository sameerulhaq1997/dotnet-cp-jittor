using Microsoft.Extensions.Configuration;
using PetaPoco;
using PetaPoco.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jittor.App.DataServices
{
    public class DatabasePoolManager
    {
        private readonly BlockingCollection<Database> _dbPool;
        Dictionary<string, string> _connectionStrings;
        public DatabasePoolManager(int poolSize, Dictionary<string, string> connectionStrings, BlockingCollection<Database> dbPool)
        {
            _connectionStrings = connectionStrings;
            _dbPool = dbPool;
            for (int j = 0; j < connectionStrings.Count; j++)   
            {
                for (int i = 0; i < poolSize; i++)
                {
                    var db = new Database(connectionStrings.ElementAt(j).Value, "System.Data.SqlClient");
                    _dbPool.Add(db);
                }
            }
        }

        //public Database GetDatabase(string connectionString)
        //{
        //    foreach (var dbInstance in _dbPool)
        //    {
        //        if (dbInstance.ConnectionString == connectionString)
        //        {
        //            //var pool = _dbPool.Where(x => x.ConnectionString == connectionString).FirstOrDefault();
        //            if (_dbPool.TryTake(out var db))
        //            {
        //                return db; 
        //            }
        //        }
        //    }

        //    return CreateNewDatabaseInstance(connectionString);
        //}

        public Database GetDatabase(string name)
        {
            return CreateNewDatabaseInstance(_connectionStrings[name]);
        }
        public void ReleaseDatabase(Database db)
        {
            //_dbPool.Add(db); // Return the instance back to the pool

            if (!_dbPool.TryAdd(db))
            {
                db.Dispose(); 
            }
        }
        private Database CreateNewDatabaseInstance(string connectionString)
        {
            return new Database(connectionString, "System.Data.SqlClient");
            //return new FrameworkRepository(connectionString)
            //{
            //    EnableAutoSelect = true
            //};
        }
    }
}
