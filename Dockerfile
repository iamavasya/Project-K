# syntax=docker/dockerfile:1

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

COPY . /source

WORKDIR /source/Project-K.App

ARG TARGETARCH

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -a ${TARGETARCH/amd64/x64} --use-current-runtime --self-contained false -o /app

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS development
COPY . /source
ENV ASPNETCORE_ENVIRONMENT Development
WORKDIR /source/Project-K.App
CMD dotnet run --no-launch-profile

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT Development

USER $APP_UID

ENTRYPOINT ["dotnet", "Project-K.App.dll"]