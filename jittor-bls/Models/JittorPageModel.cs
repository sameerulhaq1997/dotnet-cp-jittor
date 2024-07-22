using PetaPoco;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using static Jittor.App.DataServices.FrameworkRepository;

namespace Jittor.App.Models
{
    public class JittorPageModel : JITPage
    {
        public JittorPageModel()
        {
            PageAttributes = new List<JITPageAttribute>();
            SearchAttributes = new List<JITPageAttribute>();
            PageAttributes.Add(new JITPageAttribute());
            PageAttributes.Add(new JITPageAttribute());
            SearchAttributes.Add(new JITPageAttribute());
            SearchAttributes.Add(new JITPageAttribute());
            PageTables = new List<JITPageTable>();
            PageTablesData = new Dictionary<string, object>();
            AttributeDisplayGroups = new List<AttributeDisplayGroup>();
            PageSections = new List<JITPageSection>();

        }
        public object SelectedRecord { get; set; }
        public object FilterModel
        {
            get
            {
                Dictionary<string, string> dic = new Dictionary<string, string>();
                foreach (var att in PageAttributes.Where(x => x.Displayable && x.Filterable))
                {
                    dic.Add(att.AttributeName, "");
                }
                return dic;
            }
        }
        public List<JITPageAttribute> PageAttributes { get; set; }
        public List<JITPageTable> PageTables { get; set; }
        public List<JITPageSection> PageSections { get; set; }
        public List<JITPageAttribute> SearchAttributes { get; set; }
        public Dictionary<string, object> PageTablesData { get; set; }
        public List<AttributeDisplayGroup> AttributeDisplayGroups { get; set; }
        public Dictionary<string, dynamic> ValidationRule
        {
            get
            {
                Dictionary<string, dynamic> dic = new Dictionary<string, dynamic>();
                foreach (var att in this.PageAttributes.Where(x => x.Editable))
                {
                    if (!string.IsNullOrEmpty(att.ValidationExpression))
                    {
                        object? o = JsonSerializer.Deserialize<Dictionary<string, object>>(att.ValidationExpression);
                        o = o ?? att.ValidationExpression;
                        dic.Add(att.AttributeName.ToLower(), o);
                    }
                    else if (att.IsRequired)
                    {
                        dic.Add(att.AttributeName.ToLower(), new { required = true });
                    }
                }
                return dic;
            }
        }
    }
    public class AttributeDisplayGroup : JITAttributeDisplayGroup
    {
        [ResultColumn] public string TypeName { get; set; }
        [ResultColumn] public string CssClass { get; set; }
    }
    public class ProcessEntityModel
    {
        public string TableName { get; set; } = string.Empty;
        public List<JITPageAttribute> TableAttributes { get; set; } = new List<JITPageAttribute>();
        public List<Dictionary<string, object>> OtherValues { get; set; } = new List<Dictionary<string, object>>();
        public List<Dictionary<string, object>>? PrimaryValues { get; set; }
        public bool ValidToCreate { get; set; } = false;
        public bool ValidToUpdate { get; set; } = false;
        public bool ValidToDelete { get; set; } = false;
        public List<JITAttributeType> AttributeTypes { get; set; } = new List<JITAttributeType>();
        public bool InsertCompulsaryFields { get; set; }
        public Dictionary<string, object> ForeignKeyValues { get; set; }
        public string InsertCommand
        {
            get
            {
                if (!this.ValidToCreate)
                {
                    return String.Empty; // Not valid to create !!!
                }

                StringBuilder sb = new StringBuilder();
                foreach (var item in this.OtherValues)
                {
                    string[] attribs = item.Keys.Select(x => x).ToArray();
                    var parameters = Enumerable.Range(0, attribs.Length).Select(x => $"@{x}");

                    // Append the additional fields to attribs
                    string[] allAttribs = InsertCompulsaryFields ? attribs.Concat(new[] { "CreatedOn", "CreatedBy", "ModifiedOn", "ModifiedBy" }).ToArray() : attribs;

                    var allParameters = Enumerable.Range(this.OtherValues.IndexOf(item) * allAttribs.Count(), allAttribs.Count()).Select(x => $"@{x}");

                    sb.Append($"INSERT INTO {this.TableName} ({string.Join(",", allAttribs)}) VALUES({string.Join(",", allParameters)});");
                }
                return sb.ToString();
            }
        }

        public string SelectCommand
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (this.ValidToCreate)
                {
                    string[] attribs = this.OtherValues.FirstOrDefault()!.Keys.Select(x => x).ToArray();
                    var parameters = Enumerable.Range(0, attribs.Length).Select(x => $"{attribs[x]} = @{x}");
                    sb.Append($"Select * From {this.TableName} Where {string.Join(" And ", parameters)}");
                }
                else if (this.PrimaryValues != null)
                {
                    string[] attribs = this.OtherValues.FirstOrDefault()!.Keys.Select((x, i) => $"{x} = @{i}").ToArray();
                    string[] whClaus = this.PrimaryValues.FirstOrDefault()!.Keys.Select((x, i) => $"{x} = @{i + attribs.Length}").ToArray();
                    sb.Append($"Select * From {this.TableName} Where {string.Join(" AND ", whClaus)}");
                }
                return sb.ToString();
            }
        }

        public string UpdateCommand
        {
            get
            {
                if (!this.ValidToUpdate || this.PrimaryValues == null)
                {
                    return String.Empty; //Not valid to create !!!
                }
                StringBuilder sb = new StringBuilder();
                foreach (var item in this.OtherValues)
                {
                    var index = this.OtherValues.IndexOf(item);
                    string[] attribs = item.Keys.Select((x, i) => $"{x} = @{i}").ToArray();
                    string[] whClaus = this.PrimaryValues[index].Keys.Select((x, i) => $"{x} = @{i + attribs.Length}").ToArray();
                    sb.AppendLine($"Update {this.TableName} Set {string.Join(",", attribs)} ");
                    sb.AppendLine($"Where {string.Join(" AND ", whClaus)}");
                }
                return sb.ToString();
            }
        }
        public object[] InsertParamaters
        {
            get
            {
                List<object> list = new List<object>();
                foreach (var item in this.OtherValues)
                {
                    var index = this.OtherValues.IndexOf(item);
                    foreach (var key in item.Keys)
                    {
                        var attrib = this.TableAttributes.Where(x => x.AttributeName == key).First();
                        var t = this.AttributeTypes.Where(x => x.AttributeTypeID == attrib.AttributeTypeID).First();
                        if (ForeignKeyValues.ContainsKey(key))
                        {
                            list.Add(ForeignKeyValues[key]);
                        }
                        else if (item[key] == null)
                        {
                            list.Add(t.GetDefaultValue());
                        }
                        else
                        {
                            list.Add(t.GetDefaultValue(item[key]));
                        }                        
                    }
                    // Add the additional field values to the parameters list
                    if (InsertCompulsaryFields)
                    {
                        list.Add(DateTime.Now);  // CreatedOn
                        list.Add(11000);         // CreatedBy
                        list.Add(DateTime.Now);  // ModifiedOn
                        list.Add(11000);         // ModifiedBy
                    }
                }

                return list.ToArray();
            }
        }
        public object[] UpdateParamaters
        {
            get
            {
                List<object> list = new List<object>();

                foreach (var item in this.OtherValues)
                {
                    var index = this.OtherValues.IndexOf(item);
                    foreach (var key in item.Keys)
                    {
                        var attrib = this.TableAttributes.Where(x => x.AttributeName == key).First();
                        var t = this.AttributeTypes.Where(x => x.AttributeTypeID == attrib.AttributeTypeID).First();
                        if (ForeignKeyValues.ContainsKey(key))
                        {
                            list.Add(ForeignKeyValues[key]);
                        }
                        else if (item[key] == null)
                        {
                            list.Add(t.GetDefaultValue());
                        }
                        //else if(OtherValues[key] == "ModifiedOn")
                        //{
                        //    list.Add(t.GetDefaultValue(OtherValues[key]));
                        //}
                        else
                        {
                            list.Add(t.GetDefaultValue(item[key]));
                        }
                    }
                    if (PrimaryValues != null)
                    {
                        foreach (var key in PrimaryValues[index].Keys)
                        {
                            var attrib = this.TableAttributes.Where(x => x.AttributeName == key).First();
                            var t = this.AttributeTypes.Where(x => x.AttributeTypeID == attrib.AttributeTypeID).First();
                            list.Add(t.GetDefaultValue(PrimaryValues[index][key]));
                        }
                    }
                }
                return list.ToArray();
            }
        }
        public List<string> ErrorMessages { get; set; } = new List<string>();
        public static List<ProcessEntityModel> ProcessFrom(Dictionary<string, object> keyValuePairs, JittorPageModel model, List<JITAttributeType> types)
        {
            List<ProcessEntityModel> list = new List<ProcessEntityModel>();
            foreach (var table in model.PageTables.Where(x => x.ForOperation))
            {
                ProcessEntityModel entity = new ProcessEntityModel();
                entity.TableName = table.TableName;
                entity.ValidToCreate = true;
                entity.AttributeTypes = types;
                entity.TableAttributes = model.PageAttributes.Where(x => x.TableID == table.TableID).ToList();
                if(keyValuePairs.ContainsKey(entity.TableName))
                    entity = ProcessFromRecursive(entity, keyValuePairs, model);
                else
                {
                    foreach (var att in entity.TableAttributes)
                    {
                        entity = ProcessFromRecursive(entity, keyValuePairs, model, att);
                    }
                }
                if (entity.OtherValues.Count == 0)
                {
                    entity.ValidToCreate = false;
                }
                list.Add(entity);
            }
            return list;
        }

        public static ProcessEntityModel ProcessFromRecursive(ProcessEntityModel entity, Dictionary<string, object> keyValuePairs, JittorPageModel model, JITPageAttribute? att = null)
        {
            var index = entity.OtherValues.Count - 1;
            if (index == -1 && att != null)
            {
                entity.OtherValues.Add(new Dictionary<string, object>());
                entity.PrimaryValues = entity.PrimaryValues ?? new List<Dictionary<string, object>>();
                entity.PrimaryValues.Add(new Dictionary<string, object>());
                index = 0;
            }

            if (att == null)
            {
                var keyValuePairsChilds = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(keyValuePairs[entity.TableName].ToString() ?? "[]") ?? new List<Dictionary<string, object>>();
                foreach (var item in keyValuePairsChilds)
                {
                    entity.OtherValues.Add(new Dictionary<string, object>());
                    entity.PrimaryValues = entity.PrimaryValues ?? new List<Dictionary<string, object>>();
                    entity.PrimaryValues.Add(new Dictionary<string, object>());
                    foreach (var attChild in entity.TableAttributes)
                    {
                        entity = ProcessFromRecursive(entity, item, model, attChild);
                    }
                }
            }
            else if (keyValuePairs.ContainsKey(att.AttributeName))
            {
                if (att.IsPrimaryKey)
                {
                    if (keyValuePairs[att.AttributeName] != null && keyValuePairs[att.AttributeName].ToString() != "0")
                    {
                        entity.PrimaryValues = entity.PrimaryValues ?? new List<Dictionary<string, object>>();
                        entity.PrimaryValues[index].Add(att.AttributeName, keyValuePairs[att.AttributeName]);
                        entity.ValidToUpdate = true;
                        entity.ValidToCreate = false;
                        entity.ValidToDelete = model.DeleteRecord && string.IsNullOrEmpty(model.SoftDeleteColumn);
                    }
                }
                else
                {
                    entity.OtherValues[index].Add(att.AttributeName, keyValuePairs[att.AttributeName]);
                }
            }
            else if (att.IsRequired && !att.IsPrimaryKey && string.IsNullOrEmpty(att.DefaultValue))
            {
                entity.ValidToCreate = false;
                entity.ErrorMessages.Add($"{att.DisplayNameEn} is required");
            }
            else if (att.IsRequired == false && (!string.IsNullOrEmpty(att.DefaultValue) || model.PageTables.Any(x => x.TableName == att.ParentTableName)))
            {
                entity.OtherValues[index].Add(att.AttributeName, att.DefaultValue);
            }
            return entity;
        }

       

        public class jitterDeleteModel
        {
            public List<string> ColumnNames { get; set; }
            public List<object> IdValues { get; set; }

        }
        public class SortableItem
        {
            public int DisplaySeqNo { get; set; }
            // Add other properties if needed
        }
        
    }
    public class JittorColumnInfo
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public string IsNullable { get; set; }
        public bool IsAutoIncrement { get; set; }
        public string DefaultValue { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsForeignKey { get; set; }
        public string ColumnDescription { get; set; }
        public int MaxLength { get; set; }
        public int NumericPrecision { get; set; }
        public int NumericScale { get; set; }
    }


    public class JittorQueryInfo
    {
        public List<string> SelectColumns { get; set; }
        public Dictionary<string, object> Filters { get; set; }
        public Dictionary<string, string> Orders { get; set; }
        public JittorQueryPageInfo Page { get; set; }
        public List<JittorQueryForeignKeys> ForeignKeys { get; set; }
    }

    public class JittorQueryPageInfo
    {
        public int PageSize { get; set; }
        public int PageNo { get; set; }
    }
    public class JittorQueryForeignKeys
    {
        public string? ForeignKey { get; set; }
        public string? ForeignTableName { get; set; }
        public string? ForeignTablePrimaryKey { get; set; }
    }


    public class DataListerRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Sort { get; set; }
        public List<PageFilterModel>? Filters { get; set; } = null;
        public int PageId { get; set; }
    }
    public class DataListerResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public long TotalItemCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public dynamic Columns { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItemCount / PageSize);

    }


    public class DropdownListerRequest
    {
        public string? Sort { get; set; }
        public List<PageFilterModel>? Filters { get; set; } = null;
        public string ColumnName { get; set; }
        public string TableName { get; set; }
        public List<PageJoinModel>? Joins { get; set; }
    }
    public class DropdownListerResponse
    {
        public List<FieldOption> Items { get; set; } = new List<FieldOption>();
    }

    public class JitPageTableExtended : JITPageTable
    {
        [Column] public string UrlFriendlyName { get; set; }
    }
}
