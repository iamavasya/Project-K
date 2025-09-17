using MediatR;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Kurins;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Members;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Command.Handlers
{
    public class RegisterFirstUserCommandHandler : IRequestHandler<RegisterFirstUserCommand, ServiceResult<RegisterUserResponse>>
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _uow;

        public RegisterFirstUserCommandHandler(IMediator mediator, IUnitOfWork unitOfWork)
        {
            _mediator = mediator;
            _uow = unitOfWork;
        }

        public async Task<ServiceResult<RegisterUserResponse>> Handle(RegisterFirstUserCommand request, CancellationToken cancellationToken)
        {
            // TODO: Продовжити працювати над транзакціями
            await using var transaction = await _uow.BeginTransactionAsync(cancellationToken);

            try
            {
                // Step 1: Register the user
                var userResult = await _mediator.Send(new RegisterUserCommand
                {
                    Email = request.Email,
                    Password = request.Password,
                    FirstName = request.FirstName,
                    LastName = request.LastName
                }, cancellationToken);

                // Step 2: Create the new Kurin
                var kurinResult = await _mediator.Send(new UpsertKurinCommand(request.KurinNumber), cancellationToken);

                // TODO: Додати в хендлері лінкування User to Member
                // Step 3: Create the new Member and associate with User
                var memberResult = await _mediator.Send(new UpsertMemberCommand
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
