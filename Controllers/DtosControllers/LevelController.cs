using Project_K.Models;
using Project_K.Data;
using Project_K.Dtos;

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

        protected override int GetModelId(Level model)
        {
            return model.Id;
        }
    }
}