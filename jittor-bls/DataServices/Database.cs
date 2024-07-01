using System;
using System.Collections.Generic;
using MacroEconomics.Shared.DataServices;
using MacroEconomics.Shared.Helpers;
using PetaPoco;

namespace MacroEconomics.Shared.DataServices
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
        [TableName("ActionLogs")]


        [PrimaryKey("ActionID")]



        [ExplicitColumns]
        public partial class ActionLog : FrameworkRepository.Record<ActionLog>
        {


            [Column] public int ActionID { get; set; }

            [Column] public short? EntityID { get; set; }

            [Column] public int ByUserID { get; set; }

            [Column] public byte[] OldRecord { get; set; }

            [Column] public byte[] Changes { get; set; }

            [Column] public DateTime CreatedOn { get; set; }

            [Column] public short ActionTypeID { get; set; }

            [Column] public int RowID { get; set; }
        }


        [TableName("ActionTypes")]


        [PrimaryKey("ActionTypeID", AutoIncrement = false)]

        [ExplicitColumns]
        public partial class ActionType : FrameworkRepository.Record<ActionType>
        {


            [Column] public short ActionTypeID { get; set; }

            [Column] public string ActionTypeName { get; set; }
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


        [TableName("AppSqlLogs")]


        [ExplicitColumns]
        public partial class AppSqlLog : FrameworkRepository.Record<AppSqlLog>
        {


            [Column] public string ExecutingSql { get; set; }

            [Column] public DateTime ExecutedOn { get; set; }

            [Column] public string HostMachine { get; set; }
        }


        [TableName("AttributeDataTypes")]


        [PrimaryKey("AttributeDataTypeID", AutoIncrement = false)]

        [ExplicitColumns]
        public partial class AttributeDataType : FrameworkRepository.Record<AttributeDataType>
        {


            [Column] public int AttributeDataTypeID { get; set; }

            [Column] public string Name { get; set; }
        }


        [TableName("Attributes")]


        [PrimaryKey("AttributeID")]



        [ExplicitColumns]
        public partial class Attribute : FrameworkRepository.Record<Attribute>
        {


            [Column] public int AttributeID { get; set; }

            [Column] public string AttributeName { get; set; }

            [Column] public int AttributeDataTypeID { get; set; }

            [Column] public string DisplayNameEn { get; set; }

            [Column] public string DisplayNameAr { get; set; }

            [Column] public string DescriptionEn { get; set; }

            [Column] public string DescriptionAr { get; set; }

            [Column] public string Formula { get; set; }

            [Column] public string FormulaExpression { get; set; }

            [Column] public bool IsComputed { get; set; }

            [Column] public int? MeasuringUnitID { get; set; }

            [Column] public int? CurrencyID { get; set; }

            [Column] public bool IsSystemAttribute { get; set; }

            [Column] public bool IsAccumulative { get; set; }

            [Column] public int CreatedBy { get; set; }

            [Column] public DateTime CreatedOn { get; set; }

            [Column] public int? LastModifiedBy { get; set; }

            [Column] public DateTime? LastModifiedOn { get; set; }

            [Column] public string UnitAr { get; set; }

            [Column] public string UnitEn { get; set; }

            [Column] public bool ApplyFormulaOnTotal { get; set; }
        }


        [TableName("Countries")]


        [PrimaryKey("CountryID")]



        [ExplicitColumns]
        public partial class Country : FrameworkRepository.Record<Country>
        {


            [Column] public int CountryID { get; set; }

            [Column] public string NameEn { get; set; }

            [Column] public string NameAr { get; set; }

            [Column] public string CountryISOCode { get; set; }

            [Column] public string FlagUrl { get; set; }

            [Column] public bool IsActive { get; set; }
        }


        [TableName("Currencies")]


        [PrimaryKey("CurrencyID")]



        [ExplicitColumns]
        public partial class Currency : FrameworkRepository.Record<Currency>
        {


            [Column] public int CurrencyID { get; set; }

            [Column] public string NameEn { get; set; }

            [Column] public string NameAr { get; set; }

            [Column] public string CurrencyCode { get; set; }

            [Column] public bool IsActive { get; set; }
        }


        [TableName("Entities")]


        [PrimaryKey("EntityID")]



        [ExplicitColumns]
        public partial class Entity : FrameworkRepository.Record<Entity>
        {


            [Column] public int EntityID { get; set; }

            [Column] public string Name { get; set; }

            [Column] public bool IsActive { get; set; }
        }


        [TableName("EntityAttributes")]


        [PrimaryKey("EntityAttributeID")]



        [ExplicitColumns]
        public partial class EntityAttribute : FrameworkRepository.Record<EntityAttribute>
        {


            [Column] public int EntityAttributeID { get; set; }

            [Column] public int EntityID { get; set; }

            [Column] public int RowID { get; set; }

            [Column] public int AttributeID { get; set; }

            [Column] public bool IsDeleted { get; set; }

            [Column] public short DisplaySeqNo { get; set; }

            [Column] public int CreatedBy { get; set; }

            [Column] public DateTime CreatedOn { get; set; }

            [Column] public int? ModifiedBy { get; set; }

            [Column] public DateTime? ModifiedOn { get; set; }
        }


        [TableName("EntityAttributeValues")]


        [PrimaryKey("EntityAttributeValueID")]



        [ExplicitColumns]
        public partial class EntityAttributeValue : FrameworkRepository.Record<EntityAttributeValue>
        {


            [Column] public int EntityAttributeValueID { get; set; }

            [Column] public int StockExchangeID { get; set; }

            [Column] public int EntityID { get; set; }

            [Column] public int RowID { get; set; }

            [Column] public int AttributeID { get; set; }

            [Column] public string ValueAr { get; set; }

            [Column] public string ValueEn { get; set; }

            [Column] public decimal? ValueNumeric { get; set; }

            [Column] public int? ForYear { get; set; }

            [Column] public int? FiscalPeriodID { get; set; }

            [Column] public int CreatedBy { get; set; }

            [Column] public DateTime CreatedOn { get; set; }

            [Column] public int? ModifiedBy { get; set; }

            [Column] public DateTime? ModifiedOn { get; set; }

            [Column] public string NoteAr { get; set; }

            [Column] public string NoteEn { get; set; }
        }


        [TableName("FiscalPeriods")]


        [PrimaryKey("FiscalPeriodID", AutoIncrement = false)]

        [ExplicitColumns]
        public partial class FiscalPeriod : FrameworkRepository.Record<FiscalPeriod>
        {


            [Column] public int FiscalPeriodID { get; set; }

            [Column] public int FiscalPeriodTypeID { get; set; }

            [Column] public string FiscalPeriodValue { get; set; }
        }


        [TableName("FiscalPeriodTypes")]


        [PrimaryKey("FiscalPeriodTypeID", AutoIncrement = false)]

        [ExplicitColumns]
        public partial class FiscalPeriodType : FrameworkRepository.Record<FiscalPeriodType>
        {


            [Column] public int FiscalPeriodTypeID { get; set; }

            [Column] public string FiscalPeriodTypeName { get; set; }
        }


        [TableName("Languages")]


        [PrimaryKey("LanguageID", AutoIncrement = false)]

        [ExplicitColumns]
        public partial class Language : FrameworkRepository.Record<Language>
        {


            [Column] public int LanguageID { get; set; }

            [Column] public string LanguageName { get; set; }

            [Column] public string LanguageShortName { get; set; }
        }


        [TableName("MeasuringUnits")]


        [PrimaryKey("MeasuringUnitID")]



        [ExplicitColumns]
        public partial class MeasuringUnit : FrameworkRepository.Record<MeasuringUnit>
        {


            [Column] public int MeasuringUnitID { get; set; }

            [Column] public string MeasuringUnitNameAr { get; set; }

            [Column] public string MeasuringUnitNameEn { get; set; }

            [Column] public int? MeasuringUnitGroupID { get; set; }
        }


        [TableName("ResourceLocalizations")]


        [PrimaryKey("ResourceID")]



        [ExplicitColumns]
        public partial class ResourceLocalization : FrameworkRepository.Record<ResourceLocalization>
        {


            [Column] public int ResourceID { get; set; }

            [Column] public int LanguageID { get; set; }

            [Column] public string ResourceKey { get; set; }

            [Column] public string ResourceValue { get; set; }

            [Column] public string Description { get; set; }
        }


        [TableName("StockClassifications")]


        [PrimaryKey("StockClassificationID")]



        [ExplicitColumns]
        public partial class StockClassification : FrameworkRepository.Record<StockClassification>
        {


            [Column] public int StockClassificationID { get; set; }

            [Column] public string NameEn { get; set; }

            [Column] public string NameAr { get; set; }

            [Column] public bool IsActive { get; set; }

            [Column] public bool IsDeleted { get; set; }

            [Column] public DateTime? CreatedOn { get; set; }

            [Column] public int? CreatedBy { get; set; }

            [Column] public DateTime? ModifiedOn { get; set; }

            [Column] public int? ModifiedBy { get; set; }
            
        }


        [TableName("StockExchanges")]


        [PrimaryKey("StockExchangeID")]



        [ExplicitColumns]
        public partial class StockExchange : FrameworkRepository.Record<StockExchange>
        {


            [Column] public int StockExchangeID { get; set; }

            [Column] public string NameEn { get; set; }

            [Column] public string NameAr { get; set; }

            [Column] public int? CountryID { get; set; }

            [Column] public string LogoUrl { get; set; }

            [Column] public bool IsActive { get; set; }

            [Column] public bool IsDeleted { get; set; }

            [Column] public DateTime? CreatedOn { get; set; }

            [Column] public int? CreatedBy { get; set; }

            [Column] public DateTime? ModifiedOn { get; set; }

            [Column] public int? ModifiedBy { get; set; }
        }


        [TableName("StockSymbols")]


        [PrimaryKey("StockSymbolID")]



        [ExplicitColumns]
        public partial class StockSymbol : FrameworkRepository.Record<StockSymbol>
        {


            [Column] public int StockSymbolID { get; set; }

            [Column] public int StockTypeID { get; set; }

            [Column] public int StockExchangeID { get; set; }

            [Column] public string Symbol { get; set; }

            [Column] public string FullNameEn { get; set; }

            [Column] public string FullNameAr { get; set; }

            [Column] public string ShortNameEn { get; set; }

            [Column] public string ShortNameAr { get; set; }

            [Column] public string LogoUrl { get; set; }

            [Column] public string DescriptionEn { get; set; }

            [Column] public string DescriptionAr { get; set; }

            [Column] public bool IsActive { get; set; }

            [Column] public bool IsDeleted { get; set; }

            [Column] public bool IsSuspended { get; set; }

            [Column] public DateTime? CreatedOn { get; set; }

            [Column] public int? CreatedBy { get; set; }

            [Column] public DateTime? ModifiedOn { get; set; }

            [Column] public int? ModifiedBy { get; set; }
        }


        [TableName("StockTypes")]


        [PrimaryKey("StockTypeID", AutoIncrement = false)]

        [ExplicitColumns]
        public partial class StockType : FrameworkRepository.Record<StockType>
        {


            [Column] public int StockTypeID { get; set; }

            [Column] public string NameEn { get; set; }

            [Column] public string NameAr { get; set; }

            [Column] public bool IsActive { get; set; }

            [Column] public bool IsDeleted { get; set; }

            [Column] public DateTime? CreatedOn { get; set; }

            [Column] public int? CreatedBy { get; set; }

            [Column] public DateTime? ModifiedOn { get; set; }

            [Column] public int? ModifiedBy { get; set; }
        }


        [TableName("UserEntityValues")]


        [PrimaryKey("UserEntityValueID", AutoIncrement = false)]

        [ExplicitColumns]
        public partial class UserEntityValue : FrameworkRepository.Record<UserEntityValue>
        {


            [Column] public int UserEntityValueID { get; set; }

            [Column] public int UserTemplateID { get; set; }

            [Column] public int EntityAttributeValueID { get; set; }
        }


        [TableName("UserTemplates")]


        [PrimaryKey("UserTemplateID")]



        [ExplicitColumns]
        public partial class UserTemplate : FrameworkRepository.Record<UserTemplate>
        {


            [Column] public int UserTemplateID { get; set; }

            [Column] public string Name { get; set; }

            [Column] public bool IsDeleted { get; set; }

            [Column] public bool IsAllowedToShare { get; set; }

            [Column] public DateTime? CreatedOn { get; set; }

            [Column] public int? CreatedBy { get; set; }

            [Column] public DateTime? ModifiedOn { get; set; }

            [Column] public int? ModifiedBy { get; set; }
        }


        [TableName("sysdiagrams")]


        [PrimaryKey("diagram_id")]



        [ExplicitColumns]
        public partial class sysdiagram : FrameworkRepository.Record<sysdiagram>
        {


            [Column] public string name { get; set; }

            [Column] public int principal_id { get; set; }

            [Column] public int diagram_id { get; set; }

            [Column] public int? version { get; set; }

            [Column] public byte[] definition { get; set; }
        }


        [TableName("UserStockSymbolClassifications")]


        [PrimaryKey("ID", AutoIncrement = false)]

        [ExplicitColumns]
        public partial class UserStockSymbolClassification : FrameworkRepository.Record<UserStockSymbolClassification>
        {


            [Column] public int ID { get; set; }

            [Column] public int StockSymbolID { get; set; }

            [Column] public int StockClassificationID { get; set; }

            [Column] public bool IsDeleted { get; set; }

            [Column] public DateTime CreatedOn { get; set; }

            [Column] public int CreatedBy { get; set; }

            [Column] public DateTime? ModifiedOn { get; set; }

            [Column] public int? ModifiedBy { get; set; }
            [Column] public bool IsDefault { get; set; }
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

        


        [TableName("CPMenus")]
        [PrimaryKey("CPMenuID")]
        [ExplicitColumns]
        public partial class CPMenu : FrameworkRepository.Record<CPMenu>
        {
            [Column] public int CPMenuID { get; set; }
            [Column] public string MenuName { get; set; }
            [Column] public string DisplayText { get; set; }
            [Column] public string AdditionalText { get; set; }
            [Column] public string LinkUrl { get; set; }
            [Column] public string IconClass { get; set; }
            [Column] public bool IsVisible { get; set; }
            [Column] public int SequenceNo { get; set; }
            [Column] public int? ParentsID { get; set; }
            [Column] public string SVGFile { get; set; }

        }

        [TableName("dbo.MacroCategories")]
        [PrimaryKey("CategoryId")]
        [ExplicitColumns]
        public partial class MacroCategories : FrameworkRepository.Record<MacroCategories>
        {

            [Column] public int CategoryId { get; set; }
            [Column] public string TextNameEn { get; set; }
            [Column] public string TextNameAr { get; set; }
         
            [Column] public string? Title { get; set; }

            [Column] public decimal? DisplaySeqNo { get; set; }
            [Column] public bool IsActive { get; set; }

            [Column] public bool ShowInFooter { get; set; }
            [Column] public DateTime CreatedOn { get; set; }
            [Column] public int CreatedBy { get; set; }
            [Column] public DateTime? ModifiedOn { get; set; }
            [Column] public int? ModifiedBy { get; set; }
        }


        [TableName("dbo.MacroCategoryPages")]
        [PrimaryKey("PageId")]
        [ExplicitColumns]
        public partial class MacroCategoryPages : FrameworkRepository.Record<MacroCategoryPages>
        {

            [Column] public int PageId { get; set; }

            [Column] public int CategoryId { get; set; }
            [Column] public string TextNameEn { get; set; }
            [Column] public string TextNameAr { get; set; }

            [Column] public string? Title { get; set; }

            [Column] public decimal? DisplaySeqNo { get; set; }
            [Column] public bool IsActive { get; set; }

            [Column] public bool ShowInFooter { get; set; }
            [Column] public DateTime CreatedOn { get; set; }
            [Column] public int CreatedBy { get; set; }
            [Column] public DateTime? ModifiedOn { get; set; }
            [Column] public int? ModifiedBy { get; set; }
        }

        [TableName("dbo.Charts")]
        [PrimaryKey("ChartId")]
        [ExplicitColumns]
        public partial class Charts : FrameworkRepository.Record<Charts>
        {
            [Column] public int ChartId { get; set; }
            [Column] public string? Title { get; set; }
            [Column] public string? NameEn { get; set; }
            [Column] public string? NameAr { get; set; }
            [Column] public string? DataSource { get; set; }
            [Column] public string? Configuration { get; set; }
            [Column] public string? DataSourceType { get; set; }
            [Column] public bool IsActive { get; set; }
            [Column] public DateTime CreatedOn { get; set; }
            [Column] public int CreatedBy { get; set; }
            [Column] public DateTime? ModifiedOn { get; set; }
            [Column] public int? ModifiedBy { get; set; }

            [Column] public int? ChartTypeId { get; set; }
            [Column] public int? SectionId { get; set; }
            [Column] public decimal? DisplaySeqNo { get; set; }
            [Column] public int? MeasuringUnits { get; set; }

            [Column] public string? mappedFieldId { get; set; }
            [Column] public string? SourceEn { get; set; }
            [Column] public string? SourceAr { get; set; }

            [Column] public string? UniqueCode { get; set; }

            [Column] public string? ToolsOptions { get; set; }


        }

        [TableName("dbo.SectionCharts")]
        [PrimaryKey("SectionChartId")]
        [ExplicitColumns]
        public partial class SectionCharts : FrameworkRepository.Record<SectionCharts>
        {
          
            [Column] public int SectionChartId { get; set; }
            [Column] public int SectionId { get; set; }
            [Column] public int ChartId { get; set; }
            [Column] public int ChartTypeId { get; set; }
            [Column] public string? ChartSectionConfiguration { get; set; }

            [Column] public decimal? DisplaySeqNo { get; set; }
            [Column] public bool IsActive { get; set; }
        }
        [TableName("dbo.Sections")]
        [PrimaryKey("SectionId")]
        [ExplicitColumns]
        public partial class Sections : FrameworkRepository.Record<Sections>
        {

            [Column] public int SectionId { get; set; }
            [Column] public int PageId { get; set; }
            [Column] public string? Title { get; set; }
            [Column] public decimal? DisplaySeqNo { get; set; }
            [Column] public string? Configuration { get; set; }
            [Column] public bool IsActive { get; set; }
            [Column] public DateTime CreatedOn { get; set; }
            [Column] public int CreatedBy { get; set; }
            [Column] public DateTime? ModifiedOn { get; set; }
            [Column] public int? ModifiedBy { get; set; }

            [Column] public string? TextNameEn { get; set; }

            [Column] public string? TextNameAr { get; set; }
        }

        [TableName("dbo.ChartTypes")]
        [PrimaryKey("ChartTypeId")]
        [ExplicitColumns]
        public partial class ChartTypes : FrameworkRepository.Record<ChartTypes>
        {
            [Column] public int ChartTypeId { get; set; }
            [Column] public string? Name { get; set; }
            [Column] public bool IsActive { get; set; }
            [Column] public DateTime CreatedOn { get; set; }
            [Column] public int CreatedBy { get; set; }
            [Column] public DateTime? ModifiedOn { get; set; }
            [Column] public int? ModifiedBy { get; set; }
        }


        [TableName("dbo.EconomicIndicatorGroups")]
        [PrimaryKey("GroupID")]
        [ExplicitColumns]
        public partial class EconomicIndicatorGroups : FrameworkRepository.Record<EconomicIndicatorGroups>
        {

            [Column] public int GroupID { get; set; }
            [Column] public string? NameEn { get; set; }
            [Column] public string? NameAr { get; set; }
            [Column] public int? ParentGroupID { get; set; }
            [Column] public decimal DisplaySeqNo { get; set; }
        }

        [TableName("dbo.EconomicIndicatorValues")]
        [PrimaryKey("EconomicIndicatorValueID")]
        [ExplicitColumns]
        public partial class EconomicIndicatorValues : FrameworkRepository.Record<EconomicIndicatorValues>
        {

            [Column] public int EconomicIndicatorValueID { get; set; }
            [Column] public int EconomicIndicatorID { get; set; }
            [Column] public int EconomicIndicatorFieldID { get; set; }
            [Column] public string ValueEn { get; set; }
            [Column] public string ValueAr { get; set; }
            [Column] public string? NoteEn { get; set; }
            [Column] public string? NoteAr { get; set; }
        }

        [TableName("dbo.EconomicIndicatorFields")]
        [PrimaryKey("EconomicIndicatorFieldID")]
        [ExplicitColumns]
        public partial class EconomicIndicatorFields : FrameworkRepository.Record<EconomicIndicatorFields>
        {

            [Column] public int EconomicIndicatorFieldID { get; set; }
     
            [Column] public string DisplayNameEn { get; set; }
            [Column] public string DisplayNameAr { get; set; }

            [Column] public int DisplaySeqNo { get; set; }
            [Column] public int? MeasuringUnitID { get; set; }
            [Column] public int GroupID { get; set; }

            [Column] public bool IsChart { get; set; }
        }


        [TableName("dbo.EconomicIndicators")]
        [PrimaryKey("EconomicIndicatorID")]
        [ExplicitColumns]
        public partial class EconomicIndicators : FrameworkRepository.Record<EconomicIndicators>
        {

            [Column] public int EconomicIndicatorID { get; set; }

            [Column] public int? CountryID { get; set; }

            
            [Column] public DateTime UpdatedOn { get; set; }


            [Column] public bool IsPublished { get; set; }
            [Column] public int ForYear { get; set; }

            [Column] public int FiscalPeriodID { get; set; }
            [Column] public int? SubGroupID { get; set; }

        }


        [TableName("dbo.EconomicIndicatorSources")]
        [PrimaryKey("EconomicIndicatorSourceID")]
        [ExplicitColumns]
        public partial class EconomicIndicatorSources : FrameworkRepository.Record<EconomicIndicatorSources>
        {

            [Column] public int EconomicIndicatorSourceID { get; set; }

            [Column] public int CountryID { get; set; }
            [Column] public string EISourceNameEn { get; set; }
            [Column] public string EISourceNameAr { get; set; }

        }

        [TableName("dbo.MacroChartEntities")]
        [PrimaryKey("EntityId")]
        [ExplicitColumns]
        public partial class MacroChartEntities : FrameworkRepository.Record<MacroChartEntities>
        {
            [Column] public int EntityId { get; set; }
            [Column] public string? EntityNameEn { get; set; }
            [Column] public string? EntityNameAr { get; set; }
            [Column] public string? EntityType { get; set; }
            [Column] public int? RowId { get; set; }

            [Column] public int? ParentId { get; set; }
        }

        [TableName("dbo.FormulaFieldConfig")]
        [PrimaryKey("FieldConfigId")]
        [ExplicitColumns]
        public partial class FormulaFieldConfig : FrameworkRepository.Record<FormulaFieldConfig>
        {
            [Column] public int FieldConfigId { get; set; }
            [Column] public string? FieldNameEn { get; set; }
            [Column] public string? FieldNameAr { get; set; }
            [Column] public string? Formula { get; set; }
            [Column] public int? UnitId { get; set; }

            [Column] public string? DataSource { get; set; }
            [Column] public string? Description { get; set; }
            [Column] public string? FormulaFieldIDs { get; set; }

            [Column] public string? ConstantValues { get; set; }

            [Column] public string? FormulaType { get; set; }
        }


        [TableName("dbo.MacroChartIcons")]
        [PrimaryKey("IconId")]
        [ExplicitColumns]
        public partial class MacroChartIcons : FrameworkRepository.Record<MacroChartIcons>
        {

            [Column] public int IconId { get; set; }
            [Column] public string? NameEn { get; set; }
            [Column] public string? ClassName { get; set; }

            [Column] public string? CssStyling { get; set; }
            [Column] public string? IconUrl { get; set; }
            [Column] public string? IconUrlHover { get; set; }
            [Column] public bool IsActive { get; set; }
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