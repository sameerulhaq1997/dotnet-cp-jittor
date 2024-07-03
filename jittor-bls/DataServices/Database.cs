using System;
using System.Collections.Generic;
using Jittor.App.Helpers;
using PetaPoco;

namespace Jittor.App.DataServices
{
    public partial class FrameworkRepository : Database
    {
        public ExecutionLogger ELHelperInstance { get; set; }
        public string LastExecutedSql { get; set; }
        public FrameworkRepository()
            : base("StockClassificationsConnectionString")
        {
            CommonConstruct();
        }
        public FrameworkRepository(string connectionStringName)
            : base(connectionStringName)
        {
            CommonConstruct();
        }
        public FrameworkRepository(string connectionString, string provider)
            : base(connectionString, provider)
        {
            CommonConstruct();
        }
        partial void CommonConstruct();
        public interface IFactory
        {
            FrameworkRepository GetInstance();
        }
        public static IFactory Factory { get; set; }
        public static FrameworkRepository GetInstance()
        {
            if (_instance != null)
                return _instance;

            if (Factory != null)
                return Factory.GetInstance();
            else
                return new FrameworkRepository();
        }

        [ThreadStatic]
        static FrameworkRepository _instance;
        public override void OnBeginTransaction()
        {
            if (_instance == null)
                _instance = this;
        }
        public override void OnEndTransaction()
        {
            if (_instance == this)
                _instance = null;
        }
        public class Record<T> where T : new()
        {
            public static FrameworkRepository repo { get { return FrameworkRepository.GetInstance(); } }
            public bool IsNew() { return repo.IsNew(this); }
            public object Insert() { return repo.Insert(this); }

            public void Save() { repo.Save(this); }
            public int Update() { return repo.Update(this); }

            public int Update(IEnumerable<string> columns) { return repo.Update(this, columns); }
            public static int Update(string sql, params object[] args) { return repo.Update<T>(sql, args); }
            public static int Update(Sql sql) { return repo.Update<T>(sql); }
            public int Delete() { return repo.Delete(this); }
            public static int Delete(string sql, params object[] args) { return repo.Delete<T>(sql, args); }
            public static int Delete(Sql sql) { return repo.Delete<T>(sql); }
            public static int Delete(object primaryKey) { return repo.Delete<T>(primaryKey); }
            public static bool Exists(object primaryKey) { return repo.Exists<T>(primaryKey); }
            public static bool Exists(string sql, params object[] args) { return repo.Exists<T>(sql, args); }
            public static T SingleOrDefault(object primaryKey) { return repo.SingleOrDefault<T>(primaryKey); }
            public static T SingleOrDefault(string sql, params object[] args) { return repo.SingleOrDefault<T>(sql, args); }
            public static T SingleOrDefault(Sql sql) { return repo.SingleOrDefault<T>(sql); }
            public static T FirstOrDefault(string sql, params object[] args) { return repo.FirstOrDefault<T>(sql, args); }
            public static T FirstOrDefault(Sql sql) { return repo.FirstOrDefault<T>(sql); }
            public static T Single(object primaryKey) { return repo.Single<T>(primaryKey); }
            public static T Single(string sql, params object[] args) { return repo.Single<T>(sql, args); }
            public static T Single(Sql sql) { return repo.Single<T>(sql); }
            public static T First(string sql, params object[] args) { return repo.First<T>(sql, args); }
            public static T First(Sql sql) { return repo.First<T>(sql); }
            public static List<T> Fetch(string sql, params object[] args) { return repo.Fetch<T>(sql, args); }
            public static List<T> Fetch(Sql sql) { return repo.Fetch<T>(sql); }
            public static List<T> Fetch(long page, long itemsPerPage, string sql, params object[] args) { return repo.Fetch<T>(page, itemsPerPage, sql, args); }
            public static List<T> Fetch(long page, long itemsPerPage, Sql sql) { return repo.Fetch<T>(page, itemsPerPage, sql); }
            public static List<T> SkipTake(long skip, long take, string sql, params object[] args) { return repo.SkipTake<T>(skip, take, sql, args); }
            public static List<T> SkipTake(long skip, long take, Sql sql) { return repo.SkipTake<T>(skip, take, sql); }
            public static Page<T> Page(long page, long itemsPerPage, string sql, params object[] args) { return repo.Page<T>(page, itemsPerPage, sql, args); }
            public static Page<T> Page(long page, long itemsPerPage, Sql sql) { return repo.Page<T>(page, itemsPerPage, sql); }
            public static IEnumerable<T> Query(string sql, params object[] args) { return repo.Query<T>(sql, args); }
            public static IEnumerable<T> Query(Sql sql) { return repo.Query<T>(sql); }

        }

        [TableName("JITAttributeTypes")]
        [PrimaryKey("AttributeTypeID")]
        [ExplicitColumns]
        public partial class JITAttributeType : FrameworkRepository.Record<JITAttributeType>
        {
            [Column] public int AttributeTypeID { get; set; }
            [Column] public string TypeName { get; set; }
            [Column] public int DBTypeID { get; set; }
            [Column] public string DotNetType { get; set; }
            [Column] public string DotNetAlias { get; set; }

            public object GetDefaultValue()
            {
                switch (this.DotNetAlias) 
                {
                    case "String": return string.Empty; 
                    case "Boolean": return false;
                    case "DateTime": return DateTime.Now; 
                    case "Byte": return new byte();  
                    case "Byte[]": return new byte[] { };  
                    case "DateTimeOffset": return DateTimeOffset.Now;
                    case "Decimal": return 0.0M;  
                    case "Double": return 0.0M;
                    case "Int16": return 0;
                    case "Int32": return 0;
                    case "Int64": return 0;
                    case "Object ": return new object();
                    case "Single": return 0;
                    case "TimeSpan": return new TimeSpan();
                    case "Xml": return string.Empty;
                    default: return null;
                }
            }
            public object GetDefaultValue(object value)
            {
                switch (this.DotNetAlias)
                {
                    case "String": return value.ToString();
                    case "Boolean": return bool.Parse(value.ToString());
                    case "DateTime": return DateTime.Parse(value.ToString());
                    case "Byte": return byte.Parse(value.ToString());
                    case "Byte[]": return (byte[])value;
                    case "DateTimeOffset": return (DateTimeOffset) value;
                    case "Decimal": return decimal.Parse(value.ToString());
                    case "Double": return double.Parse(value.ToString());
                    case "Int16": return Int16.Parse(value.ToString());
                    case "Int32": return Int32.Parse(value.ToString());
                    case "Int64": return Int64.Parse(value.ToString());
                    case "Object ": return value;
                    case "Single": return int.Parse(value.ToString());
                    case "TimeSpan": return TimeSpan.Parse(value.ToString());
                    case "Xml": return value.ToString();
                    default: return null;
                }
            }
        }
        [TableName("JITPageAttributes")]
        [PrimaryKey("PageAttributeID")]
        [ExplicitColumns]
        public partial class JITPageAttribute : FrameworkRepository.Record<JITPageAttribute>
        {

            [Column] public int PageAttributeID { get; set; }
            [Column] public string AttributeName { get; set; }
            [Column] public string DisplayNameAr { get; set; }
            [Column] public string DisplayNameEn { get; set; }
            [Column] public int PageID { get; set; }
            [Column] public int TableID { get; set; }
            [Column] public int AttributeTypeID { get; set; }
            [Column] public bool IsRequired { get; set; }
            [Column] public bool IsForeignKey { get; set; }
            [Column] public string ParentTableName { get; set; }
            [Column] public bool? AutoComplete { get; set; }
            [Column] public string ParentTableNameColumn { get; set; }
            [Column] public string ParentCondition { get; set; }
            [Column] public bool? AddNewParentRecord { get; set; }

            [Column] public string ValidationExpression { get; set; }
            [Column] public bool IsAutoIncreament { get; set; }
            [Column] public bool IsPrimaryKey { get; set; }
            [Column] public bool Editable { get; set; }
            [Column] public bool Searchable { get; set; }
            [Column] public bool Displayable { get; set; }
            [Column] public bool Sortable { get; set; }
            [Column] public bool Filterable { get; set; }
            [Column] public int EditableSeqNo { get; set; }
            [Column] public int SearchableSeqNo { get; set; }
            [Column] public int DisplayableSeqNo { get; set; }
            [Column] public int MaxLength { get; set; }
            [Column] public string PlaceholderText { get; set; }
            [Column] public string DisplayFormat { get; set; }
            [Column] public string InputPartialView { get; set; }
            [Column] public string DefaultValue { get; set; }
            [Column] public bool? IsRange { get; set; }
            [Column] public string Range { get; set; }
            [Column] public int DisplayGroupID { get; set; }
            [Column] public string DisplayStyle { get; set; }
            [Column] public bool? IsFile { get; set; } 
            [Column] public bool? AllowListInput { get; set; }
            [Column] public string UploadPath { get; set; }
            [Column] public string FileType { get; set; }
            [Column] public string PartialURLTemplate { get; set; } 
            [Column] public string AlternateValues { get; set; }
            public List<ForigenValue> ForigenValues { get; set; }
            [Column] public string AlternameValuesQuery { get; set; }

        }
        [TableName("JITPages")]
        [PrimaryKey("PageID")]
        [ExplicitColumns]
        public partial class JITPage : FrameworkRepository.Record<JITPage>
        {
            [Column] public int PageID { get; set; }
            [Column] public string PageName { get; set; }
            [Column] public string UrlFriendlyName { get; set; }
            [Column] public string Title { get; set; }
            [Column] public int GroupID { get; set; }
            [Column] public int RecordsPerPage { get; set; }
            [Column] public int CurrentPage { get; set; }
            [Column] public bool AddNew { get; set; }
            [Column] public bool EditRecord { get; set; }
            [Column] public bool DeleteRecord { get; set; }
            [Column] public string SoftDeleteColumn { get; set; }
            [Column] public bool Preview { get; set; }
            [Column] public bool ShowSearch { get; set; }
            [Column] public bool ShowFilters { get; set; }
            [Column] public bool ShowListing { get; set; }
            [Column] public string ListingPartialView { get; set; }
            [Column] public string ListingTitle { get; set; }
            [Column] public string OrderBy { get; set; }
            [Column] public string ConditionalClause { get; set; }
            [Column] public string Extender { get; set; }
            [Column] public string Description { get; set; }
            [Column] public string PageView { get; set; }
            [Column] public string ListingCommands { get; set; }
        }

        [TableName("JITPagesGroups")]
        [PrimaryKey("GroupID")]
        [ExplicitColumns]
        public partial class JITPagesGroup : FrameworkRepository.Record<JITPagesGroup>
        {
            [Column] public int GroupID { get; set; }
            [Column] public int? ParentGroupID { get; set; }
            [Column] public string GroupName { get; set; }
            [Column] public string Icon { get; set; }
        }
        [TableName("JITPageTables")]
        [PrimaryKey("TableID")]
        [ExplicitColumns]
        public partial class JITPageTable : FrameworkRepository.Record<JITPageTable>
        {
            [Column] public int TableID { get; set; }
            [Column] public string TableName { get; set; }
            [Column] public string TableAlias { get; set; }
            [Column] public int PageID { get; set; }
            [Column] public bool ForView { get; set; }
            [Column] public bool ForOperation { get; set; }
            public string SelectColumns { get; set; }
            public string Filters { get; internal set; }
            public string Orders { get; internal set; }
            public string Joins { get; internal set; }
        }

        [TableName("JITAttributeDisplayGroups")]
        [PrimaryKey("DisplayGroupID")]
        [ExplicitColumns]
        public partial class JITAttributeDisplayGroup : FrameworkRepository.Record<JITAttributeDisplayGroup>
        {

            [Column] public int DisplayGroupID { get; set; }
            [Column] public string GroupName { get; set; }
            [Column] public string Title { get; set; }
            [Column] public int DisplayGroupTypeID { get; set; }
            [Column] public int DisplaySeqNo { get; set; }
            [Column] public bool EnableDefaultValues { get; set; }
        }

        [TableName("JITDisplayGroupTypes")]
        [PrimaryKey("DisplayGroupTypeID")]
        [ExplicitColumns]
        public partial class JITDisplayGroupType : FrameworkRepository.Record<JITDisplayGroupType>
        {
            [Column] public int DisplayGroupTypeID { get; set; }
            [Column] public string TypeName { get; set; }
            [Column] public string CssClass { get; set; }
        }

        [TableName("dbo.EventLogs")]
        [PrimaryKey("EventLogId")]
        [ExplicitColumns]
        public partial class EventLogs : FrameworkRepository.Record<EventLogs>
        {

            [Column] public int EventLogId { get; set; }
            [Column] public string? EntityType { get; set; }
            [Column] public int RowID { get; set; }
            [Column] public string? OperationType { get; set; }
            [Column] public string? LogMessage { get; set; }
            [Column] public int UserId { get; set; }
            [Column] public byte[] OldRecord { get; set; }

            [Column] public byte[] Changes { get; set; }
            [Column] public DateTime CreatedOn { get; set; }

        }

        [TableName("AppConfigs")]


        [PrimaryKey("AppConfigKey", AutoIncrement = false)]

        [ExplicitColumns]
        public partial class AppConfig : FrameworkRepository.Record<AppConfig>
        {


            [Column] public string AppConfigKey { get; set; }

            [Column] public string AppConfigValue { get; set; }

            [Column] public bool IsEditableByUser { get; set; }

            [Column] public string Description { get; set; }
        }


        [TableName("AppExceptionAdditionalData")]


        [ExplicitColumns]
        public partial class AppExceptionAdditionalDatum : FrameworkRepository.Record<AppExceptionAdditionalDatum>
        {


            [Column] public int AppExceptionID { get; set; }

            [Column] public string Variable { get; set; }

            [Column] public string Value { get; set; }
        }


        [TableName("AppExceptions")]


        [PrimaryKey("AppExceptionID")]



        [ExplicitColumns]
        public partial class AppException : FrameworkRepository.Record<AppException>
        {


            [Column] public int AppExceptionID { get; set; }

            [Column] public string SourceApp { get; set; }

            [Column] public DateTime OccuredOn { get; set; }

            [Column] public string Message { get; set; }

            [Column] public string OriginatedAt { get; set; }

            [Column] public string StackTrace { get; set; }

            [Column] public string InnerExceptionMessage { get; set; }

            [Column] public string HostMachine { get; set; }

            [Column] public string Level { get; set; }

            [Column] public string CustomMessage { get; set; }
        }
        public partial class ForigenValue : FrameworkRepository.Record<ForigenValue>
        {
            [Column] public int ID { get; set; }
            [Column] public string Name { get; set; }

            [Column]
            public string Value
            {
                get; set;

            }
        }
    }
}