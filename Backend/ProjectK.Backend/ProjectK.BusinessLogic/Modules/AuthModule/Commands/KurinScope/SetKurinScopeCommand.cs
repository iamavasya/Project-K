using MediatR;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.KurinScope
{
    public class SetKurinScopeCommand : IRequest<ServiceResult<LoginUserResponse>>
    {
        public Guid UserKey { get; }
        /// <summary>Kurin to step into, or null to return to system-wide scope.</summary>
        public Guid? KurinKey { get; }

        public SetKurinScopeCommand(Guid userKey, Guid? kurinKey)
        {
            UserKey = userKey;
            KurinKey = kurinKey;
        }
    }
}
