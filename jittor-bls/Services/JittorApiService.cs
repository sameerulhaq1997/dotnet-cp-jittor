using Jittor.App.Models;
using PetaPoco;
using static Jittor.App.DataServices.FrameworkRepository;

namespace Jittor.App.Services
{
    public class JittorApiService
    {
        JittorDataServices _jittorDataServices;
        public JittorApiService(JittorDataServices jittorDataServices)
        {
            _jittorDataServices = jittorDataServices;
        }
        public async Task<PageModel> GetPage(string pageName)
        {
            var res = await _jittorDataServices.GetPageModel(pageName, true) ?? new JittorPageModel();
            return new PageModel() { PageName = pageName, JittorPageModel = res };
        }
        public async Task<List<FieldModel>> GetTableAndChildTableColumns(string tableName, string? schemaName = "dbo")
        {
            var columns = await _jittorDataServices.GetTableAndChildTableColumns(tableName, schemaName);
            List<FieldModel> fields = new List<FieldModel>();

            foreach (var column in columns)
            {
                var splittedColumnDescription = column.ColumnDescription != null ? column.ColumnDescription.Split(",") : new string[0];

                FieldModel field = new FieldModel();
                field.TableName = column.TableName;
                field.LabelEn = splittedColumnDescription.Length > 2 ? splittedColumnDescription[2] : column.ColumnName;
                field.HelperText = field.LabelEn;
                field.Placeholder = field.LabelEn;

                field.CssClasses = splittedColumnDescription.Length > 3 ? splittedColumnDescription[3] : column.ColumnName;
                field.CssID = field.CssClasses;

                field.Id = column.ColumnName;
                field.Name = column.ColumnName;

                field.IsDisabled = false;
                field.IsVisible = true;
                field.FieldType = splittedColumnDescription.Length > 0 ? splittedColumnDescription[0].ParseEnum<ApplicationFieldTypeEnum>() : ApplicationFieldTypeEnum.INPUT;
                field.FieldSubType = splittedColumnDescription.Length > 1 ? splittedColumnDescription[1].ParseEnum<ApplicationFieldSubTypeEnum>() : ApplicationFieldSubTypeEnum.TEXT;
                field.InpValue = new InpValue()
                {
                    ActualValue = column.DefaultValue,
                    ValueType = column.DataType.GetApplicationValueTypeEnum()
                };
                field.Validations = new Dictionary<string, object>();
                if (column.IsNullable == "YES")
                    field.Validations.Add("required", "true");
                if (column.MaxLength > 0)
                    field.Validations.Add("maxLength", column.MaxLength.ToString());
                if (column.NumericPrecision > 0)
                {
                    var maxNumber = "9".PadLeft(column.NumericPrecision - 1, '9');
                    field.Validations.Add("maxNumber", maxNumber);
                    field.Validations.Add("maxScale", column.NumericScale.ToString());
                }
                fields.Add(field);
            }
            return fields;
        }

        public async Task<bool> CreateNewPage(FormPageModel form)
        {
            return await _jittorDataServices.CreateNewPage(form);
        }
    }
}
