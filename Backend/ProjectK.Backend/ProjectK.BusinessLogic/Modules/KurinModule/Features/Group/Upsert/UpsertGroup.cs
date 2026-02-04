using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Group.Upsert
{
    public class UpsertGroup : IRequest<ServiceResult<GroupResponse>>
    {
        public Guid GroupKey { get; set; }
        public string Name { get; set; }
        public Guid KurinKey { get; set; }

        public UpsertGroup(Guid groupKey, string name)
        {
            GroupKey = groupKey;
            Name = name;
        }

        public UpsertGroup(string name, Guid kurinKey)
        {
            KurinKey = kurinKey;
            Name = name;
        }
    }

    public class UpsertGroupHandler : IRequestHandler<UpsertGroup, ServiceResult<GroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public UpsertGroupHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<GroupResponse>> Handle(UpsertGroup request, CancellationToken cancellationToken)
        {
            var existing = await _unitOfWork.Groups.GetByKeyAsync(request.GroupKey, cancellationToken);
            var kurin = await _unitOfWork.Kurins.GetByKeyAsync(request.KurinKey, cancellationToken);
            bool isCreated = false;

            if (existing == null)
            {
                if (kurin == null)
                {
                    return new ServiceResult<GroupResponse>(ResultType.NotFound);
                }
                // Create new Group
                existing = new(request.Name, request.KurinKey);
                _unitOfWork.Groups.Create(existing, cancellationToken);
                isCreated = true;
            }
            else
            {
                // Update existing Group
                _mapper.Map(request, existing);
                _unitOfWork.Groups.Update(existing, cancellationToken);
            }

            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (changes <= 0)
            {
                return new ServiceResult<GroupResponse>(
                    ResultType.InternalServerError);
            }

            var response = _mapper.Map<GroupResponse>(existing);

            return isCreated
                ? new ServiceResult<GroupResponse>(
                    ResultType.Created,
                    response,
                    CreatedAtActionName: "GetByKey",
                    CreatedAtRouteValues: new { groupKey = response.GroupKey })
                : new ServiceResult<GroupResponse>(ResultType.Success, response);
        }
    }
}
