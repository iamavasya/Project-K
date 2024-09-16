using Project_K.Models;
using Project_K.Data;
using Project_K.Dtos;

namespace Project_K.Controllers.DtosControllers
{
    public class AddressController(KurinDbContext context) : BaseDtosController<Address, AddressDto>(context)
    {
        protected override Address MapToModel(AddressDto dto)
        {
            return new Address
            {
                AddressName = dto.Name
            };
        }
        protected override void UpdateTheModel(Address model, AddressDto dto)
        {
            model.AddressName = dto.Name;
        }
        protected override int GetModelId(Address model)
        {
            return model.Id;
        }
    }
}