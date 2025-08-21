using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Infrastructure.DbContexts;
using ProjectK.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public IKurinRepository Kurins { get; }
        public IGroupRepository Groups { get; }
        public IMemberRepository Members { get; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;

            Kurins = new KurinRepository(_context);
            Groups = new GroupRepository(_context);
            Members = new MemberRepository(_context);
        }

        public Task<int> SaveChangesAsync(CancellationToken token = default)
        {
            return _context.SaveChangesAsync(token);
        }
    }
}
