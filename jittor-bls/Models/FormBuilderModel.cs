using Jittor.App.Models;

namespace Jittor.App.Models
{
    public enum ApplicationFieldTypeEnum
    {
        INPUT = 1,
        TEXT_AREA = 2,
        SELECT = 3,
        AUTO_COMPLETE = 4,
        RADIO_BUTTON = 5,
        SWITCH = 6,
        CHECKBOX = 7,
        DATE_TIME = 8,
        SELECTABLE_LIST = 9,
        TREEVIEW_LIST = 10,
        ADD_REMOVE_SEARCH_TABLE = 11
    }
    public enum ApplicationFieldSubTypeEnum
    {
        NONE = 1,
        SINGLE = 2,
        MULTI = 3,
        TEXT = 4,
        PASSWORD = 5,
        NUMBER = 6,
        DATE = 7,
        TIME = 8,
        DATE_TIME = 9
    }
    public enum ApplicationValueTypeEnum
    {
        STRING = 1,
        NUMBER = 2,
        BOOL = 3,
        OBJECT = 4,
        ARRAY = 5
    }
 
    public class FieldModel
    {
        public string CssID { get; set; }
        public string CssClasses { get; set; }
        public string HelperText { get; set; }
        public string PlaceholderEn { get; set; }
        public string PlaceholderAr { get; set; }
        public string Id { get; set; }
        public bool IsDisabled { get; set; }
        public bool IsVisible { get; set; }
        public ApplicationFieldTypeEnum FieldType { get; set; }
        public ApplicationFieldSubTypeEnum FieldSubType { get; set; }
        public FieldValue InpValue { get; set; }
        public List<FieldOption> Options { get; set; }
        public Dictionary<string, object> Validations { get; set; }
        public List<FieldAction> Actions { get; set; }
        public string TableName { get; set; }
        public string LabelAr { get; set; }
        public string LabelEn { get; set; }
        public int? AttributeTypeId { get; set; }
        public string ParentTableName { get; set; }
        public string ParentTableNameColumn { get; set; }
        public string ParentCondition { get; set; }
        public int DisplayableSeqNo { get; set; }
        public int SearchableSeqNo { get; set; }
        public string OptionIdColumn { get; set; }
        public string OptionLabelColumn { get; set; }
        public string OptionTable { get; set; }
        public JittorColumnInfo? CurrentColumn { get; set; }
        public int? PageId { get; set; }
        public int? TableId { get; set; }

        public FieldModel()
        {
            Options = new List<FieldOption>();
            Validations = new Dictionary<string, object>();
            Actions = new List<FieldAction>();
        }
    }
    public class FieldOption
    {
        public int Value { get; set; }
        public string Label { get; set; }
        public bool IsDisabled { get; set; }
    }
    public class FieldValue
    {
        public dynamic ActualValue { get; set; }
        public ApplicationValueTypeEnum ValueType { get; set; }
    }
    public class FieldAction
    {
        public int ApplyOn { get; set; }
        public string TargetSection { get; set; }
        public string TargetInput { get; set; }
        public int ActionType { get; set; }
        public int RunOn { get; set; }
    }
    public class FormPageModel
    {
        public Form Form { get; set; }
        public List<FormSection> Sections { get; set; }
        
    }
    public class Form
    {
        public string FormName { get; set; }
        public List<string> ClassesName { get; set; }
        public string SoftDeleteColumn { get; set; }
        public bool ShowListing { get; set; }
        public bool ShowSearch { get; set; }
        public string ListingTitle { get; set; }
        
        public string Description { get; set; }
        public bool ShowFilters { get; set; }
        public int RecordsPerPage { get; set; }
        public int CurrentPage { get; set; }
        public string TableName { get; set; }
        public string? ListerTableName { get; set; }
        public string TableAlias { get; set; }
        public string SelectColumns { get; set; }
        public string Filters { get; set; }
        public string Orders { get; set; }
        public string Joins { get; set; }
        public string Extender { get; set; }
    }
    public class FormSection
    {
        public string Label { get; set; }
        public string Id { get; set; }
        public string CssClasses { get; set; }
        public string CssID { get; set; }
        public bool IsVisible { get; set; }
        public int DisplayableSeqNo { get; set; }
        public List<FieldModel> Fields { get; set; }
    }

    public class ResponseModel
    {
        public string Result { get; set; }
        public string Message { get; set; }
        public string Page { get; set; }
        public object Data { get; set; } // Use a specific type instead of object if you know the type of Data
        public int Created { get; set; }
        public int Updared { get; set; }
    }
    public class FormBuilderListerModel
    {
        public string PageName { get; set; }
        public string UrlFriendlyName { get; set; }
        public string Title { get; set; }
        public string ForOperation { get; set; }
        public string ForView { get; set; }
    }
    public class PageFilterModel
    {
        public string Field { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
        public string Operation { get; set; }
    }


}
