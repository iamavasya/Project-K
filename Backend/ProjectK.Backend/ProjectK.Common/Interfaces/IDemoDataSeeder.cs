using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces
{
    public interface IDemoDataSeeder
    {
        Task SeedAsync(CancellationToken cancellationToken = default);
    }
}
