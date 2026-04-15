using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using ProjectK.ProbeAndBadges.Abstractions;

namespace ProjectK.API.Helpers
{
    public static class BadgesImagesStaticFilesExtensions
    {
        private const string BadgesImagesRequestPath = "/badges_images";
        private const int CacheMaxAgeSeconds = 604800;

        public static IApplicationBuilder UseBadgesImagesStaticFiles(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("BadgesImagesStaticFiles");

            IBadgesAssetsStore? assetsStore;
            try
            {
                assetsStore = scope.ServiceProvider.GetService<IBadgesAssetsStore>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize badges assets store. Static images endpoint will be unavailable.");
                return app;
            }

            if (assetsStore is null ||
                !assetsStore.HasAssets ||
                string.IsNullOrWhiteSpace(assetsStore.AssetsDirectoryPath) ||
                !Directory.Exists(assetsStore.AssetsDirectoryPath))
            {
                logger.LogWarning("Badges assets directory is unavailable. Static images endpoint is disabled.");
                return app;
            }

            app.MapWhen(ctx =>
                    ctx.Request.Path.StartsWithSegments(BadgesImagesRequestPath, StringComparison.OrdinalIgnoreCase),
                branch =>
                {
                    branch.Use(async (context, next) =>
                    {
                        if (context.User?.Identity?.IsAuthenticated != true)
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return;
                        }

                        await next();
                    });

                    branch.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = new PhysicalFileProvider(assetsStore.AssetsDirectoryPath),
                        RequestPath = BadgesImagesRequestPath,
                        ContentTypeProvider = new FileExtensionContentTypeProvider(),
                        OnPrepareResponse = ctx =>
                        {
                            ctx.Context.Response.Headers.CacheControl = $"private,max-age={CacheMaxAgeSeconds}";
                        }
                    });
                });

            logger.LogInformation(
                "Badges images static files mapped. RequestPath={RequestPath}, AssetsDirectoryPath={AssetsDirectoryPath}",
                BadgesImagesRequestPath,
                assetsStore.AssetsDirectoryPath);

            return app;
        }
    }
}
