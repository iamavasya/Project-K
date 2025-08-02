using AutoMapper;
using ProjectK.BusinessLogic.Modules.Kurin.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands;
using ProjectK.Common.Dtos;
using ProjectK.Common.Entities.KurinModule;

namespace ProjectK.API.MappingProfiles.KurinModule
{
    public class KurinProfile : Profile
    {
        public KurinProfile()
        {
            CreateMap<Kurin, KurinResponse>();
            CreateMap<UpsertKurinCommand, KurinDto>();
        }
    }
}
