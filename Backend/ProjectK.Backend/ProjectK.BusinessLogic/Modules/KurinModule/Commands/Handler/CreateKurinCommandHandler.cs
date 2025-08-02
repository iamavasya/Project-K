using MediatR;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Entities.KurinModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Commands.Handler
{
    public class CreateKurinCommandHandler : IRequestHandler<CreateKurinCommand, Guid>
    {
        private readonly IKurinRepository _kurinRepository;
        public CreateKurinCommandHandler(IKurinRepository kurinRepository)
        {
            _kurinRepository = kurinRepository;
        }
        public async Task<Guid> Handle(CreateKurinCommand request, CancellationToken token)
        {
            var kurin = new Common.Entities.KurinModule.Kurin
            {
                KurinKey = Guid.NewGuid(),
                Number = request.Number
            };
            return await _kurinRepository.CreateAsync(kurin, token);
        }
    }
}
