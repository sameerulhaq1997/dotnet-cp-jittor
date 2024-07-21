using AutoMapper;
using Jittor.App.Models;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Jittor.App.DataServices.FrameworkRepository;

namespace Jittor.App.Helpers
{
    public class JittorMapperHelper
    {
        public static IMapper GetMapper<TDestination, TSource>()
        {
            MapperConfiguration mapperConfiguration = new MapperConfiguration(delegate (IMapperConfigurationExpression cnfg)
            {
                cnfg.AllowNullCollections = true;
                cnfg.CreateMap<TSource, TDestination>();
                cnfg.AddProfile(new JittorMappingProfile());
            });
            return mapperConfiguration.CreateMapper();
        }

        public static TDestination Map<TDestination, TSource>(TSource entity)
        {
            IMapper mapper = GetMapper<TDestination, TSource>();
            return mapper.Map<TDestination>(entity);
        }

        public static IQueryable<TDestination> MapList<TDestination, TSource>(IQueryable<TSource> entity)
        {
            IMapper mapper = GetMapper<TDestination, TSource>();
            return mapper.Map<IQueryable<TSource>, IQueryable<TDestination>>(entity);
        }

        public static IEnumerable<TDestination> MapList<TDestination, TSource>(IEnumerable<TSource> entity)
        {
            IMapper mapper = GetMapper<TDestination, TSource>();
            return mapper.Map<IEnumerable<TSource>, IEnumerable<TDestination>>(entity);
        }
    }
    public class JittorMappingProfile : Profile
    {
        public JittorMappingProfile()
        {
            CreateMap<FormPageModel, JITPage>()
            .ForMember(dest => dest.PageName, opt => opt.MapFrom(src => src.Form.FormName))
            .ForMember(dest => dest.UrlFriendlyName, opt => opt.MapFrom(src => src.Form.FormName))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Form.FormName))
            .ForMember(dest => dest.SoftDeleteColumn, opt => opt.MapFrom(src => src.Form.SoftDeleteColumn))
            .ForMember(dest => dest.ShowSearch, opt => opt.MapFrom(src => src.Form.ShowSearch))
            .ForMember(dest => dest.ShowListing, opt => opt.MapFrom(src => src.Form.ShowListing))
            .ForMember(dest => dest.ListingTitle, opt => opt.MapFrom(src => src.Form.ListingTitle))
            .ForMember(dest => dest.ListingPartialView, opt => opt.MapFrom(src => ""))
            .ForMember(dest => dest.GroupID, opt => opt.MapFrom(src => 1))
            .ForMember(dest => dest.Extender, opt => opt.MapFrom(src => src.Form.Extender))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Form.Description))
            .ForMember(dest => dest.ShowFilters, opt => opt.MapFrom(src => src.Form.ShowFilters))
            .ForMember(dest => dest.RecordsPerPage, opt => opt.MapFrom(src => src.Form.RecordsPerPage))
            .ForMember(dest => dest.CurrentPage, opt => opt.MapFrom(src => src.Form.CurrentPage))
            .ForMember(dest => dest.PageView, opt => opt.MapFrom(src => string.Empty))
            .ForMember(dest => dest.ListingCommands, opt => opt.MapFrom(src => string.Empty))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.ProjectId)).ReverseMap();

            CreateMap<Form, JITPageTable>()
            .ForMember(dest => dest.TableName, opt => opt.MapFrom(src => src.TableName))
            .ForMember(dest => dest.ForView, opt => opt.MapFrom(src => src.ForView))
            .ForMember(dest => dest.SelectColumns, opt => opt.MapFrom(src => src.SelectColumns))
            .ForMember(dest => dest.Filters, opt => opt.MapFrom(src => src.Filters))
            .ForMember(dest => dest.Orders, opt => opt.MapFrom(src => src.Orders))
            .ForMember(dest => dest.Joins, opt => opt.MapFrom(src => src.Joins))
            .ForMember(dest => dest.PageID, opt => opt.MapFrom(src => src.PageID))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.ProjectId)).ReverseMap();

            CreateMap<FormSection, JITPageSection>()
            .ForMember(dest => dest.Label, opt => opt.MapFrom(src => src.Label))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.CssId))
            .ForMember(dest => dest.Classes, opt => opt.MapFrom(src => src.CssClasses))
            .ForMember(dest => dest.IsVisible, opt => opt.MapFrom(src => src.IsVisible))
            .ForMember(dest => dest.DisplayableSeqNo, opt => opt.MapFrom(src => src.DisplayableSeqNo))
            .ForMember(dest => dest.PageId, opt => opt.MapFrom(src => src.PageID))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.ProjectId)).ReverseMap();

            CreateMap<FieldModel, JITPageAttribute>()
            .ForMember(dest => dest.AutoComplete, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.Sortable, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.Filterable, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.DisplayStyle, opt => opt.MapFrom(src => "default"))
            .ForMember(dest => dest.DisplayGroupID, opt => opt.MapFrom(src => 1))
            .ForMember(dest => dest.PageID, opt => opt.MapFrom(src => src.PageId))
            .ForMember(dest => dest.TableID, opt => opt.MapFrom(src => src.TableId))
            .ForMember(dest => dest.AttributeName, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.DisplayNameAr, opt => opt.MapFrom(src => src.LabelAr))
            .ForMember(dest => dest.DisplayNameEn, opt => opt.MapFrom(src => src.LabelEn))
            .ForMember(dest => dest.AttributeTypeID, opt => opt.MapFrom(src => src.AttributeTypeId))
            .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.Validations!.Any(x => x.Type == "required")))
            .ForMember(dest => dest.IsForeignKey, opt => opt.MapFrom(src => src.CurrentColumn!.IsForeignKey))
            .ForMember(dest => dest.ParentTableName, opt => opt.MapFrom(src => src.ParentTableName))
            .ForMember(dest => dest.ParentTableNameColumn, opt => opt.MapFrom(src => src.ParentTableNameColumn))
            .ForMember(dest => dest.ParentCondition, opt => opt.MapFrom(src => src.ParentCondition))
            .ForMember(dest => dest.ValidationExpression, opt => opt.MapFrom(src => JsonConvert.SerializeObject(src.Validations)))
            .ForMember(dest => dest.IsAutoIncreament, opt => opt.MapFrom(src => src.CurrentColumn!.IsAutoIncrement))
            .ForMember(dest => dest.IsPrimaryKey, opt => opt.MapFrom(src => src.CurrentColumn!.IsPrimaryKey))
            .ForMember(dest => dest.Editable, opt => opt.MapFrom(src => !src.IsDisabled))
            .ForMember(dest => dest.Searchable, opt => opt.MapFrom(src => src.SearchableSeqNo > 0))
            .ForMember(dest => dest.Displayable, opt => opt.MapFrom(src => src.IsVisible))
            .ForMember(dest => dest.EditableSeqNo, opt => opt.MapFrom(src => src.DisplayableSeqNo))
            .ForMember(dest => dest.SearchableSeqNo, opt => opt.MapFrom(src => src.SearchableSeqNo))
            .ForMember(dest => dest.DisplayableSeqNo, opt => opt.MapFrom(src => src.DisplayableSeqNo))
            .ForMember(dest => dest.MaxLength, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.PlaceholderText, opt => opt.MapFrom(src => src.PlaceholderEn))
            .ForMember(dest => dest.FieldType, opt => opt.MapFrom(src => (int)src.FieldType))
            .ForMember(dest => dest.FieldSubType, opt => opt.MapFrom(src => (int)src.FieldSubType))
            .ForMember(dest => dest.FieldActions, opt => opt.MapFrom(src => JsonConvert.SerializeObject(src.Actions)))
            .ForMember(dest => dest.SectionId, opt => opt.MapFrom(src => src.SectionId))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.ProjectId))
            .ForMember(dest => dest.DefaultValue, opt => opt.MapFrom(src => src.InpValue!.ActualValue ?? ""));


            CreateMap<JITPageAttribute, FieldModel>()
            .ForMember(dest => dest.PageId, opt => opt.MapFrom(src => src.PageID))
            .ForMember(dest => dest.TableId, opt => opt.MapFrom(src => src.TableID))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.AttributeName))
            .ForMember(dest => dest.LabelAr, opt => opt.MapFrom(src => src.DisplayNameAr))
            .ForMember(dest => dest.LabelEn, opt => opt.MapFrom(src => src.DisplayNameEn))
            .ForMember(dest => dest.AttributeTypeId, opt => opt.MapFrom(src => src.AttributeTypeID))
            .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired))
            .ForMember(dest => dest.IsForeignKey, opt => opt.MapFrom(src => src.IsForeignKey))
            .ForMember(dest => dest.ParentTableName, opt => opt.MapFrom(src => src.ParentTableName))
            .ForMember(dest => dest.ParentTableNameColumn, opt => opt.MapFrom(src => src.ParentTableNameColumn))
            .ForMember(dest => dest.ParentCondition, opt => opt.MapFrom(src => src.ParentCondition))
            .ForMember(dest => dest.Validations, opt => opt.MapFrom(src => JsonConvert.DeserializeObject<Dictionary<string, string>>(src.ValidationExpression)))
            .ForMember(dest => dest.IsAutoIncreament, opt => opt.MapFrom(src => src.IsAutoIncreament))
            .ForMember(dest => dest.IsPrimaryKey, opt => opt.MapFrom(src => src.IsPrimaryKey))
            .ForMember(dest => dest.IsDisabled, opt => opt.MapFrom(src => !src.Editable))
            .ForMember(dest => dest.Searchable, opt => opt.MapFrom(src => src.Searchable))
            .ForMember(dest => dest.IsVisible, opt => opt.MapFrom(src => src.Displayable))
            .ForMember(dest => dest.SearchableSeqNo, opt => opt.MapFrom(src => src.SearchableSeqNo))
            .ForMember(dest => dest.DisplayableSeqNo, opt => opt.MapFrom(src => src.DisplayableSeqNo))
            .ForMember(dest => dest.MaxLength, opt => opt.MapFrom(src => src.MaxLength))
            .ForMember(dest => dest.PlaceholderEn, opt => opt.MapFrom(src => src.PlaceholderText))
            .ForMember(dest => dest.FieldType, opt => opt.MapFrom(src => (ApplicationFieldTypeEnum)src.FieldType))
            .ForMember(dest => dest.FieldSubType, opt => opt.MapFrom(src => (ApplicationFieldSubTypeEnum)src.FieldSubType))
            .ForMember(dest => dest.Actions, opt => opt.MapFrom(src => JsonConvert.DeserializeObject<List<FieldAction>>(src.FieldActions)))
            .ForMember(dest => dest.SectionId, opt => opt.MapFrom(src => src.SectionId))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.ProjectId));

        }

    }
}
