using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectK.API.MappingProfiles;
using ProjectK.BusinessLogic.Modules.KurinModule.Queries.Kurins.Handlers;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Infrastructure.DbContexts;
using ProjectK.Infrastructure.Services;
using ProjectK.Infrastructure.Services.OrphanCleanup;

namespace ProjectK.API
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.AddDbContext<AppDbContext>(opt =>
            {
                opt.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                        b => b.MigrationsAssembly("ProjectK.Infrastructure")
                );
            });

            // --- Blob storage DI ---
            var blobOptions = new BlobStorageOptions
            {
                ConnectionString = builder.Configuration.GetConnectionString("BlobStorage") ?? "UseDevelopmentStorage=true",
                ContainerName = builder.Configuration["BlobStorage:ContainerName"] ?? "photos",
                PublicAccess = bool.TryParse(builder.Configuration["BlobStorage:PublicAccess"], out var pa) ? pa : true,
                BlobPrefix = builder.Configuration["BlobStorage:BlobPrefix"],
                PublicBaseUrl = builder.Configuration["BlobStorage:PublicBaseUrl"]
            };
            builder.Services.AddScoped<IPhotoReferenceProvider, MemberPhotoReferenceProvider>();

            builder.Services.AddSingleton(blobOptions);

            builder.Services.Configure<OrphanCleanupOptions>(builder.Configuration.GetSection("OrphanCleanup"));

            builder.Services.AddScoped<IPhotoService, AzureBlobPhotoService>(sp =>
            {
                var opts = sp.GetRequiredService<BlobStorageOptions>();
                var refProvider = sp.GetService<IPhotoReferenceProvider>();
                return new AzureBlobPhotoService(opts, refProvider);
            });

            builder.Services.AddHostedService<OrphanPhotoCleanupService>();
            // --- end Blob storage DI ---

            builder.Services.AddAutoMapper(cfg => { }, typeof(KurinModuleProfile));
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

            app.UseCors("AllowFrontend");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseRouting();
            app.MapControllers();

            app.MapGet("/", () => "Backend Started");

            app.Run();
        }
    }
}
