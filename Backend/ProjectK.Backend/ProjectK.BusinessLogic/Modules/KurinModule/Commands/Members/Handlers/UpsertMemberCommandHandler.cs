using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update.Internal;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Commands.Members.Handlers
{
    public class UpsertMemberCommandHandler : IRequestHandler<UpsertMemberCommand, ServiceResult<MemberResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;
        public UpsertMemberCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IPhotoService photoService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _photoService = photoService;
        }

        public async Task<ServiceResult<MemberResponse>> Handle(UpsertMemberCommand request, CancellationToken cancellationToken)
        {
            var existing = await _unitOfWork.Members.GetByKeyAsync(request.MemberKey, cancellationToken);
            var group = await _unitOfWork.Groups.GetByKeyAsync(request.GroupKey, cancellationToken);
            bool isCreated = false;
            string? oldBlobName = null;

            if (group != null)
            {

                if (existing == null)
                {
                    // Create new Member
                    existing = _mapper.Map<Member>(request);

                    existing.KurinKey = group.KurinKey;

                    existing.LatestPlastLevel = existing.PlastLevelHistory
                                                        .OrderByDescending(p => p.DateAchieved)
                                                        .FirstOrDefault()?.PlastLevel;

                    _unitOfWork.Members.Create(existing, cancellationToken);
                    isCreated = true;
                }
                else
                {
                    // Update existing Member
                    oldBlobName = existing.ProfilePhotoBlobName;
                    _mapper.Map(request, existing);

                    existing.KurinKey = group.KurinKey;

                    UpdatePlastLevelHistory(existing.MemberKey, request.PlastLevelHistories, existing.PlastLevelHistory);

                    existing.LatestPlastLevel = existing.PlastLevelHistory
                                                        .OrderByDescending(p => p.DateAchieved)
                                                        .FirstOrDefault()?.PlastLevel;

                    _unitOfWork.Members.Update(existing, cancellationToken);
                }
            }
            else if (request.KurinKey != Guid.Empty && request.KurinKey != null)
            {
                // Create new Member with KurinKey from request
                existing = _mapper.Map<Member>(request);

                existing.GroupKey = null;

                existing.KurinKey = (Guid)request.KurinKey!;

                existing.LatestPlastLevel = existing.PlastLevelHistory
                                                    .OrderByDescending(p => p.DateAchieved)
                                                    .FirstOrDefault()?.PlastLevel;

                _unitOfWork.Members.Create(existing, cancellationToken);
                isCreated = true;
            }
            else return new ServiceResult<MemberResponse>(ResultType.NotFound);

            if (request.BlobContent is { Length: > 0 } && !string.IsNullOrWhiteSpace(request.BlobFileName))
            {
                var upload = await _photoService.UploadPhotoAsync(request.BlobContent, request.BlobFileName, cancellationToken);
                existing.ProfilePhotoBlobName = upload.BlobName;
            }

            if (request.RemoveProfilePhoto && oldBlobName != null)
            {
                existing.ProfilePhotoBlobName = null;
                await _photoService.DeletePhotoAsync(oldBlobName, cancellationToken);
            }

            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (changes <= 0)
            {
                return new ServiceResult<MemberResponse>(ResultType.InternalServerError);
            }

            // Removing old photo if a new one was uploaded
            if (!isCreated && oldBlobName != null && oldBlobName != existing.ProfilePhotoBlobName)
            {
                await _photoService.DeletePhotoAsync(oldBlobName, cancellationToken);
            }

            var response = _mapper.Map<MemberResponse>(existing);

            return isCreated
                ? new ServiceResult<MemberResponse>(
                    ResultType.Created,
                    response,
                    CreatedAtActionName: "GetByKey",
                    CreatedAtRouteValues: new { memberKey = response.MemberKey })
                : new ServiceResult<MemberResponse>(ResultType.Success, response);
        }
        private static void UpdatePlastLevelHistory(
            Guid memberKey,
            ICollection<PlastLevelHistoryDto> plastLevelHistoryDto,
            ICollection<PlastLevelHistory> plastLevelHistory)
        {
            if (plastLevelHistoryDto == null || !plastLevelHistoryDto.Any())
            {
                plastLevelHistory.Clear();
                return;
            }

            var dtoDict = plastLevelHistoryDto
                .Where(dto => dto.PlastLevelHistoryKey.HasValue && dto.PlastLevelHistoryKey != Guid.Empty)
                .ToDictionary(dto => dto.PlastLevelHistoryKey.Value);

            var entitiesToDelete = plastLevelHistory
                .Where(e => !dtoDict.ContainsKey(e.PlastLevelHistoryKey))
                .ToList();

            foreach (var entity in entitiesToDelete)
            {
                plastLevelHistory.Remove(entity); // Deleted"
            }

            foreach (var dto in plastLevelHistoryDto)
            {
                if (!dto.PlastLevelHistoryKey.HasValue || dto.PlastLevelHistoryKey == Guid.Empty)
                {
                    var newHistory = new PlastLevelHistory
                    {
                        MemberKey = memberKey,
                        PlastLevel = dto.PlastLevel,
                        DateAchieved = dto.DateAchieved
                    };
                    plastLevelHistory.Add(newHistory); // "Added"
                }
                else
                {
                    var existingHistory = plastLevelHistory.FirstOrDefault(e => e.PlastLevelHistoryKey == dto.PlastLevelHistoryKey);
                    if (existingHistory != null)
                    {
                        existingHistory.PlastLevel = dto.PlastLevel;
                        existingHistory.DateAchieved = dto.DateAchieved; // "Modified"
                    }

                }
            }
        }
    }
}
