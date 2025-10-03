using MediatR;
using ProjectK.BusinessLogic.Modules.UsersModule.Models;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Queries
{
    public class GetAllUsersQuery : IRequest<ServiceResult<IEnumerable<UserDto>>>
    {
    }
}
