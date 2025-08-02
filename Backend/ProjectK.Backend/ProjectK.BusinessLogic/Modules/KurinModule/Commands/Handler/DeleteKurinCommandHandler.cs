using MediatR;
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
        private readonly IKurinRepository _kurinRepository;
        public DeleteKurinCommandHandler(IKurinRepository kurinRepository)
        {
            _kurinRepository = kurinRepository;
        }
        public async Task<bool> Handle(DeleteKurinCommand request, CancellationToken token)
        {
            if (request.KurinKey == Guid.Empty)
            {
                throw new ArgumentException("KurinKey cannot be empty.", nameof(request.KurinKey));
            }
            var isKurinDeleted = await _kurinRepository.DeleteAsync(request.KurinKey, token);
            return isKurinDeleted;
        }
    }
}
