FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 9001

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Osnovanie.Api/Osnovanie.Api.csproj", "src/Osnovanie.Api/"]
COPY ["src/Modules/Osnovanie.Modules.Auth/Osnovanie.Modules.Auth.csproj", "src/Modules/Osnovanie.Modules.Auth/"]
COPY ["src/Osnovanie.Framework/Osnovanie.Framework.csproj", "src/Osnovanie.Framework/"]
COPY ["src/Osnovanie.Shared/Osnovanie.Shared.csproj", "src/Osnovanie.Shared/"]
COPY ["src/Osnovanie.Infrastructure/Osnovanie.Infrastructure.csproj", "src/Osnovanie.Infrastructure/"]

#COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

RUN dotnet restore "src/Osnovanie.Api/Osnovanie.Api.csproj"
COPY . .
WORKDIR "/src/src/Osnovanie.Api"
RUN dotnet build "./Osnovanie.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Osnovanie.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Osnovanie.Api.dll"]
