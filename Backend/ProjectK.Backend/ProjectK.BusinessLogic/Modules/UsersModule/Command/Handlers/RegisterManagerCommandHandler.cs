using MediatR;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;

using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Upsert;

using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Command.Handlers
{
    public class RegisterManagerCommandHandler : IRequestHandler<RegisterManagerCommand, ServiceResult<RegisterUserResponse>>
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _uow;

        public RegisterManagerCommandHandler(IMediator mediator, IUnitOfWork unitOfWork)
        {
            _mediator = mediator;
            _uow = unitOfWork;
        }

        public async Task<ServiceResult<RegisterUserResponse>> Handle(RegisterManagerCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _uow.BeginTransactionAsync(cancellationToken);

            try
            {
                // Step 1: Create the new Kurin
                var kurinResult = await _mediator.Send(new UpsertKurin(request.KurinNumber), cancellationToken);
                
                // Step 2: Register the user
                var userResult = await _mediator.Send(new RegisterUserCommand
                {
                    Email = request.Email,
                    Password = request.Password,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Role = UserRole.Manager.ToClaimValue(),
                    KurinKey = kurinResult.Data.KurinKey
                }, cancellationToken);

                // Step 3: Create the new Member and associate with User
                await _mediator.Send(new UpsertMember
                {
                    FirstName = request.FirstName,
                    MiddleName = request.MiddleName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    KurinKey = kurinResult.Data.KurinKey,
                    UserKey = userResult.Data.UserId
                }, cancellationToken);

                // Step 4: Save all changes and commit transaction
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
