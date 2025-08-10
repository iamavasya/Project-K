using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectK.API.MappingProfiles.KurinModule;
using ProjectK.BusinessLogic.Modules.Kurin.Queries.Handlers;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.API
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<AppDbContext>(opt =>
            {
                opt.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                        b => b.MigrationsAssembly("ProjectK.Infrastructure")
                );
            });

            builder.Services.AddAutoMapper(cfg => { }, typeof(KurinProfile));
            builder.Services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(GetKurinByKeyQueryHandler).Assembly)
            );
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddProjectDependencies();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Database.MigrateAsync();
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();
            app.MapControllers();

            app.MapGet("/", () => "Backend Started");

            app.Run();
        }
    }
}
