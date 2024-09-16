using Project_K.Data;
using Project_K.Models;
using Project_K.Dtos;

namespace Project_K.Controllers.DtosControllers
{
    public class SchoolController(KurinDbContext context) : BaseDtosController<School, SchoolDto>(context)
    {
        protected override School MapToModel(SchoolDto dto)
        {
            return new School
            {
                Name = dto.Name
            };
        }
        
        protected override void UpdateTheModel(School model, SchoolDto dto)
        {
            model.Name = dto.Name;
        }

        protected override int GetModelId(School model)
        {
            return model.Id;
        }
    }
}
