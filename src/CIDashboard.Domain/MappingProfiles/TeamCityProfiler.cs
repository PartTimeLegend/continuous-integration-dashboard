﻿using System;
using AutoMapper;
using CIDashboard.Domain.Entities;
using TeamCitySharp.DomainEntities;

namespace CIDashboard.Domain.MappingProfiles
{
    public class TeamCityProfiler : Profile
    {
        [Obsolete("Create a constructor and configure inside of your profile\'s constructor instead. Will be removed in 6.0")]
        protected override void Configure()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Project, CiProject>()
                    .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.WebUrl))
                    .ForMember(dest => dest.BuildConfigs, opt => opt.MapFrom(src => src.BuildTypes.BuildType))
                    .AfterMap((src, dest) => dest.CiSource = CiSource.TeamCity);
            });


            // ReSharper disable once ObjectCreationAsStatement
            new MapperConfiguration(cfg1 =>
            {
                cfg1.CreateMap<BuildConfig, CiBuildConfig>()
                    .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.WebUrl))
                    .AfterMap((src, dest) => dest.CiSource = CiSource.TeamCity);
            });

            // ReSharper disable once ObjectCreationAsStatement
            new MapperConfiguration(cfg2 =>
                    {
                        cfg2.CreateMap<Build, CiBuildResult>()
                            .ForMember(dest => dest.Version, opt => opt.MapFrom(src => src.Number))
                            .ForMember(dest => dest.BuildName, opt => opt.MapFrom(src => src.BuildType.Name))
                            .ForMember(dest => dest.BuildId, opt => opt.MapFrom(src => src.BuildType.Id))
                            .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.WebUrl))
                            .ForMember(
                                dest => dest.Status,
                                opt => opt.MapFrom(
                                    src => src.Status.Equals("success", StringComparison.InvariantCultureIgnoreCase)
                                        ? CiBuildResultStatus.Success
                                        : CiBuildResultStatus.Failure))
                            .AfterMap((src, dest) => dest.CiSource = CiSource.TeamCity);
                    });
            }
    }
}
