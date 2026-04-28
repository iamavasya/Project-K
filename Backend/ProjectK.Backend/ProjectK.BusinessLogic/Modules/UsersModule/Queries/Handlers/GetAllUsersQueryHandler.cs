using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectK.BusinessLogic.Modules.UsersModule.Models;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Queries.Handlers
{
    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, ServiceResult<IEnumerable<UserDto>>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        public GetAllUsersQueryHandler(UserManager<AppUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }
        public async Task<ServiceResult<IEnumerable<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var users = _userManager.Users.ToList();
            var kurins = (await _unitOfWork.Kurins.GetAllAsync(cancellationToken))
                .ToDictionary(k => k.KurinKey, k => k.Number);
            
            List<UserDto> result = [];
            foreach (var user in users)
            {
                var role = await _userManager.GetRolesAsync(user);
                UserDto userDto = new()
                {
                    UserId = user.Id,
                    KurinKey = user.KurinKey,
                    KurinNumber = user.KurinKey.HasValue && kurins.TryGetValue(user.KurinKey.Value, out var number) ? number : null,
                    Email = user.Email!,
                    Role = role.FirstOrDefault()!,
                    FirstName = user.FirstName!,
                    LastName = user.LastName!
                };
                result.Add(userDto);
            }
            return new ServiceResult<IEnumerable<UserDto>>(ResultType.Success, result);
        }
    }
}
