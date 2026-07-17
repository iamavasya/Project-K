FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG NUGET_AUTH_TOKEN=
ENV NUGET_AUTH_TOKEN=${NUGET_AUTH_TOKEN}
WORKDIR /src

COPY Backend/ProjectK.Backend/ Backend/ProjectK.Backend/
RUN if [ -z "$NUGET_AUTH_TOKEN" ]; then \
      echo "NUGET_AUTH_TOKEN is required to restore Project-K private GitHub NuGet packages when building the API image locally."; \
      echo "Set PROJECTK_NUGET_AUTH_TOKEN in .env, or use the published ghcr.io/iamavasya/projectk-api image from the release bundle."; \
      exit 1; \
    fi
RUN dotnet restore Backend/ProjectK.Backend/ProjectK.API/ProjectK.API.csproj
RUN dotnet publish Backend/ProjectK.Backend/ProjectK.API/ProjectK.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProjectK.API.dll"]
