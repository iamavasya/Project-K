using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Delete;

public class DeleteMember : IRequest<ServiceResult<object>>
{
    public Guid MemberKey { get; set; }
    public DeleteMember(Guid memberKey)
    {
        MemberKey = memberKey;
    }
}

public class DeleteMemberHandler : IRequestHandler<DeleteMember, ServiceResult<object>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemberDirectory _members;
    public DeleteMemberHandler(IUnitOfWork unitOfWork, IMemberDirectory members)
    {
        _unitOfWork = unitOfWork;
        _members = members;
    }

    public async Task<ServiceResult<object>> Handle(DeleteMember request, CancellationToken cancellationToken)
    {
        if (request.MemberKey == Guid.Empty)
        {
            return ServiceResult<object>.Failure(
                ResultType.BadRequest,
                "MemberKeyRequired",
                "MemberKey cannot be empty.");
        }
        var exists = await _members.ExistsAsync(request.MemberKey, cancellationToken);
        if (!exists)
        {
            return ServiceResult<object>.Failure(
                ResultType.NotFound,
                "MemberNotFound",
                $"Member with key {request.MemberKey} not found.");
        }
        await _unitOfWork.AgendaItems.RemoveAssignmentsForTargetsAsync([request.MemberKey], cancellationToken);
        await _members.RemoveAsync(request.MemberKey, cancellationToken);
        var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (changes <= 0)
        {
            return ServiceResult<object>.Failure(
                ResultType.InternalServerError,
                "MemberDeleteFailed",
                "Failed to delete Member due to internal error.");
        }
        return new ServiceResult<object>(ResultType.Success);
    }
}
