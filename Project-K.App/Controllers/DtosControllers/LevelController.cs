using Project_K.Infrastructure.Data;
using Project_K.BusinessLogic.Dtos;
using Project_K.Infrastructure.Models;

namespace Project_K.Controllers.DtosControllers
{
    public class LevelController(KurinDbContext context) : BaseDtosController<Level, LevelDto>(context)
    {
        protected override Level MapToModel(LevelDto dto)
        {
            return new Level
            {
                Name = dto.Name
            };
        }

        protected override void UpdateTheModel(Level model, LevelDto dto)
        {
            model.Name = dto.Name;
        }

        protected override int GetModelId(Level model)
        {
            return model.Id;
        }
    }
}