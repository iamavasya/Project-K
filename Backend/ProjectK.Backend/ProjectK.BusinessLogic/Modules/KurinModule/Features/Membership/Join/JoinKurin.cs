using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using MembershipEntity = ProjectK.Common.Entities.KurinModule.Membership;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Membership.Join
{
    /// <summary>
    /// Takes an existing person into a kurin. Nothing about them changes — they keep every kurin they
    /// already belong to, and everything they have earned anywhere.
    /// </summary>
    public sealed record JoinKurin(
        Guid MemberKey,
        Guid KurinKey,
        Guid? GroupKey,
        MembershipKind Kind = MembershipKind.Youth) : IRequest<ServiceResult<Guid>>;

    public sealed class JoinKurinHandler : IRequestHandler<JoinKurin, ServiceResult<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemberDirectory _members;

        public JoinKurinHandler(IUnitOfWork unitOfWork, IMemberDirectory members)
        {
            _unitOfWork = unitOfWork;
            _members = members;
        }

        public async Task<ServiceResult<Guid>> Handle(JoinKurin request, CancellationToken cancellationToken)
        {
            if (request.MemberKey == Guid.Empty || request.KurinKey == Guid.Empty)
            {
                return ServiceResult<Guid>.Failure(
                    ResultType.BadRequest,
                    "MembershipKeysRequired",
                    "Both a member and a kurin are required.");
            }

            var person = await _members.FindAsync(request.MemberKey, cancellationToken);
            if (person is null)
            {
                return ServiceResult<Guid>.Failure(
                    ResultType.NotFound, "MemberNotFound", "No such member.");
            }

            var kurin = await _unitOfWork.Kurins.GetByKeyAsync(request.KurinKey, cancellationToken);
            if (kurin is null)
            {
                return ServiceResult<Guid>.Failure(
                    ResultType.NotFound, "KurinNotFound", "No such kurin.");
            }

            // A гурток of another kurin would place them somewhere their kurin does not reach.
            if (request.GroupKey.HasValue)
            {
                var group = await _unitOfWork.Groups.GetByKeyAsync(request.GroupKey.Value, cancellationToken);
                if (group is null || group.KurinKey != request.KurinKey)
                {
                    return ServiceResult<Guid>.Failure(
                        ResultType.BadRequest, "GroupNotInKurin", "That гурток does not belong to this kurin.");
                }
            }

            var already = await _unitOfWork.Memberships.GetActiveAsync(
                request.MemberKey, request.KurinKey, cancellationToken);
            if (already is not null)
            {
                return ServiceResult<Guid>.Failure(
                    ResultType.Conflict, "AlreadyAMember", "They already belong to this kurin.");
            }

            var membership = new MembershipEntity
            {
                MemberKey = request.MemberKey,
                UserKey = person.UserKey,
                KurinKey = request.KurinKey,
                GroupKey = request.GroupKey,
                Kind = request.Kind,
                JoinedAtUtc = DateTime.UtcNow
            };

            _unitOfWork.Memberships.Open(membership);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResult<Guid>(ResultType.Created, membership.MembershipKey);
        }
    }
}
