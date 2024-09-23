using Project_K.Infrastructure.Data;
using Project_K.BusinessLogic.Dtos;
using Project_K.Infrastructure.Models;

namespace Project_K.Controllers.DtosControllers
{
    public class KurinLevelController(KurinDbContext context) : BaseDtosController<KurinLevel, KurinLevelDto>(context)
    {
        protected override KurinLevel MapToModel(KurinLevelDto dto)
        {
            return new KurinLevel
            {
                Name = dto.Name,
                RequiredPoints = dto.RequiredPoints
            };
        }
        protected override void UpdateTheModel(KurinLevel model, KurinLevelDto dto)
        {
            model.Name = dto.Name;
            model.RequiredPoints = dto.RequiredPoints;
        }
        protected override int GetModelId(KurinLevel model)
        {
            return model.Id;
        }
    }
}
