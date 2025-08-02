using AutoMapper;
using ProjectK.BusinessLogic.Modules.Kurin.Models;
using ProjectK.Common.Entities.Kurin;

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
