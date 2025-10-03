using MediatR;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Queries
{
    public class CheckEntityAccessQuery : IRequest<ServiceResult<bool>>
    {
        public string EntityType { get; set; }
        public string EntityKey { get; set; }
        public string? ActiveKurinKey { get; set; }
    }
}
