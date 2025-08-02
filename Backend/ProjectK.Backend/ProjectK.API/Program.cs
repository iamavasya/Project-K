using Microsoft.Extensions.DependencyInjection;
using ProjectK.API.MappingProfiles.KurinModule;

namespace ProjectK.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddAutoMapper(cfg => { }, typeof(KurinProfile));

            var app = builder.Build();

            app.Run();
        }
    }
}
