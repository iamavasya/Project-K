using Project_K.Infrastructure.Models;
using Project_K.BusinessLogic.Dtos;
using Project_K.Infrastructure.Data;

namespace Project_K.Controllers.DtosControllers
{
    public class TeamController(KurinDbContext context) : BaseDtosController<Team, TeamDto>(context)
    {
        protected override Team MapToModel(TeamDto dto)
        {
            return new Team
            {
                Name = dto.Name
            };
        }

        protected override void UpdateTheModel(Team model, TeamDto dto)
        {
            model.Name = dto.Name;
        }

        protected override int GetModelId(Team model)
        {
            return model.Id;
        }
    }
}