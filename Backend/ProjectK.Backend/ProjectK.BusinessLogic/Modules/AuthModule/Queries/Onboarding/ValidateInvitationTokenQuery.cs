using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Queries.Onboarding
{
    public record ValidateInvitationTokenQuery(string Token) : IRequest<ServiceResult<InvitationValidationResponse>>;

    public record InvitationValidationResponse(string Email, string FirstName, string LastName, bool IsValid);
}
