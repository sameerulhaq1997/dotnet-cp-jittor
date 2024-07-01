using MacroEconomics.Shared;
using MacroEconomics.Shared.DataServices.CustomEntities;
using PetaPoco;
using PetaPoco.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static MacroEconomics.Shared.DataServices.FrameworkRepository;
using MacroEconomics.Shared.Enums;

namespace PetaPoco
{
    public partial class Database : IDisposable 
    {
        #region insert with logs
        public object Insert(object poco, int userId)
        {
            
            var p = this.Insert(poco);
            
            PocoData pd = PocoData.ForType(poco.GetType(), _defaultMapper);
            EventLogEntity eventLogEntity = new EventLogEntity();
            
            eventLogEntity.UserId = userId;
            eventLogEntity.OperationType = ActionTypeEnum.Insert.ToString();
            eventLogEntity.RowID = int.Parse(p.ToString());
            eventLogEntity.EntityType = pd.TableInfo.TableName;
            eventLogEntity.OldRecordEntity =  Activator.CreateInstance(poco.GetType());
            eventLogEntity.ChangesEntity = poco;
            eventLogEntity.LogMessage = "Data inserted";
            
            this.LogEvents(eventLogEntity); 

            return p;
        }
        public int Execute(int userId, string tableName, string operation,string selectSql, string sql, params object[] args)
        {
            EventLogEntity eventLogEntity = new EventLogEntity();

            eventLogEntity.UserId = userId;
            eventLogEntity.OperationType = operation;
           
            eventLogEntity.EntityType = tableName;
            eventLogEntity.OldRecordEntity = this.Fetch<dynamic>(selectSql, args).ToList();
            eventLogEntity.ChangesEntity = args;
            eventLogEntity.LogMessage = operation;

            int id = ExecuteInternal(CommandType.Text, sql, args);
            eventLogEntity.RowID = id;
            this.LogEvents(eventLogEntity);
            return id;
        }
        
        #endregion

        #region update with logs
        public int Update(object poco, int userId)
        {
            PocoData pd = PocoData.ForType(poco.GetType(), _defaultMapper);
            string tableName = pd.TableInfo.TableName;
            string primaryKeyName = pd.TableInfo.PrimaryKey;
            EventLogEntity entity = new EventLogEntity();
            entity.OldRecordEntity = this.Fetch<object>(string.Format("Select * From {0} Where {1} = @0", tableName, primaryKeyName), poco.GetType().GetProperty(primaryKeyName).GetValue(poco, null)).FirstOrDefault();
            entity.ChangesEntity = poco;
            entity.RowID = int.Parse(poco.GetType().GetProperty(primaryKeyName).GetValue(poco, null).ToString());
            entity.UserId = userId;
            entity.LogMessage = "updating record";
            entity.OperationType = ActionTypeEnum.Update.ToString();
            entity.EntityType = pd.TableInfo.TableName;
            this.LogEvents(entity);

            return this.Update(poco);

        }
        #endregion

        #region delete with logs
        public int Delete(object poco, int userId)
        {
            PocoData pd = PocoData.ForType(poco.GetType(), _defaultMapper);
            string tableName = pd.TableInfo.TableName;
            string primaryKeyName = pd.TableInfo.PrimaryKey;
            
            EventLogEntity entity = new EventLogEntity();
            
            entity.OldRecordEntity = this.Fetch<object>(string.Format("Select * From {0} Where {1} = @0", tableName, primaryKeyName), poco.GetType().GetProperty(primaryKeyName).GetValue(poco, null)).FirstOrDefault();
            entity.ChangesEntity = poco;
            entity.RowID = int.Parse(poco.GetType().GetProperty(primaryKeyName).GetValue(poco, null).ToString());
            entity.UserId = userId;
            entity.LogMessage = "deleting record";
            entity.OperationType = ActionTypeEnum.Delete.ToString();
            entity.EntityType = pd.TableInfo.TableName;
            this.LogEvents(entity); 
            return this.Delete(poco);

        }
        #endregion
        #region event log
        public void LogEvents(EventLogEntity entity)
        {

            EventLogs ent = (EventLogs)entity;
            ent.CreatedOn = DateTime.Now;
            if (entity.OldRecordEntity != null)
            {
                ent.OldRecord = JsonSerializer.Serialize(entity.OldRecordEntity).Zip(); 
            }
            if (entity.ChangesEntity != null)
            {
                ent.Changes = JsonSerializer.Serialize(entity.ChangesEntity).Zip();
            }
            this.Insert(ent);
        }
        #endregion
    }
}
