using System;
using AutoMapper;
using CIDashboard.Domain.Entities;
using CIDashboard.Web.Models;
using BuildConfig = CIDashboard.Data.Entities.BuildConfig;
using CiSource = CIDashboard.Domain.Entities.CiSource;
using Project = CIDashboard.Data.Entities.Project;
// ReSharper disable ObjectCreationAsStatement

namespace CIDashboard.Web.MappingProfiles
{
    public class ViewModelProfilers : Profile
    {
        [Obsolete("Create a constructor and configure inside of your profile\'s constructor instead. Will be removed in 6.0")]
        protected override void Configure()
        {
            new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Project, Models.Project>()
                    .ForMember(dest => dest.Builds, opt => opt.MapFrom(src => src.BuildConfigs));
            });
            new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Models.Project, Project>()
                    .ForMember(dest => dest.BuildConfigs, opt => opt.MapFrom(src => src.Builds));
            });
            new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<BuildConfig, Models.BuildConfig>();
            });
            new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Models.BuildConfig, BuildConfig>();
            });
            new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CiSource, Models.CiSource>();
            });

            new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CiBuildResult, Build>()
                    .ForMember(dest => dest.CiExternalId, opt => opt.MapFrom(src => src.BuildId))
                    .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.BuildName))
                    .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString().ToLowerInvariant()));
            });
            new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CiBuildConfig, Models.BuildConfig>()
                    .ForMember(dest => dest.Id, opt => opt.Ignore())
                    .ForMember(dest => dest.CiExternalId, opt => opt.MapFrom(src => src.Id));
            });
        }
    }
}