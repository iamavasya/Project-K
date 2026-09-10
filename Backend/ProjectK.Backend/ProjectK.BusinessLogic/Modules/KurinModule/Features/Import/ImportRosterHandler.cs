using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Membership.Join;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using GroupEntity = ProjectK.Common.Entities.KurinModule.Group;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Import
{
    /// <summary>
    /// Reads every row, decides what it means, and — unless this is a dry run — writes it.
    /// <para>
    /// The same code answers both questions, which is the point: what the провід is shown before
    /// confirming is produced by the run that would do the work, not by a second implementation of
    /// the rules that could disagree with it.
    /// </para>
    /// </summary>
    public sealed class ImportRosterHandler : IRequestHandler<ImportRoster, ServiceResult<RosterImportReport>>
    {
        private readonly IUnitOfWork _kurinData;
        private readonly IMemberUnitOfWork _memberData;
        private readonly IMembershipRepository _memberships;
        private readonly IMediator _mediator;

        public ImportRosterHandler(
            IUnitOfWork kurinData,
            IMemberUnitOfWork memberData,
            IMediator mediator)
        {
            _kurinData = kurinData;
            _memberData = memberData;
            _memberships = kurinData.Memberships;
            _mediator = mediator;
        }

        public async Task<ServiceResult<RosterImportReport>> Handle(
            ImportRoster request,
            CancellationToken cancellationToken)
        {
            var kurin = await _kurinData.Kurins.GetByKeyAsync(request.KurinKey, cancellationToken);
            if (kurin is null)
            {
                return new ServiceResult<RosterImportReport>(ResultType.NotFound);
            }

            var rows = request.Rows.Select(row => RosterRow.From(row, request.Mapping)).ToList();

            var groups = (await _kurinData.Groups.GetAllAsync(request.KurinKey, cancellationToken)).ToList();
            var groupsByName = groups.ToDictionary(
                group => group.Name.Trim(),
                group => group.GroupKey,
                StringComparer.CurrentCultureIgnoreCase);

            var usable = rows.Where(row => row.Rejection() is null).ToList();

            var missingGroups = usable
                .Select(row => row.GroupName!.Trim())
                .Where(name => !groupsByName.ContainsKey(name))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            var foreignKurinRows = usable
                .Where(row => row.KurinNumber != kurin.Number)
                .Select(row => row.Number)
                .ToList();

            var candidates = await _memberData.Members.FindPossibleMatchesAsync(
                usable.Where(row => row.Email is not null).Select(row => row.Email!).ToList(),
                usable.Where(row => row.PhoneNumber is not null).Select(row => row.PhoneNumber!).ToList(),
                usable.Select(row => row.LastName!).ToList(),
                cancellationToken);

            var alreadyHere = await MemberKeysInKurinAsync(request.KurinKey, cancellationToken);

            // Гуртки are opened before anyone is placed in them: a row cannot name one that will
            // exist a moment later. The whole import is inside one transaction, so a later failure
            // takes the new гуртки back out with everything else.
            if (!request.DryRun && request.CreateMissingGroups)
            {
                foreach (var name in missingGroups)
                {
                    var group = new GroupEntity(name, request.KurinKey);
                    _kurinData.Groups.Create(group, cancellationToken);
                    groupsByName[name] = group.GroupKey;
                }

                await _kurinData.SaveChangesAsync(cancellationToken);
            }

            var results = new List<RowResult>(rows.Count);

            foreach (var row in rows)
            {
                if (row.Rejection() is { } reason)
                {
                    results.Add(new RowResult(row.Number, row.DisplayName, RowOutcome.Rejected, reason));
                    continue;
                }

                var groupName = row.GroupName!.Trim();
                if (!groupsByName.TryGetValue(groupName, out var groupKey))
                {
                    if (!request.CreateMissingGroups)
                    {
                        results.Add(new RowResult(
                            row.Number,
                            row.DisplayName,
                            RowOutcome.Rejected,
                            $"У курені немає гуртка «{groupName}»."));
                        continue;
                    }

                    // On a dry run the гурток does not exist yet, and saying so is the whole point of
                    // the run. The row is still reported as one that would go through.
                    groupKey = Guid.Empty;
                }

                var match = MatchFor(row, candidates);

                if (match is not null && alreadyHere.Contains(match.MemberKey))
                {
                    results.Add(new RowResult(row.Number, row.DisplayName, RowOutcome.AlreadyHere));
                    continue;
                }

                var outcome = match is null ? RowOutcome.Created : RowOutcome.Attached;

                if (!request.DryRun)
                {
                    var failure = match is null
                        ? await CreateAsync(row, request.KurinKey, groupKey, cancellationToken)
                        : await AttachAsync(match, request.KurinKey, groupKey, cancellationToken);

                    if (failure is not null)
                    {
                        results.Add(new RowResult(row.Number, row.DisplayName, RowOutcome.Rejected, failure));
                        continue;
                    }
                }

                results.Add(new RowResult(row.Number, row.DisplayName, outcome));
            }

            return new ServiceResult<RosterImportReport>(
                ResultType.Success,
                new RosterImportReport(results, missingGroups, foreignKurinRows, request.DryRun));
        }

        private async Task<HashSet<Guid>> MemberKeysInKurinAsync(Guid kurinKey, CancellationToken cancellationToken)
        {
            var summaries = await _memberData.Members.GetSummariesByKurinKeyAsync(kurinKey, cancellationToken);
            return summaries.Select(summary => summary.MemberKey).ToHashSet();
        }

        /// <summary>
        /// Whether the system already knows this person. An address or a phone number is a match on
        /// its own; a name is not, and is trusted only together with a birthday — two juniors called
        /// Іван Петренко in one станиця is not a hypothetical.
        /// </summary>
        private static MemberIdentity? MatchFor(RosterRow row, IReadOnlyCollection<MemberIdentity> candidates)
        {
            if (row.Email is not null)
            {
                var byEmail = candidates.FirstOrDefault(
                    person => string.Equals(person.Email, row.Email, StringComparison.OrdinalIgnoreCase));
                if (byEmail is not null)
                {
                    return byEmail;
                }
            }

            if (row.PhoneNumber is not null)
            {
                var byPhone = candidates.FirstOrDefault(
                    person => string.Equals(person.PhoneNumber, row.PhoneNumber, StringComparison.Ordinal));
                if (byPhone is not null)
                {
                    return byPhone;
                }
            }

            return candidates.FirstOrDefault(person =>
                string.Equals(person.LastName, row.LastName, StringComparison.CurrentCultureIgnoreCase)
                && string.Equals(person.FirstName, row.FirstName, StringComparison.CurrentCultureIgnoreCase)
                && person.DateOfBirth == row.DateOfBirth);
        }

        private async Task<string?> CreateAsync(
            RosterRow row,
            Guid kurinKey,
            Guid groupKey,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new UpsertMemberProfileCommand
            {
                MemberKey = Guid.Empty,
                KurinKey = kurinKey,
                GroupKey = groupKey == Guid.Empty ? null : groupKey,
                FirstName = row.FirstName!,
                MiddleName = row.MiddleName ?? string.Empty,
                LastName = row.LastName!,
                Email = row.Email ?? string.Empty,
                PhoneNumber = row.PhoneNumber ?? string.Empty,
                DateOfBirth = row.DateOfBirth!.Value,
                Address = row.Address,
                School = row.School,
                PlastLevelHistories = row.Levels()
            }, cancellationToken);

            return result.Type == ResultType.Success ? null : "Не вдалося створити запис.";
        }

        private async Task<string?> AttachAsync(
            MemberIdentity person,
            Guid kurinKey,
            Guid groupKey,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new JoinKurin(
                person.MemberKey,
                kurinKey,
                groupKey == Guid.Empty ? null : groupKey), cancellationToken);

            // Conflict means they were already taken in — by an earlier row of the same file, say.
            // Nothing went wrong and nothing needs undoing.
            return result.Type is ResultType.Success or ResultType.Created or ResultType.Conflict
                ? null
                : "Не вдалося долучити людину до куреня.";
        }
    }
}
