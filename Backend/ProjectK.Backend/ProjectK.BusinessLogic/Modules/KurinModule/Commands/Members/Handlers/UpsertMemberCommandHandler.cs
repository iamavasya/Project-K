using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
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
                    _unitOfWork.Members.Create(existing, cancellationToken);
                    isCreated = true;
                }
                else
                {
                    // Update existing Member
                    oldBlobName = existing.ProfilePhotoBlobName;
                    _mapper.Map(request, existing);
                    existing.KurinKey = group.KurinKey;
                    _unitOfWork.Members.Update(existing, cancellationToken);
                }
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
    }
}
