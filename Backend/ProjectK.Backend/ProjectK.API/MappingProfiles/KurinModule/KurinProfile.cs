using AutoMapper;
using ProjectK.BusinessLogic.Modules.Kurin.Models;
using ProjectK.Common.Entities.KurinModule;

namespace ProjectK.API.MappingProfiles.KurinModule
{
    public class KurinProfile : Profile
    {
        public KurinProfile()
        {
            CreateMap<Kurin, KurinResponse>();
        }
    }
}
