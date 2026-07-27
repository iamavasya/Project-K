using MediatR;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.KurinScope
{
    public class SetKurinScopeCommand : IRequest<ServiceResult<LoginUserResponse>>
    {
        public Guid UserKey { get; }
        public Guid? KurinKey { get; }

        public SetKurinScopeCommand(Guid userKey, Guid? kurinKey)
        {
            UserKey = userKey;
            KurinKey = kurinKey;
        }
    }
}
