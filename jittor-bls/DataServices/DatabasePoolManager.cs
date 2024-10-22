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
        public DatabasePoolManager(int poolSize, List<string> connectionStrings, BlockingCollection<Database> dbPool)
        {
            _dbPool = dbPool;
            for (int j = 0; j < connectionStrings.Count; j++)   
            {
                for (int i = 0; i < poolSize; i++)
                {
                    var db = new Database(connectionStrings[j], "System.Data.SqlClient");
                    _dbPool.Add(db);
                }
            }
        }

        //public Database GetDatabase()
        //{
        //    //return _dbPool.Take(); // Get an available database instance
        //    if (_dbPool.TryTake(out var dbInstance))
        //    {
        //        return dbInstance; // Return the instance from the pool
        //    }

        //    // If the pool is exhausted, create a new connection on the spot
        //    return CreateNewDatabaseInstance(_connectionString);
        //}
        public Database GetDatabase(string connectionString)
        {
            foreach (var dbInstance in _dbPool)
            {
                if (dbInstance.ConnectionString == connectionString)
                {
                    if (_dbPool.TryTake(out var db))
                    {
                        return db; 
                    }
                }
            }

            return CreateNewDatabaseInstance(connectionString);
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
