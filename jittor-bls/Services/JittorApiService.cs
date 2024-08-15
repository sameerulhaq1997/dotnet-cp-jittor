﻿using AutoMapper.Internal;
using Jittor.App.Helpers;
using Jittor.App.Models;
using Jittor.Shared.Enums;
using Newtonsoft.Json;
using PetaPoco;
using System.Collections.Generic;
using System.Reflection.Emit;
using static Jittor.App.DataServices.FrameworkRepository;
using static Jittor.App.Models.ProcessEntityModel;
using static System.Collections.Specialized.BitVector32;

namespace Jittor.App.Services
{
    public class JittorApiService
    {
        JittorDataServices _jittorDataServices;
        public JittorApiService(JittorDataServices jittorDataServices)
        {
            _jittorDataServices = jittorDataServices;
        }
        public async Task<FormPageModel> GetPage(string pageName, bool loadData = true)
        {
            var res = await _jittorDataServices.GetPageModel(pageName, loadData) ?? new JittorPageModel();

            var formPageModel = JittorMapperHelper.Map<FormPageModel, JITPage>(res);
            formPageModel.Form = JittorMapperHelper.Map<Form, JITPageTable>(res.PageTables.FirstOrDefault(x => x.ForView) ?? new JITPageTable());
            formPageModel.Form.FormName = res.PageName;

            var sections = JittorMapperHelper.MapList<FormSection, JITPageSection>(res.PageSections);
            var attributes = JittorMapperHelper.MapList<FieldModel, JITPageAttribute>(res.PageAttributes);

            formPageModel.Sections = new List<FormSection>();
                foreach (var section in sections.OrderBy(x => x.DisplayableSeqNo))
                {
                    section.Fields.AddRange(attributes.Where(x => x.SectionId == section.PageSectionId).OrderBy(x => x.DisplayableSeqNo));
                    formPageModel.Sections.Add(section);
                }
                return formPageModel;
            }
        public async Task<List<FieldModel>> GetTableAndChildTableColumns(string tableName, string? schemaName = "dbo")
        {
            var columns = _jittorDataServices.GetTableAndChildTableColumns(tableName, schemaName);
            List<FieldModel> fields = new List<FieldModel>();

            foreach (var column in columns)
            {
                var splittedColumnDescription = column.ColumnDescription != null ? column.ColumnDescription.Split(",") : new string[0];

                FieldModel field = new FieldModel();
                field.TableName = column.TableName;
                field.LabelEn = splittedColumnDescription.Length > 2 ? splittedColumnDescription[2] : column.ColumnName;
                field.HelperText = field.LabelEn;
                field.PlaceholderEn = field.LabelEn;
                field.PlaceholderAr = field.LabelAr;

                field.CssClasses = splittedColumnDescription.Length > 3 ? splittedColumnDescription[3] : column.ColumnName;
                field.CssID = field.CssClasses;

                field.Id = column.ColumnName;

                field.IsDisabled = false;
                field.IsVisible = true;
                field.FieldType = column.DataType.GetApplicationFieldTypeEnum();
                field.FieldSubType = column.DataType.GetApplicationFieldSubTypeEnum();
                field.InpValue = new FieldValue()
                {
                    ActualValue = column.DefaultValue,
                    ValueType = column.DataType.GetApplicationValueTypeEnum()
                };
                field.Validations = new List<ValidationRule>();
                if (column.IsNullable != "YES")
                    field.Validations = field.Validations.SetValidationParams("required", "required");
                if (column.MaxLength > 0)
                    field.Validations = field.Validations.SetValidationParams("characterRange", "not in range", new Dictionary<string, object>() { { "max", column.MaxLength } });
                if (column.NumericPrecision > 0)
                {
                    var maxNumber = "9".PadLeft(column.NumericPrecision - 1, '9');
                    field.Validations = field.Validations.SetValidationParams("NumericRange", "not in range", new Dictionary<string, object>() { { "max", maxNumber } });
                }
                field.ParentTableName = column.ReferencedTableName;
                field.ParentTableNameColumn = column.ReferencedTableNameColumnName;
                if (!string.IsNullOrEmpty(field.ParentTableName))
                {
                    field.FieldType = ApplicationFieldTypeEnum.SELECT;
                    field.FieldSubType = ApplicationFieldSubTypeEnum.SINGLE;
                }
                fields.Add(field);
            }
            return fields;
        }

        public async Task<List<string>> GetAllTables()
        {
            var tables = _jittorDataServices.GetAllTables();
            return tables;
        }
        public async Task<List<JITPage>> GetAllPages()
        {
            var pages = await _jittorDataServices.GetAllPages();
            return pages;
        }
        public async Task<bool> CreateNewPage(FormPageModel form, int? pageID)
        {
            return await _jittorDataServices.CreateNewPage(form, pageID);
        }
        public async Task<ResponseModel> ProcessEntity(Dictionary<string, object> keyValuePairs, string pageName)
        {
            if (keyValuePairs.ContainsKey("IconId") && keyValuePairs["IconId"] != null)
            {
                if (keyValuePairs["IconId"].ToString() == "Select Icon" || keyValuePairs["IconId"].ToString() == "0" || keyValuePairs["IconId"] == null)
                {
                    keyValuePairs["IconId"] = 17;
                }
            }
            if (pageName == "MacroCategoryPages")
            {
                if (!keyValuePairs.ContainsKey("CategoryId"))
                {
                    var errormessage = "Please Select Category.";
                    return new ResponseModel() { Result = "Failed", Message = errormessage, Page = pageName };
                }
                else if (keyValuePairs.ContainsKey("CategoryId") && (keyValuePairs["CategoryId"].ToString() == "0" || keyValuePairs["CategoryId"].ToString() == "Select Category Name"))
                {
                    var errormessage = "Please Select Valid Category.";
                    return new ResponseModel() { Result = "Failed", Message = errormessage, Page = pageName };
                }
                if (!keyValuePairs.ContainsKey("IconId"))
                {
                    keyValuePairs["IconId"] = 17;
                }
            }
            else if (pageName == "Sections")
            {
                if (!keyValuePairs.ContainsKey("PageId"))
                {
                    var errormessage = "Please Select Page.";
                    return new ResponseModel() { Result = "Failed", Message = errormessage, Page = pageName };
                }
                else if (keyValuePairs.ContainsKey("PageId") && (keyValuePairs["PageId"].ToString() == "0" || keyValuePairs["PageId"].ToString() == "Select Page Name"))
                {
                    var errormessage = "Please Select Valid Page.";
                    return new ResponseModel() { Result = "Failed", Message = errormessage, Page = pageName };
                }
            }
            var results = new List<List<dynamic>>();
            try
            {
                if (keyValuePairs.ContainsKey("IsActive") && keyValuePairs.ContainsKey("ShowInFooter"))
                {
                    if (keyValuePairs["IsActive"].ToString() == "False" && keyValuePairs["ShowInFooter"].ToString() == "True")
                    {
                        var errormessage = "Please Select Active Option. Otherwise De-Activate Show In Footer Option.";
                        return new ResponseModel() { Result = "Failed", Message = errormessage, Page = pageName };
                    }
                }
                if (keyValuePairs.ContainsKey("ShowInFooter") && !keyValuePairs.ContainsKey("IsActive"))
                {
                    var errormessage = "Please Select Active Option. Otherwise De-Activate Show In Footer Option.";
                    return new ResponseModel() { Result = "Failed", Message = errormessage, Page = pageName };
                }
                JittorPageModel? pageModel = await _jittorDataServices.GetPageModel(pageName);
                int created = 0;
                int updated = 0;
                string message = "";
                if (pageModel != null)
                {
                    var types = await _jittorDataServices.GetAttributeTypes();
                    var list = ProcessEntityModel.ProcessFrom(keyValuePairs, pageModel, types);
                    foreach (var item in list)
                    {
                        item.InsertCompulsaryFields = pageModel.InsertCompulsaryFields;
                        var foreignKeyValues = new Dictionary<string, object>();
                        var attributes = item.TableAttributes.Where(y => pageModel.PageTables.Any(x => x.TableName == y.ParentTableName));
                        if (attributes.Count() > 0)
                        {
                            foreach (var tableValues in results.SelectMany(innerList => innerList))
                            {
                                Dictionary<string, object> foreignKeyValue = ExtensionService.GetValuesFromDynamicDictionary(tableValues, attributes.Select(x => x.AttributeName).ToList());
                                foreignKeyValues = foreignKeyValues.Concat(foreignKeyValue).GroupBy(kvp => kvp.Key).ToDictionary(group => group.Key, group => group.Last().Value);
                            }
                        }
                        item.ForeignKeyValues = foreignKeyValues;
                        if (item.ValidToCreate)
                        {
                            var userID = 0;
                            var result = _jittorDataServices.ExecuteCommand(userID, pageName, item.InsertCommand, item.SelectCommand, item.InsertParamaters, ActionTypeEnum.Insert.ToString());
                            results.Add(result);
                            created++;
                        }
                        if (item.ValidToUpdate)
                        {
                            var userID = 0;
                            var result = _jittorDataServices.ExecuteCommand(userID, pageName, item.UpdateCommand, item.SelectCommand, item.UpdateParamaters, ActionTypeEnum.Update.ToString());
                            results.Add(result);
                            updated++;
                        }
                        message += string.Join(", ", item.ErrorMessages);
                    }
                }
                if (created > 0)
                {
                    message += $"{created} record created !";
                }
                if (updated > 0)
                {
                    message += $"{updated} record updated !";
                }
                if (created == 0 && updated == 0)
                {
                    throw (new Exception(message));
                }
                return new ResponseModel() { Result = "Success", Message = message, Page = pageName, Data = results, Created = created, Updared = updated };
            }
            catch (Exception e)
            {
                return new ResponseModel() { Result = "Failed", Message = e.Message, Page = pageName, Data = results, Created = 0, Updared = 0 };
            }
        }
        public async Task<bool> DeleteForm(int pageID)
        {
            var res = await _jittorDataServices.DeleteForm(pageID);
            return res;
        }
        public async Task<ResponseModel> pageId(jitterDeleteModel ID, int PageID, bool loadData = true)
        {
            var results = new List<List<dynamic>>();

            int deleted = 0;
            string message = "";
            List<string> columnNames = ID.ColumnNames;
            List<object> idValues = ID.IdValues;
            JittorPageModel? jitorPageModel = new JittorPageModel();
            jitorPageModel = _jittorDataServices.GetPageId(PageID);
            jitorPageModel = await _jittorDataServices.GetPageModel(jitorPageModel.UrlFriendlyName, loadData);

            try
            {
                var firstColumnName = jitorPageModel.PageAttributes.Where(a => a.IsPrimaryKey).Select(a => a.AttributeName).FirstOrDefault();

                if (columnNames != null && idValues != null && columnNames.Count == idValues.Count && columnNames.Contains(firstColumnName))
                {
                    bool chartbysectionid = false;

                    if (firstColumnName == "SectionId")
                    {
                        chartbysectionid = _jittorDataServices.getRecordsFromChartbySectionId(firstColumnName, ID.IdValues.FirstOrDefault());
                    }
                    if (firstColumnName == "ArticleID")
                    {
                        bool isDeleted = _jittorDataServices.DeleteRecordByIdandPageName(0, jitorPageModel.PageTables.FirstOrDefault().TableName, columnNames.FirstOrDefault(), idValues.FirstOrDefault(), true);

                    }
                    else if (!chartbysectionid)
                    {
                        var userID = 0;
                        bool isDeleted = _jittorDataServices.DeleteRecordByIdandPageName(userID, jitorPageModel.UrlFriendlyName, columnNames.FirstOrDefault(), idValues.FirstOrDefault());
                        if (isDeleted)
                        {
                            deleted++;
                        }
                        else
                        {
                            // Record cannot be deleted because it is associated with a Chart
                            message = "Record cannot be deleted as it is associated with another record.";
                        }
                    }
                    else
                    {
                        message = "Record cannot be deleted as it is associated with another record.";
                    }
                }

                if (deleted > 0)
                {
                    message += $"{deleted} record deleted !";
                }
                if (message.Contains("associated with another record"))
                {
                    return new ResponseModel() { Result = "Failed", Message = message, Page = jitorPageModel.PageName, Data = results, Created = 0 };
                }
                else
                {
                    return new ResponseModel() { Result = "Success", Message = message, Page = jitorPageModel.PageName, Data = results, Created = deleted };
                }
            }
            catch (Exception e)
            {
                return new ResponseModel() { Result = "Failed", Message = e.Message, Page = jitorPageModel.PageName, Data = results, Created = 0 };
            }
        }
        public async Task<string> SortedJitterData(int PageID, List<SortableItem> newOrder)
        {
            var results = new List<List<dynamic>>();
            bool result = false;

            JittorPageModel? jitorPageModel = new JittorPageModel();
            jitorPageModel = _jittorDataServices.GetPageId(PageID);
            jitorPageModel = await _jittorDataServices.GetPageModel(jitorPageModel.UrlFriendlyName, true);
            try
            {
                var DisplaySeqNo = jitorPageModel.PageAttributes.Select(a => a.AttributeName == "DisplaySeqNo").FirstOrDefault();
            }
            catch (Exception e)
            {
                return result ? "ok" : "error";
            }
            return result ? "ok" : "error";
        }
        public DataListerResponse<dynamic>? GetPageLister(DataListerRequest request, string? externalTable = null, string? externalSelectedColumns = null, List<PageJoinModel>? externalJoins = null, string? executeExternalScripts = null)
        {
            var res = _jittorDataServices.GetPageLister(request, externalTable, externalSelectedColumns, externalJoins,executeExternalScripts);
            return res;
        }
        public DropdownListerResponse PoplulateDropDowns(DropdownListerRequest request)
        {
            var res = _jittorDataServices.PoplulateDropDowns(request);
            return res;
        }
        public async Task<List<FormBuilderListerModel>> GetFormBuilderLister(int pageId = 0)
        {
            var res = await _jittorDataServices.GetFormBuilderLister(pageId);
            return res;
        }
        public async Task<FormPageModel> GetFormBuilderPageData(int pageId = 0)
        {
            var res = await _jittorDataServices.GetFormBuilderPageData(pageId);
            return res;
        }
        public async Task<DataListerResponse<dynamic>> GetFormBuilderLister()
        {
            var dataItems = await _jittorDataServices.GetFormBuilderAllData();
            var columns = new List<string>() { "id","PageName", "UrlFriendlyName", "Title", "ForOperation", "ForView" };
            return new DataListerResponse<dynamic>()
            {
                Items = dataItems,
                PageNumber = 1,//request.PageNumber,
                PageSize = 10,//pageSize,
                TotalItemCount = dataItems.Count(),
                Columns = columns.Select(x =>
                {
                    
                    return new
                    {
                        Field = x,
                        HeaderName = x,
                        TableName = x,
                        Hideable = x == "id" ? false : true,
                    };
                }).ToList(),
                //PageName = table.UrlFriendlyName
            };
        }
    }
}
