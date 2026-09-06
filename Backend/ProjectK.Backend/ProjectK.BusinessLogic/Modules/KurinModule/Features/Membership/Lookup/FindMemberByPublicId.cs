using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Membership.Lookup
{
    /// <summary>
    /// Resolves a public code to the card a провід confirms before taking someone in. Exact match
    /// only — a code is something a person hands over, not something to be guessed at.
    /// </summary>
    public sealed record FindMemberByPublicId(Guid KurinKey, string PublicId)
        : IRequest<ServiceResult<MembershipCandidate>>;

    /// <summary>The card, plus whether this kurin already has them.</summary>
    public sealed record MembershipCandidate(MemberCard Member, bool AlreadyInThisKurin);

    public sealed class FindMemberByPublicIdHandler
        : IRequestHandler<FindMemberByPublicId, ServiceResult<MembershipCandidate>>
    {
        private readonly IMemberDirectory _members;
        private readonly IMembershipRepository _memberships;

        public FindMemberByPublicIdHandler(IMemberDirectory members, IUnitOfWork kurinData)
        {
            _members = members;
            _memberships = kurinData.Memberships;
        }

        public async Task<ServiceResult<MembershipCandidate>> Handle(
            FindMemberByPublicId request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.PublicId))
            {
                return ServiceResult<MembershipCandidate>.Failure(
                    ResultType.BadRequest, "PublicIdRequired", "A public code is required.");
            }

            var card = await _members.FindByPublicIdAsync(request.PublicId, cancellationToken);
            if (card is null)
            {
                // The same answer whether the code is malformed or simply nobody's — a lookup that
                // distinguished them would let someone map out which codes exist.
                return ServiceResult<MembershipCandidate>.Failure(
                    ResultType.NotFound, "NoSuchCode", "No one has that code.");
            }

            var already = await _memberships.GetActiveAsync(
                card.MemberKey, request.KurinKey, cancellationToken);

            return new ServiceResult<MembershipCandidate>(
                ResultType.Success,
                new MembershipCandidate(card, already is not null));
        }
    }
}
