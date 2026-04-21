using MediatR;
using ProjectK.Common.Models.Records;
using System;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding
{
    public record SubmitWaitlistRegistrationCommand(
        string FirstName,
        string LastName,
        string Email,
        string PhoneNumber,
        DateTime DateOfBirth,
        bool IsKurinLeaderCandidate,
        string? ClaimedKurinNameOrNumber) : IRequest<ServiceResult<Guid>>;
}
