using MediatR;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;

using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Leadership.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Upsert;

using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.User.Register;
using ProjectK.Common.Models.Dtos.KurinModule;
using ProjectK.Common.Models.Dtos.KurinModule.Requests;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Features.User.RegisterKurin
{
    public class RegisterKurinCommandHandler : IRequestHandler<RegisterKurinCommand, ServiceResult<RegisterUserResponse>>
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _uow;

        public RegisterKurinCommandHandler(IMediator mediator, IUnitOfWork unitOfWork)
        {
            _mediator = mediator;
            _uow = unitOfWork;
        }

        public async Task<ServiceResult<RegisterUserResponse>> Handle(RegisterKurinCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _uow.BeginTransactionAsync(cancellationToken);

            try
            {
                // Step 1: Create the new Kurin
                var kurinResult = await _mediator.Send(new UpsertKurin(request.KurinNumber), cancellationToken);

                // Step 2: Register the user.
                //
                // An address that already has an account fails here, because AppUser.KurinKey holds a
                // single kurin. Letting one person lead two kurins means making that a collection and
                // reworking the kurin-scope claim with it, so it is a feature rather than something to
                // patch in on this path.
                var userResult = await _mediator.Send(new RegisterUserCommand
                {
                    Email = request.Email,
                    Password = request.Password,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Role = SystemRole.Member,
                    KurinKey = kurinResult.Data.KurinKey
                }, cancellationToken);

                // Step 3: Create the new Member and associate with User
                var memberResult = await _mediator.Send(new UpsertMember
                {
                    FirstName = request.FirstName,
                    MiddleName = request.MiddleName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    KurinKey = kurinResult.Data.KurinKey,
                    UserKey = userResult.Data.UserId
                }, cancellationToken);

                // Step 4: Make the new owner the kurin's Зв'язковий (KV office). Upsert syncs the
                // system role from the office automatically, granting full kurin management.
                await _mediator.Send(new UpsertLeadership(new UpsertLeadershipRequest
                {
                    Type = LeadershipType.KV.ToString(),
                    EntityKey = kurinResult.Data.KurinKey,
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    LeadershipHistories = new[]
                    {
                        new LeadershipHistoryMemberDto
                        {
                            Role = LeadershipRole.Zvyazkovyi.ToString(),
                            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                            Member = new MemberLookupDto { MemberKey = memberResult.Data.MemberKey }
                        }
                    }
                }), cancellationToken);

                // Step 5: Save all changes and commit transaction
                await _uow.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = new RegisterUserResponse
                {
                    UserId = userResult.Data.UserId,
                    Email = userResult.Data.Email,
                    FirstName = userResult.Data.FirstName,
                    LastName = userResult.Data.LastName,
                    Tokens = userResult.Data.Tokens
                };
                return new ServiceResult<RegisterUserResponse>(
                    Common.Models.Enums.ResultType.Success,
                    response);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
