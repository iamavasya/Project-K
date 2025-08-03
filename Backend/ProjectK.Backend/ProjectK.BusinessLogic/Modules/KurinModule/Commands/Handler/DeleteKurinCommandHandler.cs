using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Commands.Handler
{
    public class DeleteKurinCommandHandler : IRequestHandler<DeleteKurinCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        public DeleteKurinCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<bool> Handle(DeleteKurinCommand request, CancellationToken token)
        {
            if (request.KurinKey == Guid.Empty)
            {
                throw new ArgumentException("KurinKey cannot be empty.", nameof(request.KurinKey));
            }
            var existing = await _unitOfWork.Kurins.GetByKeyAsync(request.KurinKey, token);
            if (existing is null)
            {
                return false;
            }
            _unitOfWork.Kurins.Delete(existing, token);
            var changes = await _unitOfWork.SaveChangesAsync(token);
            if (changes <= 0)
            {
                return false;
            }
            return true;
        }
    }
}
